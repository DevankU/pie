using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Pie.Helpers;

namespace Pie.Views
{
    public partial class InstalledAppsPickerWindow : Window
    {
        private List<InstalledApp> _allApps = new List<InstalledApp>();
        public InstalledApp? SelectedApp { get; private set; }

        public InstalledAppsPickerWindow()
        {
            InitializeComponent();
            LoadInstalledApps();
        }

        private void LoadInstalledApps()
        {
            try
            {
                _allApps = InstalledAppsHelper.GetInstalledApplications();
                AppsList.ItemsSource = _allApps;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load installed applications: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                _allApps = new List<InstalledApp>();
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = SearchBox.Text.Trim();
            if (string.IsNullOrEmpty(searchText))
            {
                AppsList.ItemsSource = _allApps;
            }
            else
            {
                AppsList.ItemsSource = _allApps
                    .Where(a => a.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
        }

        private void AppsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectButton.IsEnabled = AppsList.SelectedItem != null;
        }

        private void AppsList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (AppsList.SelectedItem is InstalledApp app)
            {
                SelectedApp = app;
                DialogResult = true;
                Close();
            }
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            if (AppsList.SelectedItem is InstalledApp app)
            {
                SelectedApp = app;
                DialogResult = true;
                Close();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
