using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Pie.Controls;
using Pie.Models;
using Pie.Services;

namespace Pie.Views
{
    public partial class PieMenuWindow : Window
    {
        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        private readonly PieMenuControl _pieMenuControl;
        private readonly SettingsService _settingsService;
        private readonly WindowService _windowService;
        private readonly PresetService _presetService;
        private readonly KeyboardService _keyboardService;
        private readonly MediaService _mediaService;
        private readonly SoundService _soundService;
        private PieMenuMode _currentMode;
        private bool _isClosing;
        private IntPtr _originalForegroundWindow;
        private PieMenuItem? _pendingActionItem;
        private double _dpiScaleX = 1.0;
        private double _dpiScaleY = 1.0;
        private DateTime _openedAt = DateTime.MinValue;

        public bool IsClosing => _isClosing;

        public event EventHandler? SettingsRequested;

        public PieMenuWindow(
            SettingsService settingsService,
            WindowService windowService,
            PresetService presetService,
            KeyboardService keyboardService,
            MediaService mediaService,
            SoundService soundService)
        {
            _settingsService = settingsService;
            _windowService = windowService;
            _presetService = presetService;
            _keyboardService = keyboardService;
            _mediaService = mediaService;
            _soundService = soundService;

            InitializeWindow();
            InitializeDpiScale();

            _pieMenuControl = new PieMenuControl
            {
                MenuRadius = _settingsService.Settings.MenuRadius,
                IconSize = _settingsService.Settings.IconSize,
                SoundService = _soundService
            };
            _pieMenuControl.ItemSelected += PieMenuControl_ItemSelected;
            _pieMenuControl.MenuClosed += PieMenuControl_MenuClosed;

            Content = _pieMenuControl;
        }

        private void InitializeWindow()
        {
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            // Use a nearly transparent background to ensure we catch mouse clicks everywhere
            Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0));
            Topmost = true;
            ShowInTaskbar = false;
            ResizeMode = ResizeMode.NoResize;
            ShowActivated = true;

