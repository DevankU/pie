using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using Pie.Models;

namespace Pie.Views
{
    public partial class GroupEditorWindow : Window
    {
        public string GroupName { get; private set; } = string.Empty;
        public ObservableCollection<GroupAppItem> GroupApps { get; private set; } = new();

        public GroupEditorWindow()
        {
            InitializeComponent();
            AppsListBox.ItemsSource = GroupApps;
        }

        public GroupEditorWindow(string groupName, IEnumerable<GroupAppItem> existingApps) : this()
        {
            GroupNameTextBox.Text = groupName;
            foreach (var app in existingApps)
            {
                GroupApps.Add(new GroupAppItem
                {
                    Id = app.Id,
                    Name = app.Name,
                    Path = app.Path
                });
            }
        }

        private void AddAppButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Applications (*.exe)|*.exe|All Files (*.*)|*.*",
                Title = "Select Application",
                Multiselect = true
            };

            if (dialog.ShowDialog() == true)
            {
                foreach (var fileName in dialog.FileNames)
                {
                    GroupApps.Add(new GroupAppItem
                    {
                        Name = Path.GetFileNameWithoutExtension(fileName),
                        Path = fileName
                    });
                }
            }
        }

        private void BrowseInstalledButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new InstalledAppsPickerWindow { Owner = this };
            if (picker.ShowDialog() == true && picker.SelectedApps.Count > 0)
            {
                foreach (var app in picker.SelectedApps)
                {
                    GroupApps.Add(new GroupAppItem
                    {
                        Name = app.Name,
                        Path = app.Path
                    });
                }
            }
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (AppsListBox.SelectedItem is GroupAppItem selectedItem)
            {
                GroupApps.Remove(selectedItem);
            }
        }

        private void MoveUpButton_Click(object sender, RoutedEventArgs e)
        {
            int index = AppsListBox.SelectedIndex;
            if (index > 0)
            {
                var item = GroupApps[index];
                GroupApps.RemoveAt(index);
                GroupApps.Insert(index - 1, item);
                AppsListBox.SelectedIndex = index - 1;
            }
        }

        private void MoveDownButton_Click(object sender, RoutedEventArgs e)
        {
            int index = AppsListBox.SelectedIndex;
            if (index >= 0 && index < GroupApps.Count - 1)
            {
                var item = GroupApps[index];
                GroupApps.RemoveAt(index);
                GroupApps.Insert(index + 1, item);
                AppsListBox.SelectedIndex = index + 1;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(GroupNameTextBox.Text))
            {
                MessageBox.Show("Please enter a group name.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (GroupApps.Count == 0)
            {
                MessageBox.Show("Please add at least one application to the group.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            GroupName = GroupNameTextBox.Text.Trim();
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
