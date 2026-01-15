using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Pie.Models;
using Pie.Services;

namespace Pie.Views
{
    public partial class RunningAppsPickerWindow : Window
    {
        private readonly WindowService _windowService;
        public List<string> SelectedProcessNames { get; private set; } = new List<string>();

        public RunningAppsPickerWindow(WindowService windowService)
        {
            _windowService = windowService;
            InitializeComponent();
            LoadRunningApps();
        }

        private void LoadRunningApps()
        {
            var runningApps = _windowService.GetRunningApplications()
                .GroupBy(a => a.ProcessName)
                .Select(g => g.First())
                .OrderBy(a => a.ProcessName)
                .ToList();

            AppsList.ItemsSource = runningApps;
        }

        private void AppsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedCount = AppsList.SelectedItems.Count;
            SelectButton.IsEnabled = selectedCount > 0;

            if (selectedCount > 0)
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
            if (AppsList.SelectedItem is PieMenuItem item && !string.IsNullOrEmpty(item.ProcessName))
            {
                SelectedProcessNames = new List<string> { item.ProcessName };
                DialogResult = true;
                Close();
            }
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedProcessNames = AppsList.SelectedItems
                .Cast<PieMenuItem>()
                .Where(item => !string.IsNullOrEmpty(item.ProcessName))
                .Select(item => item.ProcessName!)
                .Distinct()
                .ToList();

            if (SelectedProcessNames.Count > 0)
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
