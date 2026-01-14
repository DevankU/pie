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
        public static ImageSource CreateMediaIcon(string iconType)
        {
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
                return Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
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
            using var bitmap = new Bitmap(64, 64);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(System.Drawing.Color.Transparent);

            var primaryColor = System.Drawing.Color.FromArgb(88, 86, 214);
            using var brush = new SolidBrush(primaryColor);
            using var pen = new System.Drawing.Pen(primaryColor, 3);

            // Draw a generic action icon (gear/cog shape)
            var center = new PointF(32, 32);
            int teeth = 8;
            float outerRadius = 24;
            float innerRadius = 16;

            var path = new GraphicsPath();
            for (int i = 0; i < teeth * 2; i++)
            {
                float angle = (float)(i * Math.PI / teeth);
                float radius = (i % 2 == 0) ? outerRadius : innerRadius;
                float x = center.X + radius * (float)Math.Cos(angle);
                float y = center.Y + radius * (float)Math.Sin(angle);
                if (i == 0)
                    path.StartFigure();
                else
                    path.AddLine(path.GetLastPoint(), new PointF(x, y));
            }
            path.CloseFigure();
            graphics.FillPath(brush, path);

            // Center hole
            using var whiteBrush = new SolidBrush(System.Drawing.Color.White);
            graphics.FillEllipse(whiteBrush, 24, 24, 16, 16);

            var hBitmap = bitmap.GetHbitmap();
            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
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