            PreviewKeyDown += PieMenuWindow_PreviewKeyDown;
            MouseMove += PieMenuWindow_MouseMove;
            MouseLeftButtonUp += PieMenuWindow_MouseLeftButtonUp;
            MouseLeftButtonDown += PieMenuWindow_MouseLeftButtonDown;
            MouseRightButtonUp += PieMenuWindow_MouseRightButtonUp;
            // Block middle mouse button events from closing the menu
            PreviewMouseDown += PieMenuWindow_PreviewMouseDown;
            Deactivated += (s, e) =>
            {

                if (IsVisible && !_isClosing && (DateTime.Now - _openedAt).TotalMilliseconds > 200)
                {
                    LogService.Debug("Window deactivated - closing menu");
                    CloseMenu();
                }
            };
        }

        private void PieMenuWindow_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Right-click in the center circle cycles the mode
            var pos = e.GetPosition(_pieMenuControl);
            double centerX = _pieMenuControl.ActualWidth / 2;
            double centerY = _pieMenuControl.ActualHeight / 2;
            double dx = pos.X - centerX;
            double dy = pos.Y - centerY;
            double distance = Math.Sqrt(dx * dx + dy * dy);

            // Inner radius is 50 (defined in PieMenuControl/Settings)
            double innerRadius = 50;

            if (distance < innerRadius)
            {
                LogService.Debug("Right-click in center - cycling mode");
                CycleMode();
                e.Handled = true;
            }
        }

        private void InitializeDpiScale()
        {
            var source = PresentationSource.FromVisual(this);
            if (source != null && source.CompositionTarget != null)
            {
                _dpiScaleX = source.CompositionTarget.TransformToDevice.M11;
                _dpiScaleY = source.CompositionTarget.TransformToDevice.M22;
            }
            else
            {
                // Fallback to system DPI
                try
                {
                    using (var g = System.Drawing.Graphics.FromHwnd(IntPtr.Zero))
                    {
                        _dpiScaleX = g.DpiX / 96.0;
                        _dpiScaleY = g.DpiY / 96.0;
                    }
                }
                catch
                {
                    // Default to 1.0 if fails
                }
            }
        }

        public async void ShowAtCursor(PieMenuMode mode)
        {
            LogService.Debug($"ShowAtCursor called - mode: {mode}, IsVisible: {IsVisible}, IsClosing: {_isClosing}");

            // Only capture foreground window if the menu is NOT already visible.
            // If it IS visible (e.g. double-tap switching modes), we want to preserve the original window handle
            // so Controller mode works correctly.
            if (!IsVisible)
            {
                _originalForegroundWindow = GetForegroundWindow();
            }

            _pendingActionItem = null;

            // Cancel any ongoing close animation and reset state
            if (_isClosing)
            {
                LogService.Debug("Cancelling ongoing close animation");
                _isClosing = false;
                this.BeginAnimation(OpacityProperty, null); // Cancel animation
                _pieMenuControl.BeginAnimation(OpacityProperty, null);
            }

            // If switching modes while open, don't hide/flicker. Just update the mode.
            if (IsVisible)
            {
                LogService.Debug("PieMenu already visible, switching mode");
            }
            else
            {
                // Reset opacity only if opening fresh
                this.Opacity = 1;
            }

            _currentMode = mode;
            _isClosing = false;

            // --- DPI AWARE POSITIONING ---
            // Only update position if opening fresh (optional, but prevents jumping if mouse moved slightly)
            if (!IsVisible)
            {
                GetCursorPos(out POINT cursorPos);

                if (_dpiScaleX == 0 || _dpiScaleY == 0) InitializeDpiScale();

                double cursorX = cursorPos.X / _dpiScaleX;
                double cursorY = cursorPos.Y / _dpiScaleY;

                double menuSize = (_settingsService.Settings.MenuRadius + _settingsService.Settings.IconSize) * 2 + 40;
                Width = menuSize;
                Height = menuSize;

                double left = cursorX - menuSize / 2;
                double top = cursorY - menuSize / 2;

                Left = left;
                Top = top;
            }

            // Prepare UI state
            if (!IsVisible)
            {

                this.Opacity = 0;

                // Also clear control state
                _pieMenuControl.SetItems(new List<PieMenuItem>());
                _pieMenuControl.Opacity = 0;

                _openedAt = DateTime.Now;
                Show();
            }

            // Load data asynchronously
            List<PieMenuItem> items;
            if (mode == PieMenuMode.Switcher)
            {
                items = await _windowService.GetRunningApplicationsAsync();
            }
            else
            {
                items = GetItemsForMode(mode);
            }

            if (items.Count == 0)
            {
                LogService.Debug($"No items for mode {mode}");
                if (!IsVisible) Hide();
                else CloseMenu(); // Graceful close if empty
                return;
            }

            _pieMenuControl.SetItems(items);

            // Cancel any ongoing opacity animation
            _pieMenuControl.BeginAnimation(System.Windows.UIElement.OpacityProperty, null);

            // If it was already visible (switching modes), animate nicely
            if (IsVisible && this.Opacity > 0.9)
            {
                 _pieMenuControl.AnimateIn();
            }
            else
            {
                // New open - use dispatcher to prevent flash
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    // Now that data is ready and layout is updated, reveal the window
                    this.Opacity = 1;
                    _pieMenuControl.Visibility = Visibility.Visible;
                    _pieMenuControl.Opacity = 1;
                    _soundService.PlayActivateSound();
                    _pieMenuControl.AnimateIn();
                }), System.Windows.Threading.DispatcherPriority.Render);
            }

            Activate();

            // Prevent immediate closing from the same click/key used to open (400ms debounce)
            _ignoreClicksUntil = DateTime.Now.AddMilliseconds(400);
            // Allow toggle close via hotkey/middle button after 700ms
            _canToggleCloseAfter = DateTime.Now.AddMilliseconds(700);
        }

        private DateTime _ignoreClicksUntil = DateTime.MinValue;
        private DateTime _canToggleCloseAfter = DateTime.MinValue;

        public DateTime CanToggleCloseAfter => _canToggleCloseAfter;

        private List<PieMenuItem> GetItemsForMode(PieMenuMode mode)
        {
            return mode switch
            {
                PieMenuMode.Switcher => _windowService.GetRunningApplications(),
                PieMenuMode.Launcher => GetLauncherItems(),
                PieMenuMode.Controller => GetControllerItems(),
                PieMenuMode.MusicRemote => GetMusicRemoteItems(),
                _ => new List<PieMenuItem>()
            };
        }

        private List<PieMenuItem> GetLauncherItems()
        {
            var items = new List<PieMenuItem>();
            foreach (var itemData in _settingsService.Settings.LauncherItems)
            {
                var item = new PieMenuItem
                {
                    Id = itemData.Id,
                    Name = itemData.Name,
                    Path = itemData.Path,
                    Type = itemData.Type,
                    Order = itemData.Order,
                    GroupItems = itemData.GroupItems
                };

                if (!string.IsNullOrEmpty(itemData.CustomIconPath))
                {
                    item.Icon = _windowService.GetIconFromFile(itemData.CustomIconPath);
                }
                else if (itemData.Type == PieMenuItemType.Group && itemData.GroupItems.Count > 0)
                {
                    item.Icon = _windowService.CreateStackedGroupIcon(
                        itemData.GroupItems.Select(g => g.Path));
                }
                else if (itemData.Type == PieMenuItemType.Application)
                {
                    item.Icon = _windowService.GetIconFromFile(itemData.Path);
                }
                else if (itemData.Type == PieMenuItemType.Folder)
                {
                    item.Icon = _windowService.GetFolderIcon();
                }
                else
                {
                    item.Icon = _windowService.GetIconFromFile(itemData.Path);
                }

                items.Add(item);
            }
            return items.OrderBy(i => i.Order).ToList();
        }

        private List<PieMenuItem> GetControllerItems()
        {
            // Use the stored original foreground window (captured when menu opened)
            // instead of current foreground (which is the pie menu itself)
            var foregroundProcess = _windowService.GetProcessNameFromWindow(_originalForegroundWindow);
            if (string.IsNullOrEmpty(foregroundProcess))
            {
                // Fallback to current foreground if original wasn't captured
                foregroundProcess = _windowService.GetForegroundProcessName();
            }
            if (string.IsNullOrEmpty(foregroundProcess)) return new List<PieMenuItem>();

            LogService.Debug($"Controller: Looking for shortcuts for process '{foregroundProcess}'");

            // 1. Try User Configuration first
            var config = _settingsService.Settings.ControllerConfigs
                .FirstOrDefault(c => c.ProcessName.Equals(foregroundProcess, StringComparison.OrdinalIgnoreCase));

            if (config != null)
            {
                var items = new List<PieMenuItem>();
                foreach (var action in config.Actions)
                {
                    var item = new PieMenuItem
                    {
                        Id = action.Id,
                        Name = action.Name,
                        KeyboardShortcut = action.KeyboardShortcut,
                        Type = PieMenuItemType.Action
                    };

                    if (!string.IsNullOrEmpty(action.IconPath))
                    {
                        item.Icon = _windowService.GetIconFromFile(action.IconPath);
                    }
                    else
                    {
                        // Use action name to generate contextual icon
                        item.Icon = Helpers.IconHelper.CreateActionIcon(action.Name);
                    }

                    items.Add(item);
                }
                return items;
            }

            // 2. Fallback to Presets
            var preset = _presetService.GetPresetForProcess(foregroundProcess);
            if (preset != null)
            {
                var items = new List<PieMenuItem>();
                foreach (var action in preset.Actions)
                {
                    var item = new PieMenuItem
                    {
                        Name = action.Name,
                        KeyboardShortcut = action.Shortcut,
                        Type = PieMenuItemType.Action,
                        Icon = Helpers.IconHelper.CreateActionIcon(action.Icon)
                    };
                    items.Add(item);
                }
                return items;
            }

            // 3. No config found - Return a "Configure" item
            return new List<PieMenuItem>
            {
                new PieMenuItem
                {
                    Name = $"Configure {foregroundProcess}",
                    Type = PieMenuItemType.Action,
                    Icon = Helpers.IconHelper.CreateActionIcon("settings"),
                    Id = "::configure_controller::",
                    ProcessName = foregroundProcess // Store process name for context
                }
            };
        }

        private List<PieMenuItem> GetMusicRemoteItems()
        {
            return new List<PieMenuItem>
            {
                new PieMenuItem { Name = "Previous", Type = PieMenuItemType.MediaControl, KeyboardShortcut = "previous", Icon = Helpers.IconHelper.CreateMediaIcon("previous") },
                new PieMenuItem { Name = "Play/Pause", Type = PieMenuItemType.MediaControl, KeyboardShortcut = "playpause", Icon = Helpers.IconHelper.CreateMediaIcon("playpause") },
                new PieMenuItem { Name = "Next", Type = PieMenuItemType.MediaControl, KeyboardShortcut = "next", Icon = Helpers.IconHelper.CreateMediaIcon("next") },
                new PieMenuItem { Name = "Volume Down", Type = PieMenuItemType.MediaControl, KeyboardShortcut = "volumedown", Icon = Helpers.IconHelper.CreateMediaIcon("volumedown") },
                new PieMenuItem { Name = "Mute", Type = PieMenuItemType.MediaControl, KeyboardShortcut = "mute", Icon = Helpers.IconHelper.CreateMediaIcon("mute") },
                new PieMenuItem { Name = "Volume Up", Type = PieMenuItemType.MediaControl, KeyboardShortcut = "volumeup", Icon = Helpers.IconHelper.CreateMediaIcon("volumeup") }
            };
        }

        private void PieMenuControl_ItemSelected(object? sender, PieMenuItem item)
        {
            // For Action items (Controller shortcuts), delay execution until menu is closed
            // and focus is restored to the original window
            if (item.Type == PieMenuItemType.Action && !string.IsNullOrEmpty(item.KeyboardShortcut))
            {
                _pendingActionItem = item;
                CloseMenu();
            }
            else
            {
                ExecuteItem(item);
                CloseMenu();
            }
        }

        private void ExecuteItem(PieMenuItem item)
        {
            switch (item.Type)
            {
                case PieMenuItemType.Application:
                    if (item.IsRunning && item.WindowHandle != IntPtr.Zero)
                    {
                        _windowService.SwitchToWindow(item.WindowHandle);
                    }
                    else if (!string.IsNullOrEmpty(item.Path))
                    {
                        LaunchApplication(item.Path);
                    }
                    break;

                case PieMenuItemType.Folder:
                case PieMenuItemType.File:
                    if (!string.IsNullOrEmpty(item.Path))
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = item.Path,
                            UseShellExecute = true
                        });
                    }
                    break;

                case PieMenuItemType.Action:
                    if (item.Id == "::configure_controller::")
                    {
                        SettingsRequested?.Invoke(this, EventArgs.Empty);
                    }
                    else if (!string.IsNullOrEmpty(item.KeyboardShortcut))
                    {
                        _keyboardService.SendKeyboardShortcut(item.KeyboardShortcut);
                    }
                    break;

                case PieMenuItemType.MediaControl:
                    ExecuteMediaControl(item.KeyboardShortcut);
                    break;

                case PieMenuItemType.Group:
                    foreach (var groupApp in item.GroupItems)
                    {
                        if (!string.IsNullOrEmpty(groupApp.Path))
                        {
                            LaunchApplication(groupApp.Path);
                        }
                    }
                    break;
            }
        }

        private void LaunchApplication(string path)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to launch application: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteMediaControl(string? control)
        {
            switch (control?.ToLowerInvariant())
            {
                case "playpause":
                    _mediaService.PlayPause();
                    break;
                case "next":
                    _mediaService.NextTrack();
                    break;
                case "previous":
                    _mediaService.PreviousTrack();
                    break;
                case "stop":
                    _mediaService.Stop();
                    break;
                case "volumeup":
                    _mediaService.VolumeUp();
                    break;
                case "volumedown":
                    _mediaService.VolumeDown();
                    break;
                case "mute":
                    _mediaService.ToggleMute();
                    break;
            }
        }

        private void PieMenuControl_MenuClosed(object? sender, EventArgs e)
        {
            LogService.Debug("MenuClosed event from PieMenuControl - calling CloseMenu");
            CloseMenu();
        }

        private void PieMenuWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Ignore keys pressed too soon after opening (debouncing)
            if (DateTime.Now < _ignoreClicksUntil)
            {
                LogService.Debug($"Ignoring key {e.Key} - within debounce window");
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Escape)
            {
                LogService.Debug("Escape pressed - closing menu");
                CloseMenu();
                e.Handled = true;
            }
            else if (e.Key == Key.Enter || e.Key == Key.Space)
            {
                LogService.Debug($"Key {e.Key} pressed - confirming selection");
                _pieMenuControl.ConfirmSelection();
                e.Handled = true;
            }
        }

        private void PieMenuWindow_MouseMove(object sender, MouseEventArgs e)
        {
            var pos = e.GetPosition(_pieMenuControl);
            _pieMenuControl.UpdateSelection(pos);
        }

        private void PieMenuWindow_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Ignore clicks too soon after opening (debouncing)
            if (DateTime.Now < _ignoreClicksUntil)
            {
                LogService.Debug("Ignoring MouseLeftButtonUp - within debounce window");
                e.Handled = true;
                return;
            }

            var pos = e.GetPosition(_pieMenuControl);
            var selectedItem = _pieMenuControl.GetSelectedItem();

            LogService.Debug($"MouseLeftButtonUp - selectedItem: {selectedItem?.Name ?? "null"}");

            if (selectedItem != null)
            {
                // Item selected - confirm and close
                _pieMenuControl.ConfirmSelection();
            }
            // If no item selected, the click was in the center or outside - handled by MouseLeftButtonDown
        }

        private void PieMenuWindow_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // Block middle mouse button from triggering any close logic
            if (e.ChangedButton == MouseButton.Middle)
            {
                LogService.Debug("Blocking middle mouse button event");
                e.Handled = true;
                return;
            }
        }

        private void PieMenuWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Ignore clicks too soon after opening (debouncing)
            if (DateTime.Now < _ignoreClicksUntil)
            {
                LogService.Debug("Ignoring MouseLeftButtonDown - within debounce window");
                e.Handled = true;
                return;
            }

            var pos = e.GetPosition(_pieMenuControl);
            double centerX = _pieMenuControl.ActualWidth / 2;
            double centerY = _pieMenuControl.ActualHeight / 2;
            double dx = pos.X - centerX;
            double dy = pos.Y - centerY;
            double distance = Math.Sqrt(dx * dx + dy * dy);

            // Calculate the outer radius of the menu ring (where icons are)
            double outerRadius = _settingsService.Settings.MenuRadius + _settingsService.Settings.IconSize / 2 + 10;
            double innerRadius = 50;

            LogService.Debug($"MouseLeftButtonDown at distance {distance:F1} from center (inner: {innerRadius}, outer: {outerRadius})");

            // Click outside the menu ring = close
            // Click in center (inside inner radius) = close
            if (distance > outerRadius || distance < innerRadius)
            {
                LogService.Debug("Click outside menu area, closing");
                CloseMenu();
                e.Handled = true;
            }
        }

        public void CloseMenu()
        {
            LogService.Debug($"CloseMenu called - IsClosing: {_isClosing}, IsVisible: {IsVisible}");

            if (_isClosing || !IsVisible)
            {
                LogService.Debug("CloseMenu early return - already closing or not visible");
                return;
            }

            _isClosing = true;
            LogService.Debug("Starting close animation");

            // Clear selection so nothing is selected while animating out
            _pieMenuControl.ClearSelection();

            _pieMenuControl.AnimateOut(() =>
            {
                Dispatcher.Invoke(() =>
                {
                    LogService.Debug($"AnimateOut callback - IsClosing: {_isClosing}");
                    // Only hide if we are still in the closing state.
                    // If ShowAtCursor() was called during animation, _isClosing would be false.
                    if (_isClosing)
                    {
                        Hide();
                        _isClosing = false;
                        LogService.Debug("Menu hidden successfully");


                        _pieMenuControl.SetItems(new List<PieMenuItem>());


                        ExecutePendingAction();
                    }
                    else
                    {
                        LogService.Debug("Menu not hidden - ShowAtCursor was called during animation");
                    }
                });
            });
        }

        private void ExecutePendingAction()
        {
            if (_pendingActionItem == null) return;

            var item = _pendingActionItem;
            _pendingActionItem = null;

            // Restore focus to the original window before sending the shortcut
            if (_originalForegroundWindow != IntPtr.Zero)
            {
                LogService.Debug($"Restoring focus to original window: {_originalForegroundWindow}");
                SetForegroundWindow(_originalForegroundWindow);

                // Small delay to ensure the window is focused before sending keys
                System.Threading.Tasks.Task.Delay(50).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (!string.IsNullOrEmpty(item.KeyboardShortcut))
                        {
                            LogService.Debug($"Sending keyboard shortcut: {item.KeyboardShortcut}");
                            _keyboardService.SendKeyboardShortcut(item.KeyboardShortcut);
                        }
                    });
                });
            }
            else
            {
                // No original window, just send the shortcut
                if (!string.IsNullOrEmpty(item.KeyboardShortcut))
                {
                    _keyboardService.SendKeyboardShortcut(item.KeyboardShortcut);
                }
            }
        }

        public void CycleMode()
        {
            var flow = _settingsService.Settings.RightClickFlow;
            PieMenuMode nextMode;

            if (flow.TryGetValue(_currentMode, out var targetMode))
            {
                nextMode = targetMode;
            }
            else
            {
                // Fallback if not configured
                var modes = Enum.GetValues<PieMenuMode>();
                int currentIndex = Array.IndexOf(modes, _currentMode);
                int nextIndex = (currentIndex + 1) % modes.Length;
                nextMode = modes[nextIndex];
            }

            LogService.Debug($"Cycling mode from {_currentMode} to {nextMode}");
            ShowAtCursor(nextMode);
        }
    }
}
