using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Pie.Models;
using Pie.Services;

namespace Pie.Controls
{
    public class PieMenuControl : Canvas
    {
        private readonly List<PieMenuItemVisual> _itemVisuals = new();
        private int _selectedIndex = -1;
        private int _previousSelectedIndex = -1;
        private double _centerX;
        private double _centerY;
        private Ellipse? _centerCircle;
        private Ellipse? _backgroundCircle;
        private TextBlock? _centerLabel;
        private Border? _centerLabelBorder;
        private bool _isAnimatingIn;

        public event EventHandler<PieMenuItem>? ItemSelected;
        public event EventHandler? MenuClosed;

        public static readonly DependencyProperty MenuRadiusProperty =
            DependencyProperty.Register(nameof(MenuRadius), typeof(double), typeof(PieMenuControl),
                new PropertyMetadata(180.0, OnLayoutPropertyChanged));

        public static readonly DependencyProperty IconSizeProperty =
            DependencyProperty.Register(nameof(IconSize), typeof(double), typeof(PieMenuControl),
                new PropertyMetadata(48.0, OnLayoutPropertyChanged));

        public static readonly DependencyProperty InnerRadiusProperty =
            DependencyProperty.Register(nameof(InnerRadius), typeof(double), typeof(PieMenuControl),
                new PropertyMetadata(50.0, OnLayoutPropertyChanged));

        public double MenuRadius
        {
            get => (double)GetValue(MenuRadiusProperty);
            set => SetValue(MenuRadiusProperty, value);
        }

        public double IconSize
        {
            get => (double)GetValue(IconSizeProperty);
            set => SetValue(IconSizeProperty, value);
        }

        public double InnerRadius
        {
            get => (double)GetValue(InnerRadiusProperty);
            set => SetValue(InnerRadiusProperty, value);
        }

