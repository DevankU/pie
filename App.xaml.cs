using System;
using System.Threading;
using System.Windows;
using H.NotifyIcon;
using Pie.Models;
using Pie.Services;
using Pie.Views;

namespace Pie
{
    public partial class App : Application
    {
        private static Mutex? _mutex;
        private TaskbarIcon? _trayIcon;
        private SettingsService _settingsService = null!;
        private WindowService _windowService = null!;
        private HotkeyService _hotkeyService = null!;
        private MouseTriggerService _mouseTriggerService = null!;
        private KeyboardService _keyboardService = null!;
        private MediaService _mediaService = null!;
        private SoundService _soundService = null!;
        private PieMenuWindow _pieMenuWindow = null!;
        private SettingsWindow? _settingsWindow;
        private Window _hiddenWindow = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            LogService.Info("Pie starting...");
            LogService.Debug($"Log file: {LogService.GetLogFilePath()}");
            LogService.CleanOldLogs();

            // Single instance check
            _mutex = new Mutex(true, "PieMutex", out bool createdNew);
            if (!createdNew)
            {
                LogService.Warning("Another instance is already running. Exiting.");
                MessageBox.Show("Pie is already running.", "Pie", MessageBoxButton.OK, MessageBoxImage.Information);
                Shutdown();
                return;
            }

