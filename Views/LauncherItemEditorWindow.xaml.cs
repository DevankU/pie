using System.Windows;
using Microsoft.Win32;
using Pie.Models;

namespace Pie.Views
{
    public partial class LauncherItemEditorWindow : Window
    {
        public string ItemName { get; private set; } = string.Empty;
        public string ItemPath { get; private set; } = string.Empty;
        private readonly PieMenuItemType _itemType;

        public LauncherItemEditorWindow(PieMenuItemData item)
        {
            InitializeComponent();
            _itemType = item.Type;
            NameBox.Text = item.Name;
            PathBox.Text = item.Path;

            // Hide path panel for folders
            if (item.Type == PieMenuItemType.Folder)
            {
                PathPanel.Visibility = Visibility.Collapsed;
            }

            Loaded += (s, e) => NameBox.Focus();
        }

        private void BrowseBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Applications (*.exe)|*.exe|All Files (*.*)|*.*",
                Title = "Select Application"
            };
            if (dialog.ShowDialog() == true)
            {
                PathBox.Text = dialog.FileName;
            }
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameBox.Text))
            {
                MessageBox.Show("Please enter a name.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ItemName = NameBox.Text.Trim();
            ItemPath = PathBox.Text;
            DialogResult = true;
            Close();
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
