using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Pie.Models;
using Pie.Services;

namespace Pie.Views
{
    public partial class PresetPickerWindow : Window
    {
        private readonly List<Preset> _allPresets;
        public Preset? SelectedPreset { get; private set; }

        public PresetPickerWindow(PresetService presetService)
        {
            InitializeComponent();
            _allPresets = presetService.GetAllPresets().OrderBy(p => p.Name).ToList();
            PresetsList.ItemsSource = _allPresets;

            // Set initial focus to search box
            Loaded += (s, e) => SearchBox.Focus();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = SearchBox.Text.Trim();
            if (string.IsNullOrEmpty(searchText))
            {
                PresetsList.ItemsSource = _allPresets;
            }
            else
            {
                PresetsList.ItemsSource = _allPresets
                    .Where(p => p.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                                p.Description.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                                p.ProcessNames.Any(pn => pn.Contains(searchText, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }
        }

        private void PresetsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ImportButton.IsEnabled = PresetsList.SelectedItem != null;
        }

        private void PresetsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (PresetsList.SelectedItem is Preset preset)
            {
                SelectedPreset = preset;
                DialogResult = true;
                Close();
            }
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            if (PresetsList.SelectedItem is Preset preset)
            {
                SelectedPreset = preset;
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