        private static void OnLayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PieMenuControl control)
            {
                control.UpdateLayout();
            }
        }

        public SoundService? SoundService { get; set; }

        public void SetItems(IEnumerable<PieMenuItem> items)
        {
            ClearVisuals();
            var itemList = items.ToList();

            if (itemList.Count == 0) return;

            double totalSize = (MenuRadius + IconSize) * 2 + 40;
            Width = totalSize;
            Height = totalSize;
            _centerX = totalSize / 2;
            _centerY = totalSize / 2;

            CreateBackground();
            CreateItemVisuals(itemList);
            CreateCenterElements();
        }

        private void ClearVisuals()
        {
            Children.Clear();
            _itemVisuals.Clear();
            _selectedIndex = -1;
            _previousSelectedIndex = -1;
        }

        private void CreateBackground()
        {
            _backgroundCircle = new Ellipse
            {
                Width = MenuRadius * 2 + IconSize + 20,
                Height = MenuRadius * 2 + IconSize + 20,
                Fill = new SolidColorBrush(Color.FromArgb(220, 180, 200, 220)),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 40,
                    ShadowDepth = 0,
                    Color = Color.FromArgb(80, 0, 0, 0),
                    Opacity = 0.5
                }
            };
            SetLeft(_backgroundCircle, _centerX - _backgroundCircle.Width / 2);
            SetTop(_backgroundCircle, _centerY - _backgroundCircle.Height / 2);
            Children.Add(_backgroundCircle);

            // Inner cutout circle (creates the ring effect)
            _centerCircle = new Ellipse
            {
                Width = InnerRadius * 2,
                Height = InnerRadius * 2,
                Fill = new SolidColorBrush(Color.FromArgb(255, 200, 215, 230))
            };
            SetLeft(_centerCircle, _centerX - InnerRadius);
            SetTop(_centerCircle, _centerY - InnerRadius);
            Children.Add(_centerCircle);
        }

        private void CreateItemVisuals(List<PieMenuItem> items)
        {
            double angleStep = 360.0 / items.Count;
            double startAngle = -90; // Start from top

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                double angle = startAngle + i * angleStep;
                double radians = angle * Math.PI / 180;

                double iconCenterX = _centerX + MenuRadius * Math.Cos(radians);
                double iconCenterY = _centerY + MenuRadius * Math.Sin(radians);

                var visual = CreateItemVisual(item, iconCenterX, iconCenterY, i);
                _itemVisuals.Add(visual);

                // Add segment highlight (invisible by default)
                // Segment should be centered on the icon, so start at iconAngle - halfAngleStep
                CreateSegmentHighlight(i, startAngle + i * angleStep - angleStep / 2, angleStep);
            }
        }

        private void CreateSegmentHighlight(int index, double startAngle, double sweepAngle)
        {
            var highlight = new Path
            {
                Fill = new SolidColorBrush(Color.FromArgb(60, 100, 150, 200)),
                Opacity = 0,
                Tag = $"highlight_{index}"
            };

            // startAngle is already in the correct coordinate system (where -90 is top)
            // CreateArcGeometry expects angle where 0 is right, so we need to keep the same offset
            var geometry = CreateArcGeometry(startAngle, sweepAngle, MenuRadius + IconSize / 2 + 10, InnerRadius);
            highlight.Data = geometry;
            SetLeft(highlight, 0);
            SetTop(highlight, 0);
            Children.Insert(1, highlight); // After background, before items
        }

        private PathGeometry CreateArcGeometry(double startAngle, double sweepAngle, double outerRadius, double innerRadius)
        {
            var geometry = new PathGeometry();
            var figure = new PathFigure();

            double startRad = startAngle * Math.PI / 180;
            double endRad = (startAngle + sweepAngle) * Math.PI / 180;

            // Outer arc start point
            figure.StartPoint = new Point(
                _centerX + outerRadius * Math.Cos(startRad),
                _centerY + outerRadius * Math.Sin(startRad));

            // Outer arc
            figure.Segments.Add(new ArcSegment(
                new Point(
                    _centerX + outerRadius * Math.Cos(endRad),
                    _centerY + outerRadius * Math.Sin(endRad)),
                new Size(outerRadius, outerRadius),
                0,
                sweepAngle > 180,
                SweepDirection.Clockwise,
                true));

            // Line to inner arc
            figure.Segments.Add(new LineSegment(
                new Point(
                    _centerX + innerRadius * Math.Cos(endRad),
                    _centerY + innerRadius * Math.Sin(endRad)),
                true));

            // Inner arc (reverse direction)
            figure.Segments.Add(new ArcSegment(
                new Point(
                    _centerX + innerRadius * Math.Cos(startRad),
                    _centerY + innerRadius * Math.Sin(startRad)),
                new Size(innerRadius, innerRadius),
                0,
                sweepAngle > 180,
                SweepDirection.Counterclockwise,
                true));

            figure.IsClosed = true;
            geometry.Figures.Add(figure);

            return geometry;
        }

        private PieMenuItemVisual CreateItemVisual(PieMenuItem item, double x, double y, int index)
        {
            var transformGroup = new TransformGroup();
            transformGroup.Children.Add(new ScaleTransform(1, 1));
            transformGroup.Children.Add(new TranslateTransform(0, 0));

            var container = new Border
            {
                Width = IconSize + 16,
                Height = IconSize + 16,
                CornerRadius = new CornerRadius(12),
                Background = new SolidColorBrush(Colors.White),
                BorderBrush = new SolidColorBrush(Color.FromArgb(40, 0, 0, 0)),
                BorderThickness = new Thickness(1),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 12,
                    ShadowDepth = 2,
                    Color = Colors.Black,
                    Opacity = 0.15
                },
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = transformGroup,
                Opacity = 0,
                Tag = index
            };

            var image = new Image
            {
                Width = IconSize,
                Height = IconSize,
                Source = item.Icon,
                Stretch = Stretch.Uniform
            };
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);

            container.Child = image;
            SetLeft(container, x - container.Width / 2);
            SetTop(container, y - container.Height / 2);
            Children.Add(container);

            return new PieMenuItemVisual
            {
                Item = item,
                Container = container,
                Index = index,
                TargetX = x,
                TargetY = y
            };
        }

        private void CreateCenterElements()
        {
            _centerLabelBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(230, 50, 50, 50)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 6, 12, 6),
                Opacity = 0
            };

            _centerLabel = new TextBlock
            {
                Text = "",
                Foreground = Brushes.White,
                FontSize = 13,
                FontWeight = FontWeights.Medium,
                TextAlignment = TextAlignment.Center
            };

            _centerLabelBorder.Child = _centerLabel;
            Children.Add(_centerLabelBorder);
            UpdateCenterLabelPosition();
        }

        private void UpdateCenterLabelPosition()
        {
            if (_centerLabelBorder == null) return;
            _centerLabelBorder.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            SetLeft(_centerLabelBorder, _centerX - _centerLabelBorder.DesiredSize.Width / 2);
            SetTop(_centerLabelBorder, _centerY - _centerLabelBorder.DesiredSize.Height / 2);
        }

        public void ShowImmediate()
        {
            if (_backgroundCircle != null)
            {
                _backgroundCircle.Opacity = 1;
                _backgroundCircle.RenderTransform = new ScaleTransform(1, 1);
            }

            if (_centerCircle != null)
            {
                _centerCircle.Opacity = 1;
                _centerCircle.RenderTransform = new ScaleTransform(1, 1);
            }

            foreach (var visual in _itemVisuals)
            {
                visual.Container.Opacity = 1;
                var transformGroup = new TransformGroup();
                transformGroup.Children.Add(new ScaleTransform(1, 1));
                transformGroup.Children.Add(new TranslateTransform(0, 0));
                visual.Container.RenderTransform = transformGroup;
            }
        }

        public void AnimateIn()
        {
            LogService.Debug("AnimateIn called");
            _isAnimatingIn = true;

            var duration = TimeSpan.FromMilliseconds(380);
            var springEasing = new ElasticEase { Oscillations = 1, Springiness = 6, EasingMode = EasingMode.EaseOut };
            var backEasing = new BackEase { Amplitude = 0.6, EasingMode = EasingMode.EaseOut };

            if (_backgroundCircle != null)
            {
                var transformGroup = new TransformGroup();
                var scaleTransform = new ScaleTransform(0.8, 0.8);
                var rotateTransform = new RotateTransform(-20);
                transformGroup.Children.Add(scaleTransform);
                transformGroup.Children.Add(rotateTransform);

                _backgroundCircle.RenderTransformOrigin = new Point(0.5, 0.5);
                _backgroundCircle.RenderTransform = transformGroup;
                _backgroundCircle.Opacity = 0;

                var scaleXAnim = new DoubleAnimation(0.8, 1.0, duration) { EasingFunction = backEasing };
                var scaleYAnim = new DoubleAnimation(0.8, 1.0, duration) { EasingFunction = backEasing };
                var rotateAnim = new DoubleAnimation(-20, 0, duration) { EasingFunction = backEasing };
                var opacityAnim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));

                scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleXAnim);
                scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleYAnim);
                rotateTransform.BeginAnimation(RotateTransform.AngleProperty, rotateAnim);
                _backgroundCircle.BeginAnimation(OpacityProperty, opacityAnim);
            }

            if (_centerCircle != null)
            {
                var transformGroup = new TransformGroup();
                var scaleTransform = new ScaleTransform(0.8, 0.8);
                var rotateTransform = new RotateTransform(-20);
                transformGroup.Children.Add(scaleTransform);
                transformGroup.Children.Add(rotateTransform);

                _centerCircle.RenderTransformOrigin = new Point(0.5, 0.5);
                _centerCircle.RenderTransform = transformGroup;
                _centerCircle.Opacity = 0;

                var scaleXAnim = new DoubleAnimation(0.8, 1.0, duration) { EasingFunction = backEasing };
                var scaleYAnim = new DoubleAnimation(0.8, 1.0, duration) { EasingFunction = backEasing };
                var rotateAnim = new DoubleAnimation(-20, 0, duration) { EasingFunction = backEasing };
                var opacityAnim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));

                scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleXAnim);
                scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleYAnim);
                rotateTransform.BeginAnimation(RotateTransform.AngleProperty, rotateAnim);
                _centerCircle.BeginAnimation(OpacityProperty, opacityAnim);
            }

            for (int i = 0; i < _itemVisuals.Count; i++)
            {
                var visual = _itemVisuals[i];
                var delay = TimeSpan.FromMilliseconds(i * 30);
                var itemDuration = TimeSpan.FromMilliseconds(400);

                var transformGroup = visual.Container.RenderTransform as TransformGroup;
                if (transformGroup == null || transformGroup.Children.Count < 2)
                {
                    transformGroup = new TransformGroup();
                    transformGroup.Children.Add(new ScaleTransform(1, 1));
                    transformGroup.Children.Add(new TranslateTransform(0, 0));
                    visual.Container.RenderTransform = transformGroup;
                }

                var scaleTransform = transformGroup.Children[0] as ScaleTransform;
                var translateTransform = transformGroup.Children[1] as TranslateTransform;

                if (scaleTransform != null)
                {
                    scaleTransform.ScaleX = 0.5;
                    scaleTransform.ScaleY = 0.5;
                }

                double angleRad = Math.Atan2(visual.TargetY - _centerY, visual.TargetX - _centerX);
                double offsetDistance = 20;
                if (translateTransform != null)
                {
                    translateTransform.X = -Math.Cos(angleRad) * offsetDistance;
                    translateTransform.Y = -Math.Sin(angleRad) * offsetDistance;
                }

                var scaleXAnim = new DoubleAnimation(0.5, 1.0, itemDuration)
                {
                    EasingFunction = springEasing,
                    BeginTime = delay
                };
                var scaleYAnim = new DoubleAnimation(0.5, 1.0, itemDuration)
                {
                    EasingFunction = springEasing,
                    BeginTime = delay
                };
                var translateXAnim = new DoubleAnimation(0, itemDuration)
                {
                    EasingFunction = springEasing,
                    BeginTime = delay
                };
                var translateYAnim = new DoubleAnimation(0, itemDuration)
                {
                    EasingFunction = springEasing,
                    BeginTime = delay
                };
                var opacityAnim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200))
                {
                    BeginTime = delay
                };

                scaleTransform?.BeginAnimation(ScaleTransform.ScaleXProperty, scaleXAnim);
                scaleTransform?.BeginAnimation(ScaleTransform.ScaleYProperty, scaleYAnim);
                translateTransform?.BeginAnimation(TranslateTransform.XProperty, translateXAnim);
                translateTransform?.BeginAnimation(TranslateTransform.YProperty, translateYAnim);
                visual.Container.BeginAnimation(OpacityProperty, opacityAnim);
            }

            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                _isAnimatingIn = false;
            };
            timer.Start();
        }

        public void AnimateOut(Action? onComplete = null)
        {
            var duration = TimeSpan.FromMilliseconds(180);
            var easing = new CubicEase { EasingMode = EasingMode.EaseIn };

            if (_backgroundCircle != null)
            {
                var transformGroup = new TransformGroup();
                var scaleTransform = new ScaleTransform(1, 1);
                var rotateTransform = new RotateTransform(0);
                transformGroup.Children.Add(scaleTransform);
                transformGroup.Children.Add(rotateTransform);

                _backgroundCircle.RenderTransformOrigin = new Point(0.5, 0.5);
                _backgroundCircle.RenderTransform = transformGroup;

                var scaleAnim = new DoubleAnimation(1.0, 0.95, duration) { EasingFunction = easing };
                var rotateAnim = new DoubleAnimation(0, 5, duration) { EasingFunction = easing };

                scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
                scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);
                rotateTransform.BeginAnimation(RotateTransform.AngleProperty, rotateAnim);
            }

            if (_centerCircle != null)
            {
                var transformGroup = new TransformGroup();
                var scaleTransform = new ScaleTransform(1, 1);
                var rotateTransform = new RotateTransform(0);
                transformGroup.Children.Add(scaleTransform);
                transformGroup.Children.Add(rotateTransform);

                _centerCircle.RenderTransformOrigin = new Point(0.5, 0.5);
                _centerCircle.RenderTransform = transformGroup;

                var scaleAnim = new DoubleAnimation(1.0, 0.95, duration) { EasingFunction = easing };
                var rotateAnim = new DoubleAnimation(0, 5, duration) { EasingFunction = easing };

                scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
                scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);
                rotateTransform.BeginAnimation(RotateTransform.AngleProperty, rotateAnim);
            }

            var opacityAnim = new DoubleAnimation(1, 0, duration)
            {
                EasingFunction = easing
            };

            opacityAnim.Completed += (s, e) => onComplete?.Invoke();
            this.BeginAnimation(OpacityProperty, opacityAnim);
        }

        public void UpdateSelection(Point mousePosition)
        {
            if (_isAnimatingIn || _itemVisuals.Count == 0) return;

            double dx = mousePosition.X - _centerX;
            double dy = mousePosition.Y - _centerY;
            double distance = Math.Sqrt(dx * dx + dy * dy);

            // Minimum distance from center to select (Inner Radius)
            // Maximum distance to select (Outer Radius + buffer)
            double maxRadius = MenuRadius + IconSize + 30;

            if (distance < InnerRadius * 0.8 || distance > maxRadius)
            {
                SetSelectedIndex(-1);
                return;
            }

            double angle = Math.Atan2(dy, dx) * 180 / Math.PI;
            angle = (angle + 90 + 360) % 360; // Adjust for top-start

            double angleStep = 360.0 / _itemVisuals.Count;
            int index = (int)((angle + angleStep / 2) % 360 / angleStep);
            index = Math.Clamp(index, 0, _itemVisuals.Count - 1);

            SetSelectedIndex(index);
        }

        private void SetSelectedIndex(int index)
        {
            if (index == _selectedIndex) return;

            _previousSelectedIndex = _selectedIndex;
            _selectedIndex = index;

            // Deselect previous
            if (_previousSelectedIndex >= 0 && _previousSelectedIndex < _itemVisuals.Count)
            {
                AnimateItemDeselected(_itemVisuals[_previousSelectedIndex]);
                HideSegmentHighlight(_previousSelectedIndex);
            }

            // Select new
            if (_selectedIndex >= 0 && _selectedIndex < _itemVisuals.Count)
            {
                AnimateItemSelected(_itemVisuals[_selectedIndex]);
                ShowSegmentHighlight(_selectedIndex);
                UpdateCenterLabel(_itemVisuals[_selectedIndex].Item.Name);
                SoundService?.PlayHoverSound();
            }
            else
            {
                HideCenterLabel();
            }
        }

        private void AnimateItemSelected(PieMenuItemVisual visual)
        {
            var transformGroup = visual.Container.RenderTransform as TransformGroup;
            if (transformGroup == null || transformGroup.Children.Count < 2)
            {
                transformGroup = new TransformGroup();
                transformGroup.Children.Add(new ScaleTransform(1, 1));
                transformGroup.Children.Add(new TranslateTransform(0, 0));
                visual.Container.RenderTransform = transformGroup;
            }

            var scaleTransform = transformGroup.Children[0] as ScaleTransform;
            var translateTransform = transformGroup.Children[1] as TranslateTransform;

            var duration = TimeSpan.FromMilliseconds(180);
            var springEasing = new ElasticEase { Oscillations = 1, Springiness = 8, EasingMode = EasingMode.EaseOut };

            double angleRad = Math.Atan2(visual.TargetY - _centerY, visual.TargetX - _centerX);
            double outwardOffset = 15;

            var scaleXAnim = new DoubleAnimation(1.3, duration) { EasingFunction = springEasing };
            var scaleYAnim = new DoubleAnimation(1.3, duration) { EasingFunction = springEasing };
            var translateXAnim = new DoubleAnimation(Math.Cos(angleRad) * outwardOffset, duration) { EasingFunction = springEasing };
            var translateYAnim = new DoubleAnimation(Math.Sin(angleRad) * outwardOffset, duration) { EasingFunction = springEasing };

            scaleTransform?.BeginAnimation(ScaleTransform.ScaleXProperty, scaleXAnim);
            scaleTransform?.BeginAnimation(ScaleTransform.ScaleYProperty, scaleYAnim);
            translateTransform?.BeginAnimation(TranslateTransform.XProperty, translateXAnim);
            translateTransform?.BeginAnimation(TranslateTransform.YProperty, translateYAnim);
        }

        private void AnimateItemDeselected(PieMenuItemVisual visual)
        {
            var transformGroup = visual.Container.RenderTransform as TransformGroup;
            if (transformGroup == null || transformGroup.Children.Count < 2)
            {
                transformGroup = new TransformGroup();
                transformGroup.Children.Add(new ScaleTransform(1, 1));
                transformGroup.Children.Add(new TranslateTransform(0, 0));
                visual.Container.RenderTransform = transformGroup;
            }

            var scaleTransform = transformGroup.Children[0] as ScaleTransform;
            var translateTransform = transformGroup.Children[1] as TranslateTransform;

            var duration = TimeSpan.FromMilliseconds(150);
            var easing = new CubicEase { EasingMode = EasingMode.EaseOut };

            var scaleXAnim = new DoubleAnimation(1.0, duration) { EasingFunction = easing };
            var scaleYAnim = new DoubleAnimation(1.0, duration) { EasingFunction = easing };
            var translateXAnim = new DoubleAnimation(0, duration) { EasingFunction = easing };
            var translateYAnim = new DoubleAnimation(0, duration) { EasingFunction = easing };

            scaleTransform?.BeginAnimation(ScaleTransform.ScaleXProperty, scaleXAnim);
            scaleTransform?.BeginAnimation(ScaleTransform.ScaleYProperty, scaleYAnim);
            translateTransform?.BeginAnimation(TranslateTransform.XProperty, translateXAnim);
            translateTransform?.BeginAnimation(TranslateTransform.YProperty, translateYAnim);
        }

        private void ShowSegmentHighlight(int index)
        {
            foreach (UIElement child in Children)
            {
                if (child is Path path && path.Tag?.ToString() == $"highlight_{index}")
                {
                    var anim = new DoubleAnimation(1, TimeSpan.FromMilliseconds(100));
                    path.BeginAnimation(OpacityProperty, anim);
                    break;
                }
            }
        }

        private void HideSegmentHighlight(int index)
        {
            foreach (UIElement child in Children)
            {
                if (child is Path path && path.Tag?.ToString() == $"highlight_{index}")
                {
                    var anim = new DoubleAnimation(0, TimeSpan.FromMilliseconds(100));
                    path.BeginAnimation(OpacityProperty, anim);
                    break;
                }
            }
        }

        private void UpdateCenterLabel(string text)
        {
            if (_centerLabel == null || _centerLabelBorder == null) return;

            _centerLabel.Text = text;
            UpdateCenterLabelPosition();

            var anim = new DoubleAnimation(1, TimeSpan.FromMilliseconds(100));
            _centerLabelBorder.BeginAnimation(OpacityProperty, anim);
        }

        private void HideCenterLabel()
        {
            if (_centerLabelBorder == null) return;
            var anim = new DoubleAnimation(0, TimeSpan.FromMilliseconds(100));
            _centerLabelBorder.BeginAnimation(OpacityProperty, anim);
        }

        public PieMenuItem? GetSelectedItem()
        {
            if (_selectedIndex >= 0 && _selectedIndex < _itemVisuals.Count)
            {
                return _itemVisuals[_selectedIndex].Item;
            }
            return null;
        }

        public void ClearSelection()
        {
            SetSelectedIndex(-1);
        }

        public void ConfirmSelection()
        {
            var selectedItem = GetSelectedItem();
            LogService.Debug($"ConfirmSelection called - selectedItem: {selectedItem?.Name ?? "null"}");
            if (selectedItem != null)
            {
                SoundService?.PlaySelectSound();
                ItemSelected?.Invoke(this, selectedItem);
            }
            else
            {
                LogService.Debug("No item selected - invoking MenuClosed");
                MenuClosed?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public class PieMenuItemVisual
    {
        public PieMenuItem Item { get; set; } = null!;
        public Border Container { get; set; } = null!;
        public int Index { get; set; }
        public double TargetX { get; set; }
        public double TargetY { get; set; }
    }
}
