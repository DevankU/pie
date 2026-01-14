using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Pie.Helpers
{
    public class InstalledApp
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string ShortcutPath { get; set; } = string.Empty;
    }

    public static class InstalledAppsHelper
    {
        public static List<InstalledApp> GetInstalledApplications()
        {
            var apps = new Dictionary<string, InstalledApp>(StringComparer.OrdinalIgnoreCase);

            // Get apps from common Start Menu
            var commonStartMenu = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu);
            var commonPrograms = System.IO.Path.Combine(commonStartMenu, "Programs");
            if (Directory.Exists(commonPrograms))
            {
                EnumerateShortcuts(commonPrograms, apps);
            }

            // Get apps from user Start Menu
            var userStartMenu = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
            var userPrograms = System.IO.Path.Combine(userStartMenu, "Programs");
            if (Directory.Exists(userPrograms))
            {
                EnumerateShortcuts(userPrograms, apps);
            }

            // Sort alphabetically by name
            return apps.Values.OrderBy(a => a.Name).ToList();
        }

        private static void EnumerateShortcuts(string directory, Dictionary<string, InstalledApp> apps)
        {
            try
            {
                // Get all .lnk files recursively
                foreach (var shortcutPath in Directory.GetFiles(directory, "*.lnk", SearchOption.AllDirectories))
                {
                    try
                    {
                        var targetPath = GetShortcutTarget(shortcutPath);
                        if (!string.IsNullOrEmpty(targetPath) &&
                            targetPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) &&
                            File.Exists(targetPath))
                        {
                            // Skip uninstallers and helper apps
                            var fileName = System.IO.Path.GetFileNameWithoutExtension(shortcutPath);
                            if (IsValidAppName(fileName))
                            {
                                // Use target path as key to avoid duplicates
                                if (!apps.ContainsKey(targetPath))
                                {
                                    apps[targetPath] = new InstalledApp
                                    {
                                        Name = fileName,
                                        Path = targetPath,
                                        ShortcutPath = shortcutPath
                                    };
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Skip invalid shortcuts
                    }
                }
            }
            catch
            {
                // Skip inaccessible directories
            }
        }

        private static string GetShortcutTarget(string shortcutPath)
        {
            try
            {
                // Use dynamic COM to access WScript.Shell
                var shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null) return string.Empty;

                dynamic shell = Activator.CreateInstance(shellType)!;
                dynamic shortcut = shell.CreateShortcut(shortcutPath);
                string targetPath = shortcut.TargetPath;

                // Release COM objects
                System.Runtime.InteropServices.Marshal.ReleaseComObject(shortcut);
                System.Runtime.InteropServices.Marshal.ReleaseComObject(shell);

                return targetPath;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static bool IsValidAppName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;

            var lowerName = name.ToLowerInvariant();

            // Skip common uninstaller and helper app patterns
            var skipPatterns = new[]
            {
                "uninstall", "uninst", "remove", "setup", "install",
                "readme", "help", "license", "changelog", "release notes",
                "documentation", "manual", "guide", "website", "support",
                "update", "updater", "repair", "configure", "config",
                "debug", "crash", "error", "report"
            };

            foreach (var pattern in skipPatterns)
            {
                if (lowerName.Contains(pattern)) return false;
            }

            return true;
        }
    }
}
