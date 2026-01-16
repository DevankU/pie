using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Pie.Models;

namespace Pie.Services
{
    public class WindowService
    {
        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr hIcon);

        private const int SW_RESTORE = 9;
        private const int SW_SHOW = 5;
        private const int GWL_STYLE = -16;
        private const int GWL_EXSTYLE = -20;
        private const long WS_VISIBLE = 0x10000000L;
        private const long WS_EX_TOOLWINDOW = 0x00000080L;
        private const long WS_EX_APPWINDOW = 0x00040000L;

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        private readonly SettingsService _settingsService;
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, ImageSource> _fileIconCache = new();

        public WindowService(SettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        public List<PieMenuItem> GetRunningApplications()
        {
            var apps = new List<PieMenuItem>();
            var processedHandles = new HashSet<IntPtr>();
            var excludedApps = _settingsService.Settings.ExcludedApps
                .Select(a => a.ToLowerInvariant())
                .ToHashSet();

            EnumWindows((hWnd, lParam) =>
            {
                if (!IsWindowVisible(hWnd)) return true;

                int length = GetWindowTextLength(hWnd);
                if (length == 0) return true;

                int style = GetWindowLong(hWnd, GWL_STYLE);
                int exStyle = GetWindowLong(hWnd, GWL_EXSTYLE);

                if ((exStyle & (int)WS_EX_TOOLWINDOW) != 0 && (exStyle & (int)WS_EX_APPWINDOW) == 0)
                    return true;

                if (processedHandles.Contains(hWnd)) return true;

                GetWindowThreadProcessId(hWnd, out uint processId);

                try
                {
                    var process = Process.GetProcessById((int)processId);
                    var processName = process.ProcessName.ToLowerInvariant();

                    if (excludedApps.Contains(processName)) return true;

                    var sb = new System.Text.StringBuilder(length + 1);
                    GetWindowText(hWnd, sb, sb.Capacity);
                    var title = sb.ToString();

                    if (string.IsNullOrWhiteSpace(title)) return true;

                    string? exePath = null;
                    try
                    {
                        exePath = process.MainModule?.FileName;
                    }
                    catch { }

                    var item = new PieMenuItem
                    {
                        Name = title.Length > 30 ? title.Substring(0, 27) + "..." : title,
                        Path = exePath ?? process.ProcessName,
                        Type = PieMenuItemType.Application,
                        IsRunning = true,
                        WindowHandle = hWnd,
                        ProcessName = process.ProcessName,
                        Icon = exePath != null ? GetIconFromFile(exePath) : null
                    };

                    apps.Add(item);
                    processedHandles.Add(hWnd);
                }
                catch { }

                return true;
            }, IntPtr.Zero);

            return apps;
        }

        public void SwitchToWindow(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return;

            var foregroundWindow = GetForegroundWindow();
            GetWindowThreadProcessId(foregroundWindow, out uint foregroundThread);
            var currentThread = GetCurrentThreadId();

            if (foregroundThread != currentThread)
            {
                AttachThreadInput(currentThread, foregroundThread, true);
            }

            if (IsIconic(hWnd))
            {
                ShowWindow(hWnd, SW_RESTORE);
            }
            else
            {
                ShowWindow(hWnd, SW_SHOW);
            }

            SetForegroundWindow(hWnd);

            if (foregroundThread != currentThread)
            {
                AttachThreadInput(currentThread, foregroundThread, false);
            }
        }

        public string? GetForegroundProcessName()
        {
            var hWnd = GetForegroundWindow();
            if (hWnd == IntPtr.Zero) return null;

            GetWindowThreadProcessId(hWnd, out uint processId);
            try
            {
                var process = Process.GetProcessById((int)processId);
                return process.ProcessName;
            }
            catch
            {
                return null;
            }
        }

        public string? GetProcessNameFromWindow(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return null;

            GetWindowThreadProcessId(hWnd, out uint processId);
            try
            {
                var process = Process.GetProcessById((int)processId);
                return process.ProcessName;
            }
            catch
            {
                return null;
            }
        }

        public ImageSource? GetIconFromFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return null;
            if (_fileIconCache.TryGetValue(filePath, out var cached)) return cached;

            try
            {
                if (!File.Exists(filePath)) return null;

                using var icon = Icon.ExtractAssociatedIcon(filePath);
                if (icon == null) return null;

                var image = Imaging.CreateBitmapSourceFromHIcon(
                    icon.Handle,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
                
                image.Freeze();
                _fileIconCache.TryAdd(filePath, image);
                return image;
            }
            catch
            {
                return null;
            }
        }

        public ImageSource? GetFolderIcon()
        {
            const string cacheKey = "::system_folder_icon::";
            if (_fileIconCache.TryGetValue(cacheKey, out var cached)) return cached;

            try
            {
                var shellPath = Environment.GetFolderPath(Environment.SpecialFolder.System);
                var shell32Path = Path.Combine(shellPath, "shell32.dll");
                var hIcon = ExtractIcon(IntPtr.Zero, shell32Path, 3);
                if (hIcon == IntPtr.Zero) return null;

                var icon = Icon.FromHandle(hIcon);
                var bitmapSource = Imaging.CreateBitmapSourceFromHIcon(
                    icon.Handle,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());

                DestroyIcon(hIcon);
                bitmapSource.Freeze();
                _fileIconCache.TryAdd(cacheKey, bitmapSource);
                return bitmapSource;
            }
            catch
            {
                return null;
            }
        }

        public ImageSource? CreateStackedGroupIcon(IEnumerable<string> appPaths, int size = 48)
        {
            try
            {
                var paths = appPaths.Take(4).ToList();
                if (paths.Count == 0) return null;

                int canvasSize = size + 16;
                var drawingVisual = new System.Windows.Media.DrawingVisual();

                using (var context = drawingVisual.RenderOpen())
                {
                    int offset = 0;
                    int offsetStep = paths.Count switch
                    {
                        1 => 0,
                        2 => 10,
                        3 => 7,
                        _ => 5
                    };

                    int iconSize = paths.Count switch
                    {
                        1 => size,
                        2 => (int)(size * 0.75),
                        _ => (int)(size * 0.65)
                    };

                    foreach (var path in paths)
                    {
                        var iconSource = GetIconFromFile(path);
                        if (iconSource != null)
                        {
                            context.DrawImage(iconSource, new Rect(offset, offset, iconSize, iconSize));
                        }
                        offset += offsetStep;
                    }
                }

                var renderTarget = new RenderTargetBitmap(canvasSize, canvasSize, 96, 96, PixelFormats.Pbgra32);
                renderTarget.Render(drawingVisual);
                renderTarget.Freeze();

                return renderTarget;
            }
            catch
            {
                return null;
            }
        }
    }
}
