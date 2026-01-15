using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Pie.Helpers;

namespace Pie.Views
{
    public partial class InstalledAppsPickerWindow : Window
    {
        private List<InstalledApp> _allApps = new List<InstalledApp>();
        public InstalledApp? SelectedApp { get; private set; }
        public List<InstalledApp> SelectedApps { get; private set; } = new List<InstalledApp>();
        public bool AllowMultipleSelection { get; set; } = true;

        public InstalledAppsPickerWindow()
        {
            InitializeComponent();
            Loaded += InstalledAppsPickerWindow_Loaded;
        }

        private async void InstalledAppsPickerWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (!AllowMultipleSelection)
            {
                AppsList.SelectionMode = SelectionMode.Single;
                SelectionCountText.Visibility = Visibility.Collapsed;
            }
            await LoadInstalledAppsAsync();
        }

        private async Task LoadInstalledAppsAsync()
        {
            SearchBox.IsEnabled = false;
            SearchBox.Text = "Loading applications...";
            SelectButton.IsEnabled = false;

            try
            {
                _allApps = await Task.Run(() => InstalledAppsHelper.GetInstalledApplications());
                AppsList.ItemsSource = _allApps;
                SearchBox.Text = "";
                SearchBox.IsEnabled = true;
                SearchBox.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load installed applications: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                _allApps = new List<InstalledApp>();
                SearchBox.Text = "";
                SearchBox.IsEnabled = true;
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (SearchBox.Text == "Loading applications...") return;

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
            var selectedCount = AppsList.SelectedItems.Count;
            SelectButton.IsEnabled = selectedCount > 0;

            if (AllowMultipleSelection && selectedCount > 0)
            {
                SelectionCountText.Text = selectedCount == 1
                    ? "1 application selected"
                    : $"{selectedCount} applications selected";
            }
            else
            {
                SelectionCountText.Text = "";
            }
        }

        private void AppsList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (AppsList.SelectedItem is InstalledApp app)
            {
                SelectedApp = app;
                SelectedApps = new List<InstalledApp> { app };
                DialogResult = true;
                Close();
            }
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedApps = AppsList.SelectedItems.Cast<InstalledApp>().ToList();
            SelectedApp = SelectedApps.FirstOrDefault();

            if (SelectedApps.Count > 0)
            {
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
