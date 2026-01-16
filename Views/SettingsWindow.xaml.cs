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
using Pie.Controls;
using Pie.Helpers;

namespace Pie.Views
{
    public partial class SettingsWindow : Window
    {
        private readonly SettingsService _settingsService;
        private readonly WindowService _windowService;
        private readonly PresetService _presetService;
        private string _currentTab = "General";
        private bool _isCapturingHotkey;
        private TextBox? _hotkeyTextBox;
        private PieMenuControl? _previewControl;

        public SettingsWindow(SettingsService settingsService, WindowService windowService, PresetService presetService)
        {
            _settingsService = settingsService;
            _windowService = windowService;
            _presetService = presetService;
            InitializeComponent();

            // Attach handlers safely after initialization
            GeneralTab.Checked += Tab_Checked;
            SwitcherTab.Checked += Tab_Checked;
            LauncherTab.Checked += Tab_Checked;
            ControllerTab.Checked += Tab_Checked;
            MusicTab.Checked += Tab_Checked;
            GesturesTab.Checked += Tab_Checked;
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
                case "Gestures":
                    LoadGesturesSettings();
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

            // Double-tap settings
            AddHeader("Double-Tap (3-Finger Gesture)");

            var doubleTapPanel = CreateSettingRow("Enable Double-Tap", "Double-tap 3-finger gesture to open a specific mode");
            var doubleTapToggle = new CheckBox
            {
                IsChecked = settings.DoubleTapEnabled,
                VerticalAlignment = VerticalAlignment.Center
            };
            doubleTapToggle.Checked += (s, e) => _settingsService.UpdateSettings(s => s.DoubleTapEnabled = true);
            doubleTapToggle.Unchecked += (s, e) => _settingsService.UpdateSettings(s => s.DoubleTapEnabled = false);
            doubleTapPanel.Children.Add(doubleTapToggle);
            ContentPanel.Children.Add(doubleTapPanel);

            var doubleTapTimeoutPanel = CreateSettingRow("Double-Tap Timeout", "Maximum time between taps (milliseconds)");
            var doubleTapTimeoutSlider = new Slider
            {
                Width = 200,
                Minimum = 300,
                Maximum = 1000,
                Value = settings.DoubleTapTimeoutMs,
                TickFrequency = 100,
                IsSnapToTickEnabled = true
            };
            var doubleTapTimeoutLabel = new TextBlock { Text = $"{settings.DoubleTapTimeoutMs}ms", Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            doubleTapTimeoutSlider.ValueChanged += (s, e) =>
            {
                doubleTapTimeoutLabel.Text = $"{(int)e.NewValue}ms";
                _settingsService.UpdateSettings(s => s.DoubleTapTimeoutMs = (int)e.NewValue);
            };
            var doubleTapTimeoutValuePanel = new StackPanel { Orientation = Orientation.Horizontal };
            doubleTapTimeoutValuePanel.Children.Add(doubleTapTimeoutSlider);
            doubleTapTimeoutValuePanel.Children.Add(doubleTapTimeoutLabel);
            doubleTapTimeoutPanel.Children.Add(doubleTapTimeoutValuePanel);
            ContentPanel.Children.Add(doubleTapTimeoutPanel);

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
                Width = 150,
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

            var removeBtn = new Button { Content = "Remove", Style = FindResource("SecondaryButtonStyle") as Style, Margin = new Thickness(8, 0, 0, 0) };
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

            var buttonPanel2 = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };

            var browseBtn = new Button { Content = "Browse...", Style = FindResource("SecondaryButtonStyle") as Style };
            browseBtn.Click += (s, e) =>
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Applications (*.exe)|*.exe",
                    Title = "Select Application to Exclude"
                };
                if (dialog.ShowDialog() == true)
                {
                    var processName = System.IO.Path.GetFileNameWithoutExtension(dialog.FileName);
                    if (!settings.ExcludedApps.Contains(processName))
                    {
                        settings.ExcludedApps.Add(processName);
                        excludedList.Items.Add(processName);
                        _settingsService.SaveSettings();
                    }
                }
            };
            buttonPanel2.Children.Add(browseBtn);

            var fromRunningBtn = new Button { Content = "From Running Apps", Style = FindResource("SecondaryButtonStyle") as Style, Margin = new Thickness(8, 0, 0, 0) };
            fromRunningBtn.Click += (s, e) =>
            {
                var picker = new RunningAppsPickerWindow(_windowService) { Owner = this };
                if (picker.ShowDialog() == true && picker.SelectedProcessNames.Count > 0)
                {
                    foreach (var processName in picker.SelectedProcessNames)
                    {
                        if (!settings.ExcludedApps.Contains(processName))
                        {
                            settings.ExcludedApps.Add(processName);
                            excludedList.Items.Add(processName);
                        }
                    }
                    _settingsService.SaveSettings();
                }
            };
            buttonPanel2.Children.Add(fromRunningBtn);

