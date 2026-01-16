using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Pie.Helpers
{
    public static class IconHelper
    {
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, ImageSource> _mediaIconCache = new();
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, ImageSource> _actionIconCache = new();

        public static ImageSource CreateMediaIcon(string iconType)
        {
            var key = iconType.ToLowerInvariant();
            if (_mediaIconCache.TryGetValue(key, out var cached))
            {
                return cached;
            }

            using var bitmap = new Bitmap(64, 64);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(System.Drawing.Color.Transparent);

            var primaryColor = System.Drawing.Color.FromArgb(0, 122, 255);
            using var brush = new SolidBrush(primaryColor);
            using var pen = new System.Drawing.Pen(primaryColor, 4);

            switch (iconType.ToLowerInvariant())
            {
                case "playpause":
                    // Play triangle and pause bars
                    graphics.FillPolygon(brush, new PointF[] {
                        new PointF(22, 16),
                        new PointF(22, 48),
                        new PointF(46, 32)
                    });
                    break;

                case "next":
                    // Next track icon (triangle + bar)
                    graphics.FillPolygon(brush, new PointF[] {
                        new PointF(16, 16),
                        new PointF(16, 48),
                        new PointF(40, 32)
                    });
                    graphics.FillRectangle(brush, 44, 16, 6, 32);
                    break;

                case "previous":
                    // Previous track icon (bar + triangle)
                    graphics.FillRectangle(brush, 14, 16, 6, 32);
                    graphics.FillPolygon(brush, new PointF[] {
                        new PointF(48, 16),
                        new PointF(48, 48),
                        new PointF(24, 32)
                    });
                    break;

                case "volumeup":
                    // Speaker with plus
                    DrawSpeaker(graphics, brush);
                    graphics.FillRectangle(brush, 44, 28, 12, 4);
                    graphics.FillRectangle(brush, 48, 24, 4, 12);
                    break;

                case "volumedown":
                    // Speaker with minus
                    DrawSpeaker(graphics, brush);
                    graphics.FillRectangle(brush, 44, 28, 12, 4);
                    break;

                case "mute":
                    // Speaker with X
                    DrawSpeaker(graphics, brush);
                    using (var redBrush = new SolidBrush(System.Drawing.Color.FromArgb(255, 59, 48)))
                    using (var redPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(255, 59, 48), 3))
                    {
                        graphics.DrawLine(redPen, 44, 20, 56, 44);
                        graphics.DrawLine(redPen, 56, 20, 44, 44);
                    }
                    break;

                case "stop":
                    // Stop square
                    graphics.FillRectangle(brush, 18, 18, 28, 28);
                    break;

                default:
                    // Default circle
                    graphics.FillEllipse(brush, 16, 16, 32, 32);
                    break;
            }

            var hBitmap = bitmap.GetHbitmap();
            try
            {
                var image = Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
                
                image.Freeze();
                _mediaIconCache.TryAdd(key, image);
                return image;
            }
            finally
            {
                DeleteObject(hBitmap);
            }
        }

        private static void DrawSpeaker(Graphics graphics, SolidBrush brush)
        {
            // Speaker cone
            graphics.FillPolygon(brush, new PointF[] {
                new PointF(8, 24),
                new PointF(8, 40),
                new PointF(18, 40),
                new PointF(30, 50),
                new PointF(30, 14),
                new PointF(18, 24)
            });
        }

        public static ImageSource CreateActionIcon(string iconName)
        {
            var key = iconName.ToLowerInvariant();
            if (_actionIconCache.TryGetValue(key, out var cached))
            {
                return cached;
            }

            using var bitmap = new Bitmap(64, 64);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(System.Drawing.Color.Transparent);

            var color = System.Drawing.Color.FromArgb(88, 86, 214); // Purple-ish
            using var brush = new SolidBrush(color);
            using var pen = new System.Drawing.Pen(color, 4);
            pen.StartCap = LineCap.Round;
            pen.EndCap = LineCap.Round;

            // Simple icon mapping
            if (key.Contains("search") || key.Contains("find"))
            {
                // Magnifying glass
                graphics.DrawEllipse(pen, 16, 16, 24, 24);
                graphics.DrawLine(pen, 38, 38, 52, 52);
            }
            else if (key.Contains("add") || key.Contains("new") || key.Contains("plus"))
            {
                // Plus sign
                graphics.FillRectangle(brush, 28, 12, 8, 40);
                graphics.FillRectangle(brush, 12, 28, 40, 8);
            }
            else if (key.Contains("remove") || key.Contains("delete") || key.Contains("close"))
            {
                // X mark
                graphics.DrawLine(pen, 16, 16, 48, 48);
                graphics.DrawLine(pen, 48, 16, 16, 48);
            }
            else if (key.Contains("save") || key.Contains("download"))
            {
                // Arrow down
                graphics.DrawLine(pen, 32, 16, 32, 44);
                graphics.DrawLine(pen, 20, 32, 32, 44);
                graphics.DrawLine(pen, 44, 32, 32, 44);
            }
            else if (key.Contains("undo") || key.Contains("back") || key.Contains("left"))
            {
                // Arrow left
                graphics.DrawLine(pen, 20, 32, 48, 32);
                graphics.DrawLine(pen, 20, 32, 32, 20);
                graphics.DrawLine(pen, 20, 32, 32, 44);
            }
            else if (key.Contains("redo") || key.Contains("forward") || key.Contains("right"))
            {
                // Arrow right
                graphics.DrawLine(pen, 16, 32, 44, 32);
                graphics.DrawLine(pen, 44, 32, 32, 20);
                graphics.DrawLine(pen, 44, 32, 32, 44);
            }
            else if (key.Contains("settings") || key.Contains("config") || key.Contains("properties"))
            {
                // Gear (simplified)
                graphics.DrawEllipse(pen, 20, 20, 24, 24);
                graphics.FillEllipse(brush, 26, 26, 12, 12);
            }
            else if (key.Contains("copy") || key.Contains("duplicate"))
            {
                // Two rectangles
                graphics.DrawRectangle(pen, 20, 20, 28, 28);
                graphics.DrawRectangle(pen, 12, 12, 28, 28);
            }
            else if (key.Contains("cut"))
            {
                // Scissors metaphor (X)
                graphics.DrawLine(pen, 16, 16, 48, 48);
                graphics.DrawLine(pen, 48, 16, 16, 48);
                graphics.DrawEllipse(pen, 12, 44, 8, 8);
                graphics.DrawEllipse(pen, 44, 44, 8, 8);
            }
            else if (key.Contains("terminal") || key.Contains("code") || key.Contains("console"))
            {
                // Terminal prompt >_
                graphics.DrawLine(pen, 16, 16, 32, 32);
                graphics.DrawLine(pen, 32, 32, 16, 48);
                graphics.DrawLine(pen, 36, 48, 52, 48);
            }
            else if (key.Contains("play") || key.Contains("run"))
            {
                // Play triangle
                graphics.FillPolygon(brush, new PointF[] { new PointF(20, 16), new PointF(20, 48), new PointF(48, 32) });
            }
            else if (key.Contains("refresh") || key.Contains("reload"))
            {
                // Circular arrow
                graphics.DrawArc(pen, 16, 16, 32, 32, 45, 270);
                graphics.DrawLine(pen, 36, 14, 48, 24); // Arrow head attempt
            }
            else if (key.Contains("command") || key.Contains("cmd") || key.Contains("palette"))
            {
                // Command icon (rectangle with loops or simplified grid)
                graphics.DrawRectangle(pen, 16, 16, 32, 32);
                graphics.DrawLine(pen, 24, 24, 40, 24);
                graphics.DrawLine(pen, 24, 32, 40, 32);
                graphics.DrawLine(pen, 24, 40, 40, 40);
            }
            else if (key.Contains("format") || key.Contains("align"))
            {
                // Format lines
                graphics.DrawLine(pen, 16, 16, 48, 16);
                graphics.DrawLine(pen, 16, 28, 36, 28);
                graphics.DrawLine(pen, 16, 40, 48, 40);
            }
            else if (key.Contains("view") || key.Contains("screen") || key.Contains("zen"))
            {
                // Screen / Zen mode
                graphics.DrawRectangle(pen, 12, 16, 40, 32);
                graphics.DrawLine(pen, 12, 40, 52, 40); // Bottom bar
            }
            else if (key.Contains("split"))
            {
                // Split view
                graphics.DrawRectangle(pen, 12, 16, 40, 32);
                graphics.DrawLine(pen, 32, 16, 32, 48); // Split line
            }
            else if (key.Contains("activity") || key.Contains("list") || key.Contains("task"))
            {
                // List icon
                graphics.FillEllipse(brush, 16, 18, 6, 6);
                graphics.DrawLine(pen, 28, 20, 48, 20);
                graphics.FillEllipse(brush, 16, 30, 6, 6);
                graphics.DrawLine(pen, 28, 32, 48, 32);
                graphics.FillEllipse(brush, 16, 42, 6, 6);
                graphics.DrawLine(pen, 28, 44, 48, 44);
            }
            else if (key.Contains("up") || key.Contains("level"))
            {
                // Up arrow
                graphics.DrawLine(pen, 32, 48, 32, 16);
                graphics.DrawLine(pen, 20, 28, 32, 16);
                graphics.DrawLine(pen, 44, 28, 32, 16);
            }
            else if (key.Contains("inprivate") || key.Contains("incognito"))
            {
                // Mask/Hat icon
                graphics.DrawArc(pen, 12, 20, 40, 20, 180, 180); // Hat top
                graphics.DrawLine(pen, 8, 30, 56, 30); // Brim
                graphics.DrawEllipse(pen, 16, 36, 12, 12); // Glasses L
                graphics.DrawEllipse(pen, 36, 36, 12, 12); // Glasses R
                graphics.DrawLine(pen, 28, 42, 36, 42); // Bridge
            }
            else if (key.Contains("book") || key.Contains("read"))
            {
                // Book icon
                graphics.DrawRectangle(pen, 16, 16, 32, 36);
                graphics.DrawLine(pen, 32, 16, 32, 52); // Spine
                graphics.DrawLine(pen, 16, 28, 48, 28);
            }
            else if (key.Contains("camera") || key.Contains("shot"))
            {
                // Camera icon
                graphics.DrawRectangle(pen, 12, 20, 40, 28);
                graphics.FillRectangle(brush, 24, 14, 16, 6); // Top
                graphics.DrawEllipse(pen, 24, 26, 16, 16); // Lens
            }
            else
            {
                // Fallback: Draw First Letter
                string letter = !string.IsNullOrEmpty(iconName) ? iconName.Substring(0, 1).ToUpper() : "?";

                using var font = new Font("Segoe UI", 28, System.Drawing.FontStyle.Bold);
                var stringFormat = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };

                graphics.DrawString(letter, font, brush, new RectangleF(0, 0, 64, 64), stringFormat);
            }

            var hBitmap = bitmap.GetHbitmap();
            try
            {
                var image = Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());

                image.Freeze();
                _actionIconCache.TryAdd(key, image);
                return image;
            }
            finally
            {
                DeleteObject(hBitmap);
            }
        }

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);
    }
}
