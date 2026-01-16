using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace Pie.Views
{
    public partial class ActionEditorWindow : Window
    {
        public string ActionName { get; private set; } = string.Empty;
        public string ActionShortcut { get; private set; } = string.Empty;

        public ActionEditorWindow(string name, string shortcut)
        {
            InitializeComponent();
            NameBox.Text = name;
            ShortcutBox.Text = shortcut;

            ShortcutBox.PreviewKeyDown += ShortcutBox_PreviewKeyDown;
            Loaded += (s, e) => NameBox.Focus();
        }

        private void ShortcutBox_PreviewKeyDown(object sender, KeyEventArgs e)
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
            ShortcutBox.Text = string.Join("+", modifiers);
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameBox.Text))
            {
                MessageBox.Show("Please enter an action name.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(ShortcutBox.Text))
            {
                MessageBox.Show("Please set a keyboard shortcut.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ActionName = NameBox.Text.Trim();
            ActionShortcut = ShortcutBox.Text;
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
