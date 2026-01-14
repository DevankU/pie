using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using Pie.Models;
using Pie.Services;

namespace Pie.Views
{
    public partial class SettingsWindow : Window
    {
        private readonly SettingsService _settingsService;
        private readonly WindowService _windowService;
        private string _currentTab = "General";
        private bool _isCapturingHotkey;
        private TextBox? _hotkeyTextBox;

        public SettingsWindow(SettingsService settingsService, WindowService windowService)
        {
            _settingsService = settingsService;
            _windowService = windowService;
            InitializeComponent();

            // Attach handlers safely after initialization
            GeneralTab.Checked += Tab_Checked;
            SwitcherTab.Checked += Tab_Checked;
            LauncherTab.Checked += Tab_Checked;
            ControllerTab.Checked += Tab_Checked;
            MusicTab.Checked += Tab_Checked;
            AboutTab.Checked += Tab_Checked;

            LoadGeneralSettings();
        }

        private void Tab_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.Content != null)
            {
                _currentTab = rb.Content.ToString() ?? "General";
                LoadTabContent();
            }
        }

        private void LoadTabContent()
        {
            ContentPanel.Children.Clear();
            switch (_currentTab)
            {
                case "General":
                    LoadGeneralSettings();
                    break;
                case "Switcher":
                    LoadSwitcherSettings();
                    break;
                case "Launcher":
                    LoadLauncherSettings();
                    break;
                case "Controller":
                    LoadControllerSettings();
                    break;
                case "Music Remote":
                    LoadMusicSettings();
                    break;
                case "About":
                    LoadAboutContent();
                    break;
            }
        }

        private void LoadGeneralSettings()
        {
            ContentPanel.Children.Clear();
            var settings = _settingsService.Settings;

            AddHeader("Activation");

            // Hotkey setting
            var hotkeyPanel = CreateSettingRow("Keyboard Shortcut", "Press to activate the pie menu");
            _hotkeyTextBox = new TextBox
            {
                Text = settings.ActivationHotkey,
                Width = 200,
                Padding = new Thickness(8, 6, 8, 6),
                FontSize = 13,
                IsReadOnly = true,
                Cursor = Cursors.Hand,
                Background = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                BorderThickness = new Thickness(1)
            };
            _hotkeyTextBox.PreviewKeyDown += HotkeyTextBox_PreviewKeyDown;
            _hotkeyTextBox.GotFocus += (s, e) => { _isCapturingHotkey = true; _hotkeyTextBox.Text = "Press keys..."; };
            _hotkeyTextBox.LostFocus += (s, e) => { _isCapturingHotkey = false; if (_hotkeyTextBox.Text == "Press keys...") _hotkeyTextBox.Text = settings.ActivationHotkey; };
            hotkeyPanel.Children.Add(_hotkeyTextBox);
            ContentPanel.Children.Add(hotkeyPanel);

            // Long press duration
            var longPressPanel = CreateSettingRow("Right-Click Hold Duration", "Hold right mouse button to activate (milliseconds)");
            var longPressSlider = new Slider
            {
                Width = 200,
                Minimum = 500,
                Maximum = 3000,
                Value = 2000,
                TickFrequency = 500,
                IsSnapToTickEnabled = true
            };
            var longPressLabel = new TextBlock { Text = "2000ms", Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            longPressSlider.ValueChanged += (s, e) => longPressLabel.Text = $"{(int)e.NewValue}ms";
            var longPressValuePanel = new StackPanel { Orientation = Orientation.Horizontal };
            longPressValuePanel.Children.Add(longPressSlider);
            longPressValuePanel.Children.Add(longPressLabel);
            longPressPanel.Children.Add(longPressValuePanel);
            ContentPanel.Children.Add(longPressPanel);

            // Default mode
            AddHeader("Default Mode");
            var modePanel = CreateSettingRow("Mode on Activation", "Which mode to show when activated");
            var modeCombo = new ComboBox
            {
                Width = 200,
                Padding = new Thickness(8, 6, 8, 6),
                SelectedIndex = (int)settings.DefaultMode
            };
            modeCombo.Items.Add("Switcher");
            modeCombo.Items.Add("Launcher");
            modeCombo.Items.Add("Controller");
            modeCombo.Items.Add("Music Remote");
            modeCombo.SelectionChanged += (s, e) =>
            {
                if (modeCombo.SelectedIndex >= 0)
                {
                    _settingsService.UpdateSettings(s => s.DefaultMode = (PieMenuMode)modeCombo.SelectedIndex);
                }
            };
            modePanel.Children.Add(modeCombo);
            ContentPanel.Children.Add(modePanel);

            AddHeader("Appearance");

            // Menu radius
            var radiusPanel = CreateSettingRow("Menu Size", "Adjust the size of the pie menu");
            var radiusSlider = new Slider
            {
                Width = 200,
                Minimum = 120,
                Maximum = 280,
                Value = settings.MenuRadius,
                TickFrequency = 20,
                IsSnapToTickEnabled = true
            };
            radiusSlider.ValueChanged += (s, e) => _settingsService.UpdateSettings(s => s.MenuRadius = e.NewValue);
            radiusPanel.Children.Add(radiusSlider);
            ContentPanel.Children.Add(radiusPanel);

            // Icon size
            var iconPanel = CreateSettingRow("Icon Size", "Size of app icons in the menu");
            var iconSlider = new Slider
            {
                Width = 200,
                Minimum = 32,
                Maximum = 72,
                Value = settings.IconSize,
                TickFrequency = 8,
                IsSnapToTickEnabled = true
            };
            iconSlider.ValueChanged += (s, e) => _settingsService.UpdateSettings(s => s.IconSize = e.NewValue);
            iconPanel.Children.Add(iconSlider);
            ContentPanel.Children.Add(iconPanel);

            AddHeader("Sound");

            // Sound toggle
            var soundPanel = CreateSettingRow("Enable Sounds", "Play sounds when interacting with the menu");
            var soundToggle = new CheckBox
            {
                IsChecked = settings.SoundsEnabled,
                VerticalAlignment = VerticalAlignment.Center
            };
            soundToggle.Checked += (s, e) => _settingsService.UpdateSettings(s => s.SoundsEnabled = true);
            soundToggle.Unchecked += (s, e) => _settingsService.UpdateSettings(s => s.SoundsEnabled = false);
            soundPanel.Children.Add(soundToggle);
            ContentPanel.Children.Add(soundPanel);

            // Volume
            var volumePanel = CreateSettingRow("Volume", "Adjust sound volume");
            var volumeSlider = new Slider
            {
                Width = 200,
                Minimum = 0,
                Maximum = 1,
                Value = settings.SoundVolume,
                TickFrequency = 0.1,
                IsSnapToTickEnabled = true
            };
            volumeSlider.ValueChanged += (s, e) => _settingsService.UpdateSettings(s => s.SoundVolume = e.NewValue);
            volumePanel.Children.Add(volumeSlider);
            ContentPanel.Children.Add(volumePanel);

            AddHeader("System");

            // Launch at startup
            var startupPanel = CreateSettingRow("Launch at Startup", "Start Pie when Windows starts");
            var startupToggle = new CheckBox
            {
                IsChecked = settings.LaunchAtStartup,
                VerticalAlignment = VerticalAlignment.Center
            };
            startupToggle.Checked += (s, e) => { _settingsService.UpdateSettings(s => s.LaunchAtStartup = true); SetStartupRegistry(true); };
            startupToggle.Unchecked += (s, e) => { _settingsService.UpdateSettings(s => s.LaunchAtStartup = false); SetStartupRegistry(false); };
            startupPanel.Children.Add(startupToggle);
            ContentPanel.Children.Add(startupPanel);
        }

        private void LoadSwitcherSettings()
        {
            ContentPanel.Children.Clear();
            var settings = _settingsService.Settings;

            AddHeader("Excluded Applications");
            AddDescription("These applications will not appear in the Switcher mode.");

            var excludedList = new ListBox
            {
                Height = 200,
                Margin = new Thickness(0, 8, 0, 8),
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200))
            };

            foreach (var app in settings.ExcludedApps)
            {
                excludedList.Items.Add(app);
            }

            ContentPanel.Children.Add(excludedList);

            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };

            var addInput = new TextBox
            {
                Width = 200,
                Padding = new Thickness(8, 6, 8, 6),
                Margin = new Thickness(0, 0, 8, 0)
            };
            buttonPanel.Children.Add(addInput);

            var addBtn = new Button { Content = "Add", Style = FindResource("ModernButtonStyle") as Style };
            addBtn.Click += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(addInput.Text) && !settings.ExcludedApps.Contains(addInput.Text))
                {
                    settings.ExcludedApps.Add(addInput.Text);
                    excludedList.Items.Add(addInput.Text);
                    _settingsService.SaveSettings();
                    addInput.Clear();
                }
            };
            buttonPanel.Children.Add(addBtn);

            var removeBtn = new Button { Content = "Remove Selected", Style = FindResource("SecondaryButtonStyle") as Style, Margin = new Thickness(8, 0, 0, 0) };
            removeBtn.Click += (s, e) =>
            {
                if (excludedList.SelectedItem != null)
                {
                    var item = excludedList.SelectedItem.ToString();
                    if (item != null)
                    {
                        settings.ExcludedApps.Remove(item);
                        excludedList.Items.Remove(excludedList.SelectedItem);
                        _settingsService.SaveSettings();
                    }
                }
            };
            buttonPanel.Children.Add(removeBtn);

            ContentPanel.Children.Add(buttonPanel);
        }

        private void LoadLauncherSettings()
        {
            ContentPanel.Children.Clear();
            var settings = _settingsService.Settings;

            AddHeader("Launcher Items");
            AddDescription("Add applications, folders, and files to your launcher menu.");

            var itemsList = new ListBox
            {
                Height = 250,
                Margin = new Thickness(0, 8, 0, 8),
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200))
            };

            RefreshLauncherList(itemsList);

            ContentPanel.Children.Add(itemsList);

            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };

            var addAppBtn = new Button { Content = "Add Application", Style = FindResource("ModernButtonStyle") as Style };
            addAppBtn.Click += (s, e) =>
            {
                var dialog = new OpenFileDialog
                {
                    Filter = "Applications (*.exe)|*.exe|All Files (*.*)|*.*",
                    Title = "Select Application"
                };
                if (dialog.ShowDialog() == true)
                {
                    var newItem = new PieMenuItemData
                    {
                        Name = Path.GetFileNameWithoutExtension(dialog.FileName),
                        Path = dialog.FileName,
                        Type = PieMenuItemType.Application,
                        Order = settings.LauncherItems.Count
                    };
                    settings.LauncherItems.Add(newItem);
                    _settingsService.SaveSettings();
                    RefreshLauncherList(itemsList);
                }
            };
            buttonPanel.Children.Add(addAppBtn);

            var addFolderBtn = new Button { Content = "Add Folder", Style = FindResource("SecondaryButtonStyle") as Style, Margin = new Thickness(8, 0, 0, 0) };
            addFolderBtn.Click += (s, e) =>
            {
                var dialog = new System.Windows.Forms.FolderBrowserDialog();
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var newItem = new PieMenuItemData
                    {
                        Name = Path.GetFileName(dialog.SelectedPath),
                        Path = dialog.SelectedPath,
                        Type = PieMenuItemType.Folder,
                        Order = settings.LauncherItems.Count
                    };
                    settings.LauncherItems.Add(newItem);
                    _settingsService.SaveSettings();
                    RefreshLauncherList(itemsList);
                }
            };
            buttonPanel.Children.Add(addFolderBtn);

            var browseInstalledBtn = new Button { Content = "Browse Installed", Style = FindResource("SecondaryButtonStyle") as Style, Margin = new Thickness(8, 0, 0, 0) };
            browseInstalledBtn.Click += (s, e) =>
            {
                var picker = new InstalledAppsPickerWindow { Owner = this };
                if (picker.ShowDialog() == true && picker.SelectedApp != null)
                {
                    var app = picker.SelectedApp;
                    var newItem = new PieMenuItemData
                    {
                        Name = app.Name,
                        Path = app.Path,
                        Type = PieMenuItemType.Application,
                        Order = settings.LauncherItems.Count
                    };
                    settings.LauncherItems.Add(newItem);
                    _settingsService.SaveSettings();
                    RefreshLauncherList(itemsList);
                }
            };
            buttonPanel.Children.Add(browseInstalledBtn);

            var removeBtn = new Button { Content = "Remove", Style = FindResource("SecondaryButtonStyle") as Style, Margin = new Thickness(8, 0, 0, 0) };
            removeBtn.Click += (s, e) =>
            {
                if (itemsList.SelectedItem is ListBoxItem selectedItem && selectedItem.Tag is string id)
                {
                    var item = settings.LauncherItems.FirstOrDefault(i => i.Id == id);
                    if (item != null)
                    {
                        settings.LauncherItems.Remove(item);
                        _settingsService.SaveSettings();
                        RefreshLauncherList(itemsList);
                    }
                }
            };
            buttonPanel.Children.Add(removeBtn);

            ContentPanel.Children.Add(buttonPanel);

            // Move buttons
            var movePanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
            var moveUpBtn = new Button { Content = "Move Up", Style = FindResource("SecondaryButtonStyle") as Style };
            moveUpBtn.Click += (s, e) => MoveItem(itemsList, -1);
            movePanel.Children.Add(moveUpBtn);

            var moveDownBtn = new Button { Content = "Move Down", Style = FindResource("SecondaryButtonStyle") as Style, Margin = new Thickness(8, 0, 0, 0) };
            moveDownBtn.Click += (s, e) => MoveItem(itemsList, 1);
            movePanel.Children.Add(moveDownBtn);

            ContentPanel.Children.Add(movePanel);
        }

        private void RefreshLauncherList(ListBox itemsList)
        {
            itemsList.Items.Clear();
            foreach (var item in _settingsService.Settings.LauncherItems.OrderBy(i => i.Order))
            {
                var listItem = new ListBoxItem
                {
                    Content = $"{item.Name} ({item.Type})",
                    Tag = item.Id,
                    Padding = new Thickness(8, 4, 8, 4)
                };
                itemsList.Items.Add(listItem);
            }
        }

        private void MoveItem(ListBox itemsList, int direction)
        {
            if (itemsList.SelectedItem is not ListBoxItem selectedItem || selectedItem.Tag is not string id) return;

            var items = _settingsService.Settings.LauncherItems.OrderBy(i => i.Order).ToList();
            var item = items.FirstOrDefault(i => i.Id == id);
            if (item == null) return;

            int currentIndex = items.IndexOf(item);
            int newIndex = currentIndex + direction;

            if (newIndex < 0 || newIndex >= items.Count) return;

            // Swap orders
            var otherItem = items[newIndex];
            int tempOrder = item.Order;
            item.Order = otherItem.Order;
            otherItem.Order = tempOrder;

            _settingsService.SaveSettings();
            RefreshLauncherList(itemsList);
            itemsList.SelectedIndex = newIndex;
        }

        private void LoadControllerSettings()
        {
            ContentPanel.Children.Clear();

            AddHeader("Application Controllers");
            AddDescription("Configure keyboard shortcuts for specific applications. The controller menu will show these shortcuts when that app is active.");

            var appCombo = new ComboBox
            {
                Width = 300,
                Padding = new Thickness(8, 6, 8, 6),
                Margin = new Thickness(0, 8, 0, 8)
            };

            // Populate with existing configs and option to add new
            foreach (var config in _settingsService.Settings.ControllerConfigs)
            {
                appCombo.Items.Add(config.AppName);
            }
            appCombo.Items.Add("+ Add New Application");

            ContentPanel.Children.Add(appCombo);

            var actionsPanel = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };
            ContentPanel.Children.Add(actionsPanel);

            appCombo.SelectionChanged += (s, e) =>
            {
                if (appCombo.SelectedItem?.ToString() == "+ Add New Application")
                {
                    ShowAddAppDialog(appCombo, actionsPanel);
                }
                else if (appCombo.SelectedIndex >= 0 && appCombo.SelectedIndex < _settingsService.Settings.ControllerConfigs.Count)
                {
                    LoadControllerActions(actionsPanel, _settingsService.Settings.ControllerConfigs[appCombo.SelectedIndex]);
                }
            };

            if (appCombo.Items.Count > 1)
            {
                appCombo.SelectedIndex = 0;
            }
        }

        private void ShowAddAppDialog(ComboBox appCombo, StackPanel actionsPanel)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Applications (*.exe)|*.exe",
                Title = "Select Application"
            };

            if (dialog.ShowDialog() == true)
            {
                var processName = Path.GetFileNameWithoutExtension(dialog.FileName);
                var appName = processName;

                var newConfig = new AppControllerConfig
                {
                    ProcessName = processName,
                    AppName = appName,
                    Actions = new List<ControllerAction>()
                };

                _settingsService.Settings.ControllerConfigs.Add(newConfig);
                _settingsService.SaveSettings();

                appCombo.Items.Insert(appCombo.Items.Count - 1, appName);
                appCombo.SelectedIndex = appCombo.Items.Count - 2;
            }
            else
            {
                appCombo.SelectedIndex = -1;
            }
        }

        private void LoadControllerActions(StackPanel actionsPanel, AppControllerConfig config)
        {
            actionsPanel.Children.Clear();

            var actionsList = new ListBox
            {
                Height = 200,
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200))
            };

            RefreshActionsList(actionsList, config);
            actionsPanel.Children.Add(actionsList);

            // Add action form
            var formPanel = new StackPanel { Margin = new Thickness(0, 16, 0, 0) };

            var nameRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 4) };
            nameRow.Children.Add(new TextBlock { Text = "Action Name:", Width = 120, VerticalAlignment = VerticalAlignment.Center });
            var nameInput = new TextBox { Width = 200, Padding = new Thickness(8, 6, 8, 6) };
            nameRow.Children.Add(nameInput);
            formPanel.Children.Add(nameRow);

            var shortcutRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 4) };
            shortcutRow.Children.Add(new TextBlock { Text = "Shortcut:", Width = 120, VerticalAlignment = VerticalAlignment.Center });
            var shortcutInput = new TextBox { Width = 200, Padding = new Thickness(8, 6, 8, 6), IsReadOnly = true };
            shortcutInput.PreviewKeyDown += (s, e) =>
            {
                e.Handled = true;
                var modifiers = new List<string>();
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) modifiers.Add("Ctrl");
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) modifiers.Add("Shift");
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt)) modifiers.Add("Alt");

                var key = e.Key == Key.System ? e.SystemKey : e.Key;
                if (key != Key.LeftCtrl && key != Key.RightCtrl &&
                    key != Key.LeftShift && key != Key.RightShift &&
                    key != Key.LeftAlt && key != Key.RightAlt &&
                    key != Key.LWin && key != Key.RWin)
                {
                    modifiers.Add(key.ToString());
                }
                shortcutInput.Text = string.Join("+", modifiers);
            };
            shortcutRow.Children.Add(shortcutInput);
            formPanel.Children.Add(shortcutRow);

            var addActionBtn = new Button { Content = "Add Action", Style = FindResource("ModernButtonStyle") as Style, Margin = new Thickness(0, 8, 0, 0) };
            addActionBtn.Click += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(nameInput.Text) && !string.IsNullOrWhiteSpace(shortcutInput.Text))
                {
                    config.Actions.Add(new ControllerAction
                    {
                        Name = nameInput.Text,
                        KeyboardShortcut = shortcutInput.Text
                    });
                    _settingsService.SaveSettings();
                    RefreshActionsList(actionsList, config);
                    nameInput.Clear();
                    shortcutInput.Clear();
                }
            };
            formPanel.Children.Add(addActionBtn);

            var removeActionBtn = new Button { Content = "Remove Selected", Style = FindResource("SecondaryButtonStyle") as Style, Margin = new Thickness(0, 8, 0, 0) };
            removeActionBtn.Click += (s, e) =>
            {
                if (actionsList.SelectedItem is ListBoxItem item && item.Tag is string actionId)
                {
                    var action = config.Actions.FirstOrDefault(a => a.Id == actionId);
                    if (action != null)
                    {
                        config.Actions.Remove(action);
                        _settingsService.SaveSettings();
                        RefreshActionsList(actionsList, config);
                    }
                }
            };
            formPanel.Children.Add(removeActionBtn);

            actionsPanel.Children.Add(formPanel);
        }

        private void RefreshActionsList(ListBox actionsList, AppControllerConfig config)
        {
            actionsList.Items.Clear();
            foreach (var action in config.Actions)
            {
                actionsList.Items.Add(new ListBoxItem
                {
                    Content = $"{action.Name} - {action.KeyboardShortcut}",
                    Tag = action.Id,
                    Padding = new Thickness(8, 4, 8, 4)
                });
            }
        }

        private void LoadMusicSettings()
        {
            ContentPanel.Children.Clear();

            AddHeader("Music Remote");
            AddDescription("Control your music playback with the pie menu. Supports all media players that respond to Windows media keys.");

            var infoPanel = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(230, 240, 255)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(180, 200, 230)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 16, 0, 0)
            };

            var infoText = new TextBlock
            {
                Text = "The Music Remote mode provides these controls:\n\n" +
                       "• Play/Pause - Toggle playback\n" +
                       "• Next Track - Skip to next song\n" +
                       "• Previous Track - Go to previous song\n" +
                       "• Volume Up/Down - Adjust volume\n" +
                       "• Mute - Toggle mute\n\n" +
                       "These controls work with Spotify, Apple Music, Windows Media Player, and most other media applications.",
                TextWrapping = TextWrapping.Wrap,
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(60, 80, 120))
            };

            infoPanel.Child = infoText;
            ContentPanel.Children.Add(infoPanel);
        }

        private void LoadAboutContent()
        {
            ContentPanel.Children.Clear();

            var logo = new TextBlock
            {
                Text = "Pie",
                FontSize = 32,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 8)
            };
            ContentPanel.Children.Add(logo);

            var version = new TextBlock
            {
                Text = "Version 1.0.0",
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(128, 128, 128)),
                Margin = new Thickness(0, 0, 0, 24)
            };
            ContentPanel.Children.Add(version);

            var description = new TextBlock
            {
                Text = "A radial pie menu launcher for Windows. Quickly switch between apps, launch favorites, control your music, and trigger keyboard shortcuts with a beautiful, intuitive interface.",
                TextWrapping = TextWrapping.Wrap,
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 24)
            };
            ContentPanel.Children.Add(description);

            var featuresHeader = new TextBlock
            {
                Text = "Features",
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 8)
            };
            ContentPanel.Children.Add(featuresHeader);

            var features = new TextBlock
            {
                Text = "• Switcher Mode - Jump between running applications\n" +
                       "• Launcher Mode - Quick access to favorite apps and folders\n" +
                       "• Controller Mode - App-specific keyboard shortcuts\n" +
                       "• Music Remote - Control media playback\n" +
                       "• Keyboard hotkey (Ctrl+Space) or touchpad gesture activation\n" +
                       "• Smooth macOS-style animations",
                TextWrapping = TextWrapping.Wrap,
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 24)
            };
            ContentPanel.Children.Add(features);
        }

        private void AddHeader(string text)
        {
            ContentPanel.Children.Add(new TextBlock
            {
                Text = text,
                Style = FindResource("SectionHeaderStyle") as Style
            });
        }

        private void AddDescription(string text)
        {
            ContentPanel.Children.Add(new TextBlock
            {
                Text = text,
                Style = FindResource("DescriptionStyle") as Style
            });
        }

        private StackPanel CreateSettingRow(string label, string description)
        {
            var panel = new StackPanel { Margin = new Thickness(0, 8, 0, 8) };
            panel.Children.Add(new TextBlock { Text = label, FontSize = 14, FontWeight = FontWeights.Medium });
            panel.Children.Add(new TextBlock { Text = description, Style = FindResource("DescriptionStyle") as Style, Margin = new Thickness(0, 2, 0, 8) });
            return panel;
        }

        private void HotkeyTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!_isCapturingHotkey || _hotkeyTextBox == null) return;

            e.Handled = true;

            var modifiers = new List<string>();
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) modifiers.Add("Ctrl");
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) modifiers.Add("Shift");
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt)) modifiers.Add("Alt");
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Windows)) modifiers.Add("Win");

            var key = e.Key == Key.System ? e.SystemKey : e.Key;
            if (key != Key.LeftCtrl && key != Key.RightCtrl &&
                key != Key.LeftShift && key != Key.RightShift &&
                key != Key.LeftAlt && key != Key.RightAlt &&
                key != Key.LWin && key != Key.RWin)
            {
                modifiers.Add(key.ToString());
                var hotkeyString = string.Join("+", modifiers);
                _hotkeyTextBox.Text = hotkeyString;
                _settingsService.UpdateSettings(s => s.ActivationHotkey = hotkeyString);

                var (parsedKey, parsedModifiers) = KeyboardService.ParseHotkey(hotkeyString);
                _settingsService.UpdateSettings(s =>
                {
                    s.ActivationKey = parsedKey;
                    s.ActivationModifiers = parsedModifiers;
                });

                _isCapturingHotkey = false;
                Keyboard.ClearFocus();
            }
        }

        private void SetStartupRegistry(bool enable)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                if (key == null) return;

                if (enable)
                {
                    var exePath = Process.GetCurrentProcess().MainModule?.FileName;
                    if (exePath != null)
                    {
                        key.SetValue("Pie", $"\"{exePath}\"");
                    }
                }
                else
                {
                    key.DeleteValue("Pie", false);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to update startup settings: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