            var fromInstalledBtn = new Button { Content = "Browse Installed", Style = FindResource("SecondaryButtonStyle") as Style, Margin = new Thickness(8, 0, 0, 0) };
            fromInstalledBtn.Click += (s, e) =>
            {
                var picker = new InstalledAppsPickerWindow { Owner = this };
                if (picker.ShowDialog() == true && picker.SelectedApps.Count > 0)
                {
                    foreach (var app in picker.SelectedApps)
                    {
                        var processName = System.IO.Path.GetFileNameWithoutExtension(app.Path);
                        if (!settings.ExcludedApps.Contains(processName))
                        {
                            settings.ExcludedApps.Add(processName);
                            excludedList.Items.Add(processName);
                        }
                    }
                    _settingsService.SaveSettings();
                }
            };
            buttonPanel2.Children.Add(fromInstalledBtn);

            ContentPanel.Children.Add(buttonPanel2);
        }

        private void LoadLauncherSettings()
        {
            ContentPanel.Children.Clear();
            var settings = _settingsService.Settings;

            AddHeader("Launcher Items");
            AddDescription("Add applications, folders, and groups. Use Up/Down buttons or drag and drop to reorder.");

            // Main Grid layout for List + Preview
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300) });
            grid.Margin = new Thickness(0, 16, 0, 0);

            // Left Column: List and Buttons
            var leftPanel = new StackPanel { Margin = new Thickness(0, 0, 16, 0) };

            // Container for list and Up/Down buttons
            var listContainer = new Grid();
            listContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            listContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });

            var itemsList = new ListBox
            {
                Height = 300,
                Margin = new Thickness(0, 0, 8, 0),
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                AllowDrop = true
            };

            // Enable Drag and Drop
            itemsList.PreviewMouseLeftButtonDown += ItemsList_PreviewMouseLeftButtonDown;
            itemsList.Drop += ItemsList_Drop;

            Grid.SetColumn(itemsList, 0);
            listContainer.Children.Add(itemsList);

            // Up/Down buttons panel
            var upDownPanel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

            // Create arrow icons using Polygon for robust rendering
            var upArrow = new System.Windows.Shapes.Polygon
            {
                Points = new PointCollection { new Point(0, 8), new Point(5, 0), new Point(10, 8) },
                Fill = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                Stretch = Stretch.Fill,
                Width = 12,
                Height = 8,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var upBtn = new Button
            {
                Content = upArrow,
                Width = 32,
                Height = 32,
                Margin = new Thickness(0, 0, 0, 4),
                Style = FindResource("SecondaryButtonStyle") as Style
            };
            upBtn.Click += (s, e) => MoveItem(itemsList, -1);
            upDownPanel.Children.Add(upBtn);

            var downArrow = new System.Windows.Shapes.Polygon
            {
                Points = new PointCollection { new Point(0, 0), new Point(5, 8), new Point(10, 0) },
                Fill = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                Stretch = Stretch.Fill,
                Width = 12,
                Height = 8,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var downBtn = new Button
            {
                Content = downArrow,
                Width = 32,
                Height = 32,
                Style = FindResource("SecondaryButtonStyle") as Style
            };
            downBtn.Click += (s, e) => MoveItem(itemsList, 1);
            upDownPanel.Children.Add(downBtn);

            Grid.SetColumn(upDownPanel, 1);
            listContainer.Children.Add(upDownPanel);

            leftPanel.Children.Add(listContainer);

            // Buttons
            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0), HorizontalAlignment = HorizontalAlignment.Left };

            // ... Add App Button ...
            var addAppBtn = new Button { Content = "App", Style = FindResource("ModernButtonStyle") as Style, Margin = new Thickness(0, 0, 8, 0) };
            addAppBtn.Click += (s, e) =>
            {
                var dialog = new OpenFileDialog { Filter = "Applications (*.exe)|*.exe|All Files (*.*)|*.*", Title = "Select Application" };
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

            // ... Folder Button ...
            var addFolderBtn = new Button { Content = "Folder", Style = FindResource("SecondaryButtonStyle") as Style, Margin = new Thickness(0, 0, 8, 0) };
            addFolderBtn.Click += (s, e) =>
            {
                var dialog = new System.Windows.Forms.FolderBrowserDialog();
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var newItem = new PieMenuItemData { Name = Path.GetFileName(dialog.SelectedPath), Path = dialog.SelectedPath, Type = PieMenuItemType.Folder, Order = settings.LauncherItems.Count };
                    settings.LauncherItems.Add(newItem);
                    _settingsService.SaveSettings();
                    RefreshLauncherList(itemsList);
                }
            };
            buttonPanel.Children.Add(addFolderBtn);

            // ... Installed Button ...
            var browseInstalledBtn = new Button { Content = "Installed", Style = FindResource("SecondaryButtonStyle") as Style, Margin = new Thickness(0, 0, 8, 0) };
            browseInstalledBtn.Click += (s, e) =>
            {
                var picker = new InstalledAppsPickerWindow { Owner = this };
                if (picker.ShowDialog() == true && picker.SelectedApps.Count > 0)
                {
                    foreach (var app in picker.SelectedApps)
                    {
                        var newItem = new PieMenuItemData { Name = app.Name, Path = app.Path, Type = PieMenuItemType.Application, Order = settings.LauncherItems.Count };
                        settings.LauncherItems.Add(newItem);
                    }
                    _settingsService.SaveSettings();
                    RefreshLauncherList(itemsList);
                }
            };
            buttonPanel.Children.Add(browseInstalledBtn);

            // ... Group Button ...
            var createGroupBtn = new Button { Content = "Group", Style = FindResource("SecondaryButtonStyle") as Style, Margin = new Thickness(0, 0, 8, 0) };
            createGroupBtn.Click += (s, e) =>
            {
                var editor = new GroupEditorWindow { Owner = this };
                if (editor.ShowDialog() == true)
                {
                    var newGroup = new PieMenuItemData { Name = editor.GroupName, Type = PieMenuItemType.Group, Order = settings.LauncherItems.Count, GroupItems = editor.GroupApps.ToList() };
                    settings.LauncherItems.Add(newGroup);
                    _settingsService.SaveSettings();
                    RefreshLauncherList(itemsList);
                }
            };
            buttonPanel.Children.Add(createGroupBtn);

            leftPanel.Children.Add(buttonPanel);

            // Row 2 Buttons (Edit, Remove)
            var buttonPanel2 = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0), HorizontalAlignment = HorizontalAlignment.Left };

            var editBtn = new Button { Content = "Edit", Style = FindResource("SecondaryButtonStyle") as Style, Margin = new Thickness(0, 0, 8, 0) };
            editBtn.Click += (s, e) =>
            {
                if (itemsList.SelectedItem is ListBoxItem selectedItem && selectedItem.Tag is string id)
                {
                    var item = settings.LauncherItems.FirstOrDefault(i => i.Id == id);
                    if (item != null)
                    {
                        if (item.Type == PieMenuItemType.Group)
                        {
                            var editor = new GroupEditorWindow(item.Name, item.GroupItems) { Owner = this };
                            if (editor.ShowDialog() == true)
                            {
                                item.Name = editor.GroupName;
                                item.GroupItems = editor.GroupApps.ToList();
                                _settingsService.SaveSettings();
                                RefreshLauncherList(itemsList);
                            }
                        }
                        else
                        {
                            // Edit Application or Folder - show simple name editor
                            var editDialog = new LauncherItemEditorWindow(item) { Owner = this };
                            if (editDialog.ShowDialog() == true)
                            {
                                item.Name = editDialog.ItemName;
                                if (item.Type == PieMenuItemType.Application && !string.IsNullOrEmpty(editDialog.ItemPath))
                                {
                                    item.Path = editDialog.ItemPath;
                                }
                                _settingsService.SaveSettings();
                                RefreshLauncherList(itemsList);
                            }
                        }
                    }
                }
            };
            buttonPanel2.Children.Add(editBtn);

            var removeBtn = new Button { Content = "Remove", Style = FindResource("SecondaryButtonStyle") as Style };
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
            buttonPanel2.Children.Add(removeBtn);

            leftPanel.Children.Add(buttonPanel2);

            Grid.SetColumn(leftPanel, 0);
            grid.Children.Add(leftPanel);

            // Right Column: Preview
            var rightPanel = new StackPanel();
            rightPanel.Children.Add(new TextBlock { Text = "Preview", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 8), HorizontalAlignment = HorizontalAlignment.Center });

            var previewBorder = new Border
            {
                Width = 280,
                Height = 280,
                Background = new SolidColorBrush(Color.FromRgb(240, 240, 245)),
                CornerRadius = new CornerRadius(16),
                BorderBrush = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                BorderThickness = new Thickness(1),
                ClipToBounds = true
            };

            // Initialize Preview Control
            _previewControl = new PieMenuControl
            {
                MenuRadius = 100, // Smaller for preview
                IconSize = 32,
                InnerRadius = 30,
                Width = 280,
                Height = 280
            };

            previewBorder.Child = _previewControl;
            rightPanel.Children.Add(previewBorder);

            Grid.SetColumn(rightPanel, 1);
            grid.Children.Add(rightPanel);

            ContentPanel.Children.Add(grid);

            // Initial Load
            RefreshLauncherList(itemsList);
        }

        // Drag and Drop Handlers
        private Point _dragStartPoint;

        private void ItemsList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
        }

        private void ItemsList_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && sender is ListBox listBox)
            {
                Point mousePos = e.GetPosition(null);
                Vector diff = _dragStartPoint - mousePos;

                if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    if (listBox.SelectedItem is ListBoxItem selectedItem)
                    {
                        DragDrop.DoDragDrop(listBox, selectedItem, DragDropEffects.Move);
                    }
                }
            }
        }

        private void ItemsList_Drop(object sender, DragEventArgs e)
        {
            if (sender is ListBox listBox && e.Data.GetData(typeof(ListBoxItem)) is ListBoxItem droppedData)
            {
                var target = ((UIElement)e.OriginalSource).FindVisualParent<ListBoxItem>();
                if (target != null && target != droppedData)
                {
                    string sourceId = (string)droppedData.Tag;
                    string targetId = (string)target.Tag;

                    var items = _settingsService.Settings.LauncherItems.OrderBy(i => i.Order).ToList();
                    var sourceItem = items.FirstOrDefault(i => i.Id == sourceId);
                    var targetItem = items.FirstOrDefault(i => i.Id == targetId);

                    if (sourceItem != null && targetItem != null)
                    {
                        int sourceIndex = items.IndexOf(sourceItem);
                        int targetIndex = items.IndexOf(targetItem);

                        items.RemoveAt(sourceIndex);
                        items.Insert(targetIndex, sourceItem);

                        // Update orders
                        for (int i = 0; i < items.Count; i++)
                        {
                            items[i].Order = i;
                        }

                        _settingsService.SaveSettings();
                        RefreshLauncherList(listBox);
                    }
                }
            }
        }

        private void UpdatePreview()
        {
            if (_previewControl == null) return;

            var items = new List<PieMenuItem>();
            foreach (var itemData in _settingsService.Settings.LauncherItems.OrderBy(i => i.Order))
            {
                var item = new PieMenuItem
                {
                    Id = itemData.Id,
                    Name = itemData.Name,
                    Type = itemData.Type
                };

                // Simplified icon loading for preview to avoid thread issues or lag
                if (itemData.Type == PieMenuItemType.Group && itemData.GroupItems.Count > 0)
                {
                     item.Icon = _windowService.CreateStackedGroupIcon(itemData.GroupItems.Select(g => g.Path), 32);
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

            _previewControl.SetItems(items);
            _previewControl.ShowImmediate(); // Show without animation
        }

        private void RefreshLauncherList(ListBox itemsList)
        {
            itemsList.Items.Clear();
            foreach (var item in _settingsService.Settings.LauncherItems.OrderBy(i => i.Order))
            {
                string displayText;
                if (item.Type == PieMenuItemType.Group)
                {
                    displayText = $"{item.Name} (Group - {item.GroupItems.Count} apps)";
                }
                else
                {
                    displayText = $"{item.Name} ({item.Type})";
                }

                var listItem = new ListBoxItem
                {
                    Content = displayText,
                    Tag = item.Id,
                    Padding = new Thickness(8, 4, 8, 4)
                };
                itemsList.Items.Add(listItem);
            }
            UpdatePreview();
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

        private PieMenuControl? _controllerPreviewControl;
        private ListBox? _controllerActionsList;
        private AppControllerConfig? _selectedControllerConfig;

        private void LoadControllerSettings()
        {
            ContentPanel.Children.Clear();

            AddHeader("Application Controllers");
            AddDescription("Configure keyboard shortcuts for specific applications. The controller menu will show these shortcuts when that app is active.");

            // Main Grid layout for Config + Preview
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300) });
            grid.Margin = new Thickness(0, 16, 0, 0);

            // Left Column: Configuration
            var leftPanel = new StackPanel { Margin = new Thickness(0, 0, 16, 0) };

            // App Selection ComboBox
            var appCombo = new ComboBox
            {
                Width = 300,
                Padding = new Thickness(8, 6, 8, 6),
                Margin = new Thickness(0, 0, 0, 8),
                HorizontalAlignment = HorizontalAlignment.Left
            };

            // Populate with existing configs
            foreach (var config in _settingsService.Settings.ControllerConfigs)
            {
                appCombo.Items.Add(config.AppName);
            }
            appCombo.Items.Add("+ Add New Application");

            leftPanel.Children.Add(appCombo);

            // App selection buttons row
            var appButtonPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };

            var browseAppBtn = new Button { Content = "Browse...", Style = FindResource("SecondaryButtonStyle") as Style, Margin = new Thickness(0, 0, 8, 0) };
            browseAppBtn.Click += (s, e) => AddControllerAppFromBrowse(appCombo);
            appButtonPanel.Children.Add(browseAppBtn);

            var fromRunningBtn = new Button { Content = "From Running Apps", Style = FindResource("SecondaryButtonStyle") as Style, Margin = new Thickness(0, 0, 8, 0) };
            fromRunningBtn.Click += (s, e) => AddControllerAppFromRunning(appCombo);
            appButtonPanel.Children.Add(fromRunningBtn);

            var importPresetBtn = new Button { Content = "Import Preset", Style = FindResource("ModernButtonStyle") as Style };
            importPresetBtn.Click += (s, e) => ImportControllerPreset(appCombo);
            appButtonPanel.Children.Add(importPresetBtn);

            leftPanel.Children.Add(appButtonPanel);

            // Second row: Import JSON file
            var appButtonPanel2 = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };

            var importJsonBtn = new Button { Content = "Import JSON File...", Style = FindResource("SecondaryButtonStyle") as Style };
            importJsonBtn.Click += (s, e) =>
            {
                var dialog = new OpenFileDialog
                {
                    Filter = "JSON Files (*.json)|*.json",
                    Title = "Import Presets from JSON"
                };
                if (dialog.ShowDialog() == true)
                {
                    try
                    {
                        _presetService.ImportPresetsFromFile(dialog.FileName);
                        MessageBox.Show("Presets imported successfully!", "Import Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to import presets: {ex.Message}", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            };
            appButtonPanel2.Children.Add(importJsonBtn);

            leftPanel.Children.Add(appButtonPanel2);

            // Installed apps with presets section
            var presetsAvailableHeader = new TextBlock
            {
                Text = "Apps with Presets Available:",
                FontWeight = FontWeights.Medium,
                FontSize = 12,
                Margin = new Thickness(0, 8, 0, 4),
                Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100))
            };
            leftPanel.Children.Add(presetsAvailableHeader);

            var presetAppsList = new ListBox
            {
                Height = 80,
                Background = new SolidColorBrush(Color.FromRgb(250, 250, 255)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 220)),
                Margin = new Thickness(0, 0, 0, 12)
            };

            // Find installed apps that have presets
            var installedAppsWithPresets = GetInstalledAppsWithPresets();
            foreach (var app in installedAppsWithPresets.Take(10))
            {
                var listItem = new ListBoxItem
                {
                    Content = $"{app.Name} ({app.ProcessName})",
                    Tag = app,
                    Padding = new Thickness(8, 4, 8, 4),
                    FontSize = 12
                };
                presetAppsList.Items.Add(listItem);
            }

            if (presetAppsList.Items.Count == 0)
            {
                presetAppsList.Items.Add(new ListBoxItem { Content = "No installed apps with presets found", IsEnabled = false, FontStyle = FontStyles.Italic });
            }

            presetAppsList.MouseDoubleClick += (s, e) =>
            {
                if (presetAppsList.SelectedItem is ListBoxItem item && item.Tag is InstalledAppWithPreset app)
                {
                    ImportPresetForApp(app, appCombo);
                }
            };
            leftPanel.Children.Add(presetAppsList);

            // Actions section
            var actionsHeader = new TextBlock { Text = "Actions", FontWeight = FontWeights.SemiBold, FontSize = 13, Margin = new Thickness(0, 8, 0, 8) };
            leftPanel.Children.Add(actionsHeader);

            _controllerActionsList = new ListBox
            {
                Height = 150,
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200))
            };
            leftPanel.Children.Add(_controllerActionsList);

            // Action form
            var formPanel = new StackPanel { Margin = new Thickness(0, 12, 0, 0) };

            var nameRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 4) };
            nameRow.Children.Add(new TextBlock { Text = "Action Name:", Width = 100, VerticalAlignment = VerticalAlignment.Center, FontSize = 12 });
            var nameInput = new TextBox { Width = 180, Padding = new Thickness(8, 6, 8, 6) };
            nameRow.Children.Add(nameInput);
            formPanel.Children.Add(nameRow);

            var shortcutRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 4) };
            shortcutRow.Children.Add(new TextBlock { Text = "Shortcut:", Width = 100, VerticalAlignment = VerticalAlignment.Center, FontSize = 12 });
            var shortcutInput = new TextBox { Width = 180, Padding = new Thickness(8, 6, 8, 6), IsReadOnly = true };
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

            var actionButtonPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };

            var addActionBtn = new Button { Content = "Add Action", Style = FindResource("ModernButtonStyle") as Style, Margin = new Thickness(0, 0, 8, 0) };
            addActionBtn.Click += (s, e) =>
            {
                if (_selectedControllerConfig != null && !string.IsNullOrWhiteSpace(nameInput.Text) && !string.IsNullOrWhiteSpace(shortcutInput.Text))
                {
                    _selectedControllerConfig.Actions.Add(new ControllerAction
                    {
                        Name = nameInput.Text,
                        KeyboardShortcut = shortcutInput.Text
                    });
                    _settingsService.SaveSettings();
                    RefreshControllerActionsList();
                    UpdateControllerPreview();
                    nameInput.Clear();
                    shortcutInput.Clear();
                }
            };
            actionButtonPanel.Children.Add(addActionBtn);

            var removeActionBtn = new Button { Content = "Remove", Style = FindResource("SecondaryButtonStyle") as Style, Margin = new Thickness(0, 0, 8, 0) };
            removeActionBtn.Click += (s, e) =>
            {
                if (_selectedControllerConfig != null && _controllerActionsList?.SelectedItem is ListBoxItem item && item.Tag is string actionId)
                {
                    var action = _selectedControllerConfig.Actions.FirstOrDefault(a => a.Id == actionId);
                    if (action != null)
                    {
                        _selectedControllerConfig.Actions.Remove(action);
                        _settingsService.SaveSettings();
                        RefreshControllerActionsList();
                        UpdateControllerPreview();
                    }
                }
            };
            actionButtonPanel.Children.Add(removeActionBtn);

            var editActionBtn = new Button { Content = "Edit", Style = FindResource("SecondaryButtonStyle") as Style, Margin = new Thickness(0, 0, 8, 0) };
            editActionBtn.Click += (s, e) =>
            {
                if (_selectedControllerConfig != null && _controllerActionsList?.SelectedItem is ListBoxItem item && item.Tag is string actionId)
                {
                    var action = _selectedControllerConfig.Actions.FirstOrDefault(a => a.Id == actionId);
                    if (action != null)
                    {
                        var editor = new ActionEditorWindow(action.Name, action.KeyboardShortcut) { Owner = this };
                        if (editor.ShowDialog() == true)
                        {
                            action.Name = editor.ActionName;
                            action.KeyboardShortcut = editor.ActionShortcut;
                            _settingsService.SaveSettings();
                            RefreshControllerActionsList();
                            UpdateControllerPreview();
                        }
                    }
                }
            };
            actionButtonPanel.Children.Add(editActionBtn);

            var deleteAppBtn = new Button { Content = "Delete App", Style = FindResource("SecondaryButtonStyle") as Style };
            deleteAppBtn.Click += (s, e) =>
            {
                if (_selectedControllerConfig != null)
                {
                    var result = MessageBox.Show($"Delete all shortcuts for {_selectedControllerConfig.AppName}?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        _settingsService.Settings.ControllerConfigs.Remove(_selectedControllerConfig);
                        _settingsService.SaveSettings();
                        _selectedControllerConfig = null;
                        LoadControllerSettings(); // Reload
                    }
                }
            };
            actionButtonPanel.Children.Add(deleteAppBtn);

            formPanel.Children.Add(actionButtonPanel);
            leftPanel.Children.Add(formPanel);

            Grid.SetColumn(leftPanel, 0);
            grid.Children.Add(leftPanel);

            // Right Column: Preview
            var rightPanel = new StackPanel();
            rightPanel.Children.Add(new TextBlock { Text = "Preview", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 8), HorizontalAlignment = HorizontalAlignment.Center });

            var previewBorder = new Border
            {
                Width = 280,
                Height = 280,
                Background = new SolidColorBrush(Color.FromRgb(240, 240, 245)),
                CornerRadius = new CornerRadius(16),
                BorderBrush = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                BorderThickness = new Thickness(1),
                ClipToBounds = true
            };

            _controllerPreviewControl = new PieMenuControl
            {
                MenuRadius = 100,
                IconSize = 32,
                InnerRadius = 30,
                Width = 280,
                Height = 280
            };

            previewBorder.Child = _controllerPreviewControl;
            rightPanel.Children.Add(previewBorder);

            Grid.SetColumn(rightPanel, 1);
            grid.Children.Add(rightPanel);

            ContentPanel.Children.Add(grid);

            // Handle app selection
            appCombo.SelectionChanged += (s, e) =>
            {
                if (appCombo.SelectedItem?.ToString() == "+ Add New Application")
                {
                    AddControllerAppFromBrowse(appCombo);
                }
                else if (appCombo.SelectedIndex >= 0 && appCombo.SelectedIndex < _settingsService.Settings.ControllerConfigs.Count)
                {
                    _selectedControllerConfig = _settingsService.Settings.ControllerConfigs[appCombo.SelectedIndex];
                    RefreshControllerActionsList();
                    UpdateControllerPreview();
                }
            };

            if (appCombo.Items.Count > 1)
            {
                appCombo.SelectedIndex = 0;
            }
        }

        private void AddControllerAppFromBrowse(ComboBox appCombo)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Applications (*.exe)|*.exe",
                Title = "Select Application"
            };

            if (dialog.ShowDialog() == true)
            {
                var processName = Path.GetFileNameWithoutExtension(dialog.FileName);
                AddControllerConfig(processName, processName, appCombo);
            }
            else
            {
                appCombo.SelectedIndex = -1;
            }
        }

        private void AddControllerAppFromRunning(ComboBox appCombo)
        {
            var picker = new RunningAppsPickerWindow(_windowService) { Owner = this };
            if (picker.ShowDialog() == true && picker.SelectedProcessNames.Count > 0)
            {
                foreach (var processName in picker.SelectedProcessNames)
                {
                    AddControllerConfig(processName, processName, appCombo);
                }
            }
        }

        private void ImportControllerPreset(ComboBox appCombo)
        {
            var picker = new PresetPickerWindow(_presetService) { Owner = this };
            if (picker.ShowDialog() == true && picker.SelectedPreset != null)
            {
                var preset = picker.SelectedPreset;
                var config = new AppControllerConfig
                {
                    ProcessName = preset.ProcessNames.FirstOrDefault() ?? preset.Name.ToLower(),
                    AppName = preset.Name,
                    Actions = preset.Actions.Select(a => new ControllerAction
                    {
                        Name = a.Name,
                        KeyboardShortcut = a.Shortcut
                    }).ToList()
                };

                // Check if already exists
                var existing = _settingsService.Settings.ControllerConfigs.FirstOrDefault(c => c.ProcessName.Equals(config.ProcessName, StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    var result = MessageBox.Show($"{preset.Name} already has shortcuts configured. Replace them?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        existing.Actions = config.Actions;
                        _settingsService.SaveSettings();
                        LoadControllerSettings();
                    }
                }
                else
                {
                    _settingsService.Settings.ControllerConfigs.Add(config);
                    _settingsService.SaveSettings();
                    appCombo.Items.Insert(appCombo.Items.Count - 1, config.AppName);
                    appCombo.SelectedIndex = appCombo.Items.Count - 2;
                }
            }
        }

        private void AddControllerConfig(string processName, string appName, ComboBox appCombo)
        {
            // Check if already exists
            var existing = _settingsService.Settings.ControllerConfigs.FirstOrDefault(c => c.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                var index = _settingsService.Settings.ControllerConfigs.IndexOf(existing);
                appCombo.SelectedIndex = index;
                return;
            }

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

        private void RefreshControllerActionsList()
        {
            if (_controllerActionsList == null || _selectedControllerConfig == null) return;

            _controllerActionsList.Items.Clear();
            foreach (var action in _selectedControllerConfig.Actions)
            {
                _controllerActionsList.Items.Add(new ListBoxItem
                {
                    Content = $"{action.Name} - {action.KeyboardShortcut}",
                    Tag = action.Id,
                    Padding = new Thickness(8, 4, 8, 4)
                });
            }
        }

        private void UpdateControllerPreview()
        {
            if (_controllerPreviewControl == null || _selectedControllerConfig == null) return;

            var items = new List<PieMenuItem>();
            foreach (var action in _selectedControllerConfig.Actions)
            {
                var item = new PieMenuItem
                {
                    Name = action.Name,
                    KeyboardShortcut = action.KeyboardShortcut,
                    Type = PieMenuItemType.Action,
                    Icon = Helpers.IconHelper.CreateActionIcon(action.Name)
                };
                items.Add(item);
            }

            _controllerPreviewControl.SetItems(items);
            _controllerPreviewControl.ShowImmediate();
        }

        private class InstalledAppWithPreset
        {
            public string Name { get; set; } = string.Empty;
            public string ProcessName { get; set; } = string.Empty;
            public string Path { get; set; } = string.Empty;
            public Preset Preset { get; set; } = null!;
        }

        private List<InstalledAppWithPreset> GetInstalledAppsWithPresets()
        {
            var result = new List<InstalledAppWithPreset>();
            var allPresets = _presetService.GetAllPresets();

            // Check common app paths
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            foreach (var preset in allPresets)
            {
                foreach (var processName in preset.ProcessNames)
                {
                    // Check if app is running
                    var running = System.Diagnostics.Process.GetProcessesByName(processName);
                    if (running.Length > 0)
                    {
                        try
                        {
                            var path = running[0].MainModule?.FileName ?? "";
                            result.Add(new InstalledAppWithPreset
                            {
                                Name = preset.Name,
                                ProcessName = processName,
                                Path = path,
                                Preset = preset
                            });
                            break;
                        }
                        catch { }
                    }
                }
            }

            return result.DistinctBy(x => x.ProcessName).ToList();
        }

        private void ImportPresetForApp(InstalledAppWithPreset app, ComboBox appCombo)
        {
            var config = new AppControllerConfig
            {
                ProcessName = app.ProcessName,
                AppName = app.Preset.Name,
                Actions = app.Preset.Actions.Select(a => new ControllerAction
                {
                    Name = a.Name,
                    KeyboardShortcut = a.Shortcut
                }).ToList()
            };

            var existing = _settingsService.Settings.ControllerConfigs.FirstOrDefault(c => c.ProcessName.Equals(app.ProcessName, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                var result = MessageBox.Show($"{app.Preset.Name} already has shortcuts. Replace?", "Confirm", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    existing.Actions = config.Actions;
                    _settingsService.SaveSettings();
                    LoadControllerSettings();
                }
            }
            else
            {
                _settingsService.Settings.ControllerConfigs.Add(config);
                _settingsService.SaveSettings();
                appCombo.Items.Insert(appCombo.Items.Count - 1, config.AppName);
                appCombo.SelectedIndex = appCombo.Items.Count - 2;
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

        private void LoadGesturesSettings()
        {
            ContentPanel.Children.Clear();
            var settings = _settingsService.Settings;

            AddHeader("Gesture Flow Editor");
            AddDescription("Drag and drop modes to customize your workflow.");

            // 1. Mode Palette (Draggable Sources)
            var palettePanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 20, 0, 30),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            palettePanel.Children.Add(CreateModeCard(PieMenuMode.Switcher));
            palettePanel.Children.Add(CreateModeCard(PieMenuMode.Launcher));
            palettePanel.Children.Add(CreateModeCard(PieMenuMode.Controller));
            palettePanel.Children.Add(CreateModeCard(PieMenuMode.MusicRemote));

            ContentPanel.Children.Add(palettePanel);

            // 2. Activation Triggers
            AddHeader("Activation Triggers");
            AddDescription("What opens when you tap?");

            var activationGrid = new Grid { Margin = new Thickness(0, 10, 0, 30) };
            activationGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            activationGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });
            activationGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Single Tap Target
            var singleTapPanel = new StackPanel();
            singleTapPanel.Children.Add(new TextBlock { Text = "Single Tap (3-Finger)", FontWeight = FontWeights.Medium, Margin = new Thickness(0, 0, 0, 8), HorizontalAlignment = HorizontalAlignment.Center });
            var singleTapTarget = CreateDropTarget(
                settings.DefaultMode,
                (mode) =>
                {
                    _settingsService.UpdateSettings(s => s.DefaultMode = mode);
                    LoadGesturesSettings(); // Refresh
                });
            singleTapPanel.Children.Add(singleTapTarget);
            Grid.SetColumn(singleTapPanel, 0);
            activationGrid.Children.Add(singleTapPanel);

            // Double Tap Target
            var doubleTapPanel = new StackPanel();
            doubleTapPanel.Children.Add(new TextBlock { Text = "Double Tap", FontWeight = FontWeights.Medium, Margin = new Thickness(0, 0, 0, 8), HorizontalAlignment = HorizontalAlignment.Center });
            var doubleTapTarget = CreateDropTarget(
                settings.DoubleTapMode,
                (mode) =>
                {
                    _settingsService.UpdateSettings(s => s.DoubleTapMode = mode);
                    LoadGesturesSettings(); // Refresh
                });
            doubleTapPanel.Children.Add(doubleTapTarget);
            Grid.SetColumn(doubleTapPanel, 2);
            activationGrid.Children.Add(doubleTapPanel);

            ContentPanel.Children.Add(activationGrid);

            // 3. Right-Click Flow
            AddHeader("Right-Click Flow");
            AddDescription("Define the cycle: If you are in Mode A, Right-Click takes you to Mode B.");

            var flowPanel = new StackPanel { Margin = new Thickness(0, 10, 0, 0) };

            foreach (PieMenuMode mode in Enum.GetValues(typeof(PieMenuMode)))
            {
                var row = new Grid { Margin = new Thickness(0, 0, 0, 10) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) }); // Source Label
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });  // Arrow
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) }); // Target Box

                // Source Mode Label
                var sourceCard = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(240, 240, 245)),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(10, 6, 10, 6),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Width = 110
                };
                sourceCard.Child = new TextBlock { Text = mode.ToString(), FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Center };
                Grid.SetColumn(sourceCard, 0);
                row.Children.Add(sourceCard);

                // Arrow
                var arrow = new TextBlock
                {
                    Text = "→",
                    FontSize = 18,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Foreground = Brushes.Gray
                };
                Grid.SetColumn(arrow, 1);
                row.Children.Add(arrow);

                // Target Drop Zone
                PieMenuMode? nextMode = null;
                if (settings.RightClickFlow.ContainsKey(mode))
                {
                    nextMode = settings.RightClickFlow[mode];
                }

                var targetZone = CreateDropTarget(
                    nextMode,
                    (droppedMode) =>
                    {
                        _settingsService.UpdateSettings(s => s.RightClickFlow[mode] = droppedMode);
                        LoadGesturesSettings();
                    },
                    isFlowTarget: true);

                Grid.SetColumn(targetZone, 2);
                row.Children.Add(targetZone);

                flowPanel.Children.Add(row);
            }

            ContentPanel.Children.Add(flowPanel);
        }

        private UIElement CreateModeCard(PieMenuMode mode)
        {
            var card = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0, 122, 255)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16, 10, 16, 10),
                Margin = new Thickness(8),
                Cursor = Cursors.Hand,
                Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 5, ShadowDepth = 2, Opacity = 0.2 }
            };

            var text = new TextBlock { Text = mode.ToString(), Foreground = Brushes.White, FontWeight = FontWeights.Bold };
            card.Child = text;

            card.MouseLeftButtonDown += (s, e) =>
            {
                DragDrop.DoDragDrop(card, mode, DragDropEffects.Copy);
            };

            return card;
        }

        private UIElement CreateDropTarget(PieMenuMode? currentMode, Action<PieMenuMode> onDrop, bool isFlowTarget = false)
        {
            var border = new Border
            {
                Background = currentMode.HasValue ? new SolidColorBrush(Color.FromRgb(230, 240, 255)) : Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10),
                Height = isFlowTarget ? 40 : 60,
                AllowDrop = true
            };

            // Dashed border if empty
            if (!currentMode.HasValue)
            {
                border.BorderBrush = new SolidColorBrush(Color.FromRgb(220, 220, 220));
                // Note: WPF Border doesn't support StrokeDashArray directly, would need a Rectangle/Path,
                // but solid gray is fine for "empty slot" visual.
            }

            var content = new TextBlock
            {
                Text = currentMode.HasValue ? currentMode.ToString() : "Drop Here",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = currentMode.HasValue ? new SolidColorBrush(Color.FromRgb(0, 122, 255)) : Brushes.Gray,
                FontWeight = currentMode.HasValue ? FontWeights.Bold : FontWeights.Normal
            };

            border.Child = content;

            border.Drop += (s, e) =>
            {
                if (e.Data.GetData(typeof(PieMenuMode)) is PieMenuMode droppedMode)
                {
                    onDrop(droppedMode);
                }
            };

            return border;
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
                Text = "Version 0.6-alpha",
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(128, 128, 128)),
                Margin = new Thickness(0, 0, 0, 8)
            };
            ContentPanel.Children.Add(version);

            var madeBy = new TextBlock
            {
                Text = "Made by Devank",
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 4)
            };
            ContentPanel.Children.Add(madeBy);

            var githubLink = new TextBlock
            {
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 24)
            };
            var hyperlink = new System.Windows.Documents.Hyperlink
            {
                NavigateUri = new Uri("https://github.com/DevankU")
            };
            hyperlink.Inlines.Add("github.com/DevankU");
            hyperlink.RequestNavigate += (s, e) =>
            {
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
                e.Handled = true;
            };
            githubLink.Inlines.Add(hyperlink);
            ContentPanel.Children.Add(githubLink);

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
                       "• Gesture Flow Editor - Visualize and customize your workflow\n" +
                       "• Smart Context - Auto-detects active app for Controller\n" +
                       "• Dynamic Icons - Smart icon generation for actions\n" +
                       "• Fluid animations & Instant Response",
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