            try
            {
                InitializeServices();
                InitializeTrayIcon();
                InitializeHiddenWindow();
                InitializePieMenuWindow();
                RegisterHotkey();
                StartMouseTrigger();
                LogService.Info("Pie started successfully.");
            }
            catch (Exception ex)
            {
                LogService.Error("Failed to start Pie", ex);
                MessageBox.Show($"Failed to start: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        private void InitializeServices()
        {
            LogService.Debug("Initializing services...");
            _settingsService = new SettingsService();
            _windowService = new WindowService(_settingsService);
            _hotkeyService = new HotkeyService();
            _mouseTriggerService = new MouseTriggerService();
            _keyboardService = new KeyboardService();
            _mediaService = new MediaService();
            _soundService = new SoundService(_settingsService);
        }

        private void InitializeTrayIcon()
        {
            // Create context menu
            var contextMenu = new System.Windows.Controls.ContextMenu();

            var switcherItem = new System.Windows.Controls.MenuItem { Header = "Switcher Mode" };
            switcherItem.Click += (s, e) => ShowPieMenu(PieMenuMode.Switcher);
            contextMenu.Items.Add(switcherItem);

            var launcherItem = new System.Windows.Controls.MenuItem { Header = "Launcher Mode" };
            launcherItem.Click += (s, e) => ShowPieMenu(PieMenuMode.Launcher);
            contextMenu.Items.Add(launcherItem);

            var controllerItem = new System.Windows.Controls.MenuItem { Header = "Controller Mode" };
            controllerItem.Click += (s, e) => ShowPieMenu(PieMenuMode.Controller);
            contextMenu.Items.Add(controllerItem);

            var musicItem = new System.Windows.Controls.MenuItem { Header = "Music Remote" };
            musicItem.Click += (s, e) => ShowPieMenu(PieMenuMode.MusicRemote);
            contextMenu.Items.Add(musicItem);

            contextMenu.Items.Add(new System.Windows.Controls.Separator());

            var settingsItem = new System.Windows.Controls.MenuItem { Header = "Settings" };
            settingsItem.Click += (s, e) => ShowSettings();
            contextMenu.Items.Add(settingsItem);

            contextMenu.Items.Add(new System.Windows.Controls.Separator());

            var exitItem = new System.Windows.Controls.MenuItem { Header = "Exit" };
            exitItem.Click += (s, e) => ExitApplication();
            contextMenu.Items.Add(exitItem);

            _trayIcon = new TaskbarIcon
            {
                ToolTipText = "Pie - Right-click for options",
                ContextMenu = contextMenu,
                Icon = CreateTrayIcon()
            };

            _trayIcon.ForceCreate();
            _trayIcon.TrayMouseDoubleClick += (s, e) => ShowSettings();
        }

        private System.Drawing.Icon CreateTrayIcon()
        {
            using var bitmap = new System.Drawing.Bitmap(32, 32);
            using var graphics = System.Drawing.Graphics.FromImage(bitmap);
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            using var brush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(0, 122, 255));
            graphics.FillEllipse(brush, 2, 2, 28, 28);

            using var whiteBrush = new System.Drawing.SolidBrush(System.Drawing.Color.White);
            graphics.FillEllipse(whiteBrush, 10, 10, 12, 12);

            return System.Drawing.Icon.FromHandle(bitmap.GetHicon());
        }

        private void InitializeHiddenWindow()
        {
            _hiddenWindow = new Window
            {
                Width = 0,
                Height = 0,
                WindowStyle = WindowStyle.None,
                ShowInTaskbar = false,
                ShowActivated = false
            };
            _hiddenWindow.Show();
            _hiddenWindow.Hide();
        }

        private void InitializePieMenuWindow()
        {
            _pieMenuWindow = new PieMenuWindow(
                _settingsService,
                _windowService,
                _keyboardService,
                _mediaService,
                _soundService);
        }

        private DateTime _lastHotkeyTime = DateTime.MinValue;

        private void RegisterHotkey()
        {
            _hotkeyService.HotkeyPressed += (s, e) =>
            {
                var now = DateTime.Now;
                if ((now - _lastHotkeyTime).TotalMilliseconds < 500)
                {
                    LogService.Debug("Hotkey pressed - ignoring (within 500ms debounce)");
                    return;
                }
                _lastHotkeyTime = now;

                Dispatcher.Invoke(() =>
                {
                    LogService.Debug($"Hotkey pressed - IsVisible: {_pieMenuWindow.IsVisible}, IsClosing: {_pieMenuWindow.IsClosing}");
                    if (!_pieMenuWindow.IsVisible || _pieMenuWindow.IsClosing)
                    {
                        LogService.Debug("Hotkey showing menu");
                        ShowPieMenu(_settingsService.Settings.DefaultMode);
                    }
                    else if (DateTime.Now >= _pieMenuWindow.CanToggleCloseAfter)
                    {
                        LogService.Debug("Hotkey closing menu (after toggle timeout)");
                        _pieMenuWindow.CloseMenu();
                    }
                    else
                    {
                        LogService.Debug("Hotkey ignored - menu visible but within 700ms toggle timeout");
                    }
                });
            };

            var settings = _settingsService.Settings;
            _hotkeyService.Register(_hiddenWindow, settings.ActivationKey, settings.ActivationModifiers);

            _settingsService.SettingsChanged += (s, e) =>
            {
                var newSettings = _settingsService.Settings;
                _hotkeyService.RegisterHotkey(newSettings.ActivationKey, newSettings.ActivationModifiers);
            };
        }

        private void StartMouseTrigger()
        {
            _mouseTriggerService.MiddleButtonTriggered += (s, e) =>
            {
                Dispatcher.Invoke(() =>
                {
                    LogService.Debug($"Middle button handler - IsVisible: {_pieMenuWindow.IsVisible}, IsClosing: {_pieMenuWindow.IsClosing}");

                    if (!_pieMenuWindow.IsVisible || _pieMenuWindow.IsClosing)
                    {
                        LogService.Debug("Middle mouse trigger - showing pie menu");
                        ShowPieMenu(_settingsService.Settings.DefaultMode);
                    }
                    else if (DateTime.Now >= _pieMenuWindow.CanToggleCloseAfter)
                    {
                        LogService.Debug("Middle mouse trigger - closing menu (after toggle timeout)");
                        _pieMenuWindow.CloseMenu();
                    }
                    else
                    {
                        LogService.Debug("Middle mouse trigger - menu visible but within 700ms toggle timeout, ignoring");
                    }
                });
            };
            _mouseTriggerService.Start();
            LogService.Info("Mouse trigger service started (Middle Button)");
        }

        private void ShowPieMenu(PieMenuMode mode)
        {
            _pieMenuWindow.ShowAtCursor(mode);
        }

        private void ShowSettings()
        {
            if (_settingsWindow != null && _settingsWindow.IsVisible)
            {
                _settingsWindow.Activate();
                return;
            }

            _settingsWindow = new SettingsWindow(_settingsService, _windowService);
            _settingsWindow.Closed += (s, e) => _settingsWindow = null;
            _settingsWindow.Show();
        }

        private void ExitApplication()
        {
            _hotkeyService.Dispose();
            _mouseTriggerService.Dispose();
            _trayIcon?.Dispose();
            _mutex?.ReleaseMutex();
            _mutex?.Dispose();
            Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _hotkeyService?.Dispose();
            _mouseTriggerService?.Dispose();
            _trayIcon?.Dispose();
            base.OnExit(e);
        }
    }
}
