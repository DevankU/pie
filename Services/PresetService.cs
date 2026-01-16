using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Pie.Models;

namespace Pie.Services
{
    public class PresetService
    {
        private List<Preset> _presets = new();
        private Dictionary<string, Preset> _processMap = new();
        private readonly string _userPresetsPath;

        public PresetService()
        {
            _userPresetsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Pie", "user_presets.json");
            LoadPresets();
        }

        private void LoadPresets()
        {
            try
            {
                // Load built-in presets
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = "Pie.presets.json";

                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            var json = reader.ReadToEnd();
                            _presets = JsonConvert.DeserializeObject<List<Preset>>(json) ?? new List<Preset>();
                        }
                    }
                }

                // Load user presets and merge
                LoadUserPresets();

                // Build lookup map for O(1) access
                RebuildProcessMap();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load presets: {ex.Message}");
            }
        }

        private void LoadUserPresets()
        {
            try
            {
                if (File.Exists(_userPresetsPath))
                {
                    var json = File.ReadAllText(_userPresetsPath);
                    var userPresets = JsonConvert.DeserializeObject<List<Preset>>(json) ?? new List<Preset>();

                    foreach (var userPreset in userPresets)
                    {
                        // Replace or add user presets
                        var existing = _presets.FirstOrDefault(p => p.Id == userPreset.Id ||
                            p.ProcessNames.Any(pn => userPreset.ProcessNames.Contains(pn, StringComparer.OrdinalIgnoreCase)));

                        if (existing != null)
                        {
                            // Replace existing
                            var index = _presets.IndexOf(existing);
                            _presets[index] = userPreset;
                        }
                        else
                        {
                            _presets.Add(userPreset);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load user presets: {ex.Message}");
            }
        }

        private void RebuildProcessMap()
        {
            _processMap.Clear();
            foreach (var preset in _presets)
            {
                foreach (var process in preset.ProcessNames)
                {
                    var key = process.ToLowerInvariant();
                    if (!_processMap.ContainsKey(key))
                    {
                        _processMap[key] = preset;
                    }
                }
            }
        }

        public Preset? GetPresetForProcess(string processName)
        {
            if (string.IsNullOrEmpty(processName)) return null;

            var key = processName.ToLowerInvariant();

            // 1. Try exact match from map
            if (_processMap.TryGetValue(key, out var preset))
            {
                return preset;
            }

            // 2. Try partial match (e.g. "code" matching "code-insiders")
            return _presets.FirstOrDefault(p => p.ProcessNames.Any(pn => key.Contains(pn) || pn.Contains(key)));
        }

        public List<Preset> GetAllPresets()
        {
            return _presets;
        }

        public void ImportPresetsFromFile(string filePath)
        {
            try
            {
                var json = File.ReadAllText(filePath);
                var importedPresets = JsonConvert.DeserializeObject<List<Preset>>(json);

                if (importedPresets != null && importedPresets.Count > 0)
                {
                    foreach (var preset in importedPresets)
                    {
                        // Ensure IDs are set
                        if (string.IsNullOrEmpty(preset.Id))
                        {
                            preset.Id = Guid.NewGuid().ToString();
                        }

                        // Replace or add
                        var existing = _presets.FirstOrDefault(p => p.Id == preset.Id ||
                            p.ProcessNames.Any(pn => preset.ProcessNames.Contains(pn, StringComparer.OrdinalIgnoreCase)));

                        if (existing != null)
                        {
                            var index = _presets.IndexOf(existing);
                            _presets[index] = preset;
                        }
                        else
                        {
                            _presets.Add(preset);
                        }
                    }

                    SaveUserPresets();
                    RebuildProcessMap();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to import presets: {ex.Message}", ex);
            }
        }

        public void UpdatePreset(Preset preset)
        {
            var existing = _presets.FirstOrDefault(p => p.Id == preset.Id);
            if (existing != null)
            {
                var index = _presets.IndexOf(existing);
                _presets[index] = preset;
                SaveUserPresets();
                RebuildProcessMap();
            }
        }

        private void SaveUserPresets()
        {
            try
            {
                // Only save presets that differ from built-in or are new
                var userModified = new List<Preset>();

                // Load original built-in presets to compare
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = "Pie.presets.json";
                List<Preset> builtInPresets = new();

                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            var json = reader.ReadToEnd();
                            builtInPresets = JsonConvert.DeserializeObject<List<Preset>>(json) ?? new List<Preset>();
                        }
                    }
                }

                foreach (var preset in _presets)
                {
                    var builtIn = builtInPresets.FirstOrDefault(b => b.Id == preset.Id);
                    if (builtIn == null || !PresetsAreEqual(builtIn, preset))
                    {
                        userModified.Add(preset);
                    }
                }

                var dir = Path.GetDirectoryName(_userPresetsPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var userJson = JsonConvert.SerializeObject(userModified, Formatting.Indented);
                File.WriteAllText(_userPresetsPath, userJson);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save user presets: {ex.Message}");
            }
        }

        private bool PresetsAreEqual(Preset a, Preset b)
        {
            if (a.Name != b.Name || a.Description != b.Description) return false;
            if (a.ProcessNames.Count != b.ProcessNames.Count) return false;
            if (a.Actions.Count != b.Actions.Count) return false;

            for (int i = 0; i < a.Actions.Count; i++)
            {
                if (a.Actions[i].Name != b.Actions[i].Name ||
                    a.Actions[i].Shortcut != b.Actions[i].Shortcut)
                {
                    return false;
                }
            }

            return true;
        }
    }
}