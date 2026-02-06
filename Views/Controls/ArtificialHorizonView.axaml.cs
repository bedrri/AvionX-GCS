using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Markup.Xaml;

namespace AvionX.Views.Controls
{
    /// <summary>
    /// Artificial Horizon - Professional Aviation Grade
    /// Based on artificialHorizon reference implementation
    ///
    /// DESIGN PATTERN: Custom Control with Dynamic Canvas Rendering
    /// - Properties: PitchAngle, RollAngle, YawAngle
    /// - Auto-redraw on property changes
    /// - Multiple canvas layers for performance
    ///
    /// AVIATION PHYSICS:
    /// - 36° vertical FOV (Field of View)
    /// - 45° horizontal FOV
    /// - 26° yaw compass FOV
    /// - Pitch: Positive = nose up, Negative = nose down
    /// - Roll: Positive = right wing down, Negative = left wing up
    /// </summary>
    public partial class ArtificialHorizonView : UserControl
    {
        // FOV Constants (from artificialHorizon reference)
        private const int VERTICAL_DEG_TO_DISP = 36;
        private const int HORIZONTAL_DEG_TO_DISP = 45;
        private const int YAW_COMPASS_DEG_TO_DISP = 26;

        // Canvas references (using PART_ prefix to avoid Name generator conflicts)
        private Grid? Grid_Viewport;
        private Grid? Grid_PitchIndicator;
        private Grid? Grid_Compass;
        private Canvas? Canvas_Background;
        private Canvas? Canvas_PitchIndicator;
        private Canvas? Canvas_HUD;
        private Canvas? Canvas_Compass;

        public ArtificialHorizonView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            // Get references after XAML load (PART_ names)
            Grid_Viewport = this.Find<Grid>("PART_Viewport");
            Grid_PitchIndicator = this.Find<Grid>("PART_PitchGrid");
            Grid_Compass = this.Find<Grid>("PART_CompassGrid");
            Canvas_Background = this.Find<Canvas>("PART_Background");
            Canvas_PitchIndicator = this.Find<Canvas>("PART_PitchIndicator");
            Canvas_HUD = this.Find<Canvas>("PART_HUD");
            Canvas_Compass = this.Find<Canvas>("PART_Compass");

            Console.WriteLine($"[AH] InitializeComponent: Grid_Viewport={(Grid_Viewport != null ? "OK" : "NULL")}");
            Console.WriteLine($"[AH] InitializeComponent: Canvas_Background={(Canvas_Background != null ? "OK" : "NULL")}");

            this.Loaded += OnLoaded;
            this.SizeChanged += OnSizeChanged;
        }

        private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Console.WriteLine($"[AH] OnLoaded called");
            Redraw();
        }

        private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
        {
            // Redraw when size changes
            Redraw();
        }

        #region Properties

        public static readonly StyledProperty<double> RollAngleProperty =
            AvaloniaProperty.Register<ArtificialHorizonView, double>(nameof(RollAngle), 0);

        public double RollAngle
        {
            get => GetValue(RollAngleProperty);
            set => SetValue(RollAngleProperty, value);
        }

        public static readonly StyledProperty<double> PitchAngleProperty =
            AvaloniaProperty.Register<ArtificialHorizonView, double>(nameof(PitchAngle), 0);

        public double PitchAngle
        {
            get => GetValue(PitchAngleProperty);
            set => SetValue(PitchAngleProperty, value);
        }

        public static readonly StyledProperty<double> YawAngleProperty =
            AvaloniaProperty.Register<ArtificialHorizonView, double>(nameof(YawAngle), 0);

        public double YawAngle
        {
            get => GetValue(YawAngleProperty);
            set => SetValue(YawAngleProperty, value);
        }

        static ArtificialHorizonView()
        {
            RollAngleProperty.Changed.AddClassHandler<ArtificialHorizonView>((x, e) => x.Redraw());
            PitchAngleProperty.Changed.AddClassHandler<ArtificialHorizonView>((x, e) => x.Redraw());
            YawAngleProperty.Changed.AddClassHandler<ArtificialHorizonView>((x, e) => x.Redraw());
        }

        #endregion

        private void Redraw()
        {
            if (Grid_Viewport == null || Canvas_Background == null)
            {
                Console.WriteLine("[AH] Redraw: Controls not ready yet");
                return;
            }

            if (Grid_Viewport.Bounds.Width == 0 || Grid_Viewport.Bounds.Height == 0)
            {
                Console.WriteLine($"[AH] Redraw: Invalid bounds W={Grid_Viewport.Bounds.Width} H={Grid_Viewport.Bounds.Height}");
                return;
            }

            Console.WriteLine($"[AH] Redraw: W={Grid_Viewport.Bounds.Width} H={Grid_Viewport.Bounds.Height} Pitch={PitchAngle:F1} Roll={RollAngle:F1}");

            // Clear all canvases
            Canvas_Background?.Children.Clear();
            Canvas_PitchIndicator?.Children.Clear();
            Canvas_HUD?.Children.Clear();
            Canvas_Compass?.Children.Clear();

            // Draw all elements
            DrawGroundAndSky(PitchAngle);
            DrawPitchTicks(PitchAngle, RollAngle);
            DrawCompass(YawAngle);
            DrawHeading(YawAngle);
            DrawRoll(RollAngle);
            DrawAircraft();

            // Apply transformations
            if (Canvas_Background != null)
            {
                Canvas_Background.RenderTransform = new RotateTransform(-RollAngle);
            }

            if (Canvas_PitchIndicator != null && Grid_Viewport != null)
            {
                var transformGroup = new TransformGroup();
                transformGroup.Children.Add(new RotateTransform(-RollAngle));
                Canvas_PitchIndicator.RenderTransform = transformGroup;
            }
        }

        #region Ground and Sky

        private void DrawGroundAndSky(double pitchDeg)
        {
            if (Grid_Viewport == null || Canvas_Background == null) return;

            double vertPixelsPerDeg = Grid_Viewport.Bounds.Height / VERTICAL_DEG_TO_DISP;
            double offset = pitchDeg * vertPixelsPerDeg;

            double maxDim = Math.Max(Grid_Viewport.Bounds.Width, Grid_Viewport.Bounds.Height);
            offset = Math.Clamp(offset, -maxDim, maxDim);

            const double OVERSIZE_RATIO = 5;
            double rectDimension = maxDim * OVERSIZE_RATIO;

            // Ground gradient (brown)
            var gndGradBrush = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
                GradientStops =
                {
                    new GradientStop(Color.FromRgb(173, 104, 13), 0.0),
                    new GradientStop(Color.FromRgb(247, 147, 17), 0.25)
                }
            };

            // Sky gradient (blue)
            var skyGradBrush = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
                GradientStops =
                {
                    new GradientStop(Color.FromRgb(3, 84, 196), 0.75),
                    new GradientStop(Color.FromRgb(2, 147, 247), 1)
                }
            };

            // Ground rectangle
            var gndRect = new Rectangle
            {
                Fill = gndGradBrush,
                Width = rectDimension,
                Height = rectDimension
            };
            Canvas.SetLeft(gndRect, -maxDim);
            Canvas.SetTop(gndRect, offset);

            // Sky rectangle
            var skyRect = new Rectangle
            {
                Fill = skyGradBrush,
                Width = rectDimension,
                Height = rectDimension
            };
            Canvas.SetLeft(skyRect, -maxDim);
            Canvas.SetBottom(skyRect, -offset);

            // Horizon line
            var line = new Line
            {
                StartPoint = new Point(-rectDimension, offset),
                EndPoint = new Point(rectDimension, offset),
                Stroke = Brushes.White,
                StrokeThickness = 2
            };

            Canvas_Background.Children.Add(gndRect);
            Canvas_Background.Children.Add(skyRect);
            Canvas_Background.Children.Add(line);
        }

        #endregion

        #region Pitch Ticks

        private void DrawMajorPitchTick(double offset, double val, bool dispTxt)
        {
            if (Canvas_PitchIndicator == null) return;

            // Left tick
            var lnL = new Line
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(40, 0),
                Stroke = Brushes.White,
                StrokeThickness = 2
            };
            Canvas.SetLeft(lnL, -80);
            Canvas.SetTop(lnL, -offset);
            Canvas_PitchIndicator.Children.Add(lnL);

            // Right tick
            var lnR = new Line
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(40, 0),
                Stroke = Brushes.White,
                StrokeThickness = 2
            };
            Canvas.SetLeft(lnR, 40);
            Canvas.SetTop(lnR, -offset);
            Canvas_PitchIndicator.Children.Add(lnR);

            // Vertical bars for non-zero pitches
            if (val != 0)
            {
                double y2 = val < 0 ? -7 : 7;

                var left = new Line
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(0, y2),
                    Stroke = Brushes.White,
                    StrokeThickness = 2
                };
                Canvas.SetLeft(left, -80);
                Canvas.SetTop(left, -offset);
                Canvas_PitchIndicator.Children.Add(left);

                var right = new Line
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(0, y2),
                    Stroke = Brushes.White,
                    StrokeThickness = 2
                };
                Canvas.SetRight(right, -80);
                Canvas.SetTop(right, -offset);
                Canvas_PitchIndicator.Children.Add(right);
            }

            // Text labels
            if (dispTxt)
            {
                var txtBlkL = CreateTextLabel(val.ToString("##0"), 16, FontWeight.Bold);
                Canvas.SetTop(txtBlkL, -offset - 13);
                Canvas.SetLeft(txtBlkL, -120);
                Canvas_PitchIndicator.Children.Add(txtBlkL);

                var txtBlkR = CreateTextLabel(val.ToString("##0"), 16, FontWeight.Bold);
                Canvas.SetTop(txtBlkR, -offset - 13);
                Canvas.SetRight(txtBlkR, -120);
                Canvas_PitchIndicator.Children.Add(txtBlkR);
            }
        }

        private void DrawMinorPitchTick(double offset, double val)
        {
            if (Canvas_PitchIndicator == null) return;

            // Left tick (shorter)
            var lnL = new Line
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(25, 0),
                Stroke = Brushes.White,
                StrokeThickness = 1
            };
            Canvas.SetLeft(lnL, -60);
            Canvas.SetTop(lnL, -offset);
            Canvas_PitchIndicator.Children.Add(lnL);

            // Right tick (shorter)
            var lnR = new Line
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(25, 0),
                Stroke = Brushes.White,
                StrokeThickness = 1
            };
            Canvas.SetLeft(lnR, 35);
            Canvas.SetTop(lnR, -offset);
            Canvas_PitchIndicator.Children.Add(lnR);

            // Vertical bars for non-zero pitches
            if (val != 0)
            {
                double y2 = val < 0 ? -5 : 5;

                var left = new Line
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(0, y2),
                    Stroke = Brushes.White,
                    StrokeThickness = 1
                };
                Canvas.SetLeft(left, -60);
                Canvas.SetTop(left, -offset);
                Canvas_PitchIndicator.Children.Add(left);

                var right = new Line
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(0, y2),
                    Stroke = Brushes.White,
                    StrokeThickness = 1
                };
                Canvas.SetRight(right, -60);
                Canvas.SetTop(right, -offset);
                Canvas_PitchIndicator.Children.Add(right);
            }
        }

        private void DrawPitchTicks(double pitchDeg, double rollAngle)
        {
            if (Grid_Viewport == null || Grid_PitchIndicator == null) return;

            double vertPixelsPerDeg = Grid_Viewport.Bounds.Height / VERTICAL_DEG_TO_DISP;
            double zeroOffset = -(pitchDeg * vertPixelsPerDeg);

            double gridOffset = ((Grid_Viewport.Bounds.Height - Grid_PitchIndicator.Bounds.Height) / 2.0) *
                Math.Cos(rollAngle * Math.PI / 180.0);

            zeroOffset += gridOffset;

            // Draw major ticks every 10° and minor ticks every 5°
            for (int i = 1; i < 10; i++)
            {
                double pitchVal = i * 10;
                double offset = (pitchVal * vertPixelsPerDeg) + zeroOffset;
                DrawMajorPitchTick(offset, pitchVal, true);

                offset -= (5 * vertPixelsPerDeg);
                DrawMinorPitchTick(offset, pitchVal - 5);

                offset = -(pitchVal * vertPixelsPerDeg) + zeroOffset;
                DrawMajorPitchTick(offset, -pitchVal, true);

                offset += (5 * vertPixelsPerDeg);
                DrawMinorPitchTick(offset, -pitchVal + 5);
            }
        }

        #endregion

        #region Compass

        private void DrawCompass(double yawDeg)
        {
            if (Grid_Compass == null || Canvas_Compass == null) return;

            double wl = Grid_Compass.Bounds.Width;
            double horzPixelsPerDeg = wl / YAW_COMPASS_DEG_TO_DISP;

            double startYaw = yawDeg - (YAW_COMPASS_DEG_TO_DISP / 2.0);
            int roundedStart = (int)Math.Ceiling(startYaw);

            double tickOffset = (roundedStart - startYaw) * horzPixelsPerDeg;

            for (int i = 0; i < YAW_COMPASS_DEG_TO_DISP; i++)
            {
                if (((i + roundedStart) % 2) == 0)
                {
                    var tl = new Line
                    {
                        StartPoint = new Point(tickOffset + (i * horzPixelsPerDeg), ((i + roundedStart) % 10) == 0 ? 21 : 25),
                        EndPoint = new Point(tickOffset + (i * horzPixelsPerDeg), 30),
                        Stroke = Brushes.White,
                        StrokeThickness = 1
                    };
                    Canvas_Compass.Children.Add(tl);

                    if (((i + roundedStart) % 10) == 0)
                    {
                        int txt = (i + roundedStart);
                        if (txt < 0) txt += 360;
                        txt /= 10;

                        var ticktext = CreateTextLabel(txt.ToString("D2"), 14, FontWeight.Normal, "Courier New");
                        Canvas.SetTop(ticktext, 2);
                        Canvas.SetLeft(ticktext, tickOffset + (i * horzPixelsPerDeg) - 10);
                        Canvas_Compass.Children.Add(ticktext);
                    }
                }
            }
        }

        private void DrawHeading(double yawDeg)
        {
            if (Grid_Compass == null || Canvas_Compass == null) return;

            yawDeg = yawDeg % 360;
            if (yawDeg < 0) yawDeg += 360;

            int yawInt = (int)yawDeg;
            if (yawInt == 360) yawInt = 0;

            double left = (Grid_Compass.Bounds.Width / 2) - 30;

            string hdgStr = "HDG ";
            if (yawInt < 100) hdgStr += " ";
            if (yawInt < 10) hdgStr += " ";

            var heading = CreateTextLabel(hdgStr + yawInt.ToString(), 12, FontWeight.Normal, "Courier New");

            var border = new Border
            {
                Child = heading,
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.White
            };
            Canvas.SetTop(border, 44);
            Canvas.SetLeft(border, left);
            Canvas_Compass.Children.Add(border);

            // Heading pointer (chevron)
            var leftLn = new Line
            {
                StartPoint = new Point((Grid_Compass.Bounds.Width / 2) - 15, 44),
                EndPoint = new Point(Grid_Compass.Bounds.Width / 2, 30),
                Stroke = Brushes.White,
                StrokeThickness = 1
            };
            Canvas_Compass.Children.Add(leftLn);

            var rightLn = new Line
            {
                StartPoint = new Point((Grid_Compass.Bounds.Width / 2) + 15, 44),
                EndPoint = new Point(Grid_Compass.Bounds.Width / 2, 30),
                Stroke = Brushes.White,
                StrokeThickness = 1
            };
            Canvas_Compass.Children.Add(rightLn);
        }

        #endregion

        #region Roll Indicator

        private void DrawRollTick(double circleRad, double rollAngle, bool isLarge)
        {
            if (Canvas_HUD == null) return;

            var line = new Line
            {
                StartPoint = new Point(0, isLarge ? -24 : -12),
                EndPoint = new Point(0, 0),
                Stroke = Brushes.White,
                StrokeThickness = 2,
                RenderTransform = new RotateTransform(rollAngle, 0, circleRad)
            };
            Canvas.SetTop(line, -circleRad);
            Canvas_HUD.Children.Add(line);
        }

        private void DrawZeroRollTick(double circleRad)
        {
            if (Canvas_HUD == null) return;

            var triangle = CreateTriangle(0, 0, 12, -16, -12, -16);
            Canvas.SetTop(triangle, -circleRad);
            Canvas_HUD.Children.Add(triangle);
        }

        private void DrawRollIndicator(double circleRad, double rollAngle)
        {
            if (Canvas_HUD == null) return;

            var triangle = CreateTriangle(0, 0, 9, 12, -9, 12);
            triangle.RenderTransform = new RotateTransform(rollAngle, 0, circleRad);

            var trapezoid = new Polygon
            {
                Stroke = Brushes.White,
                Fill = Brushes.White,
                StrokeThickness = 1,
                Points = new Points
                {
                    new Point(-12, 16),
                    new Point(12, 16),
                    new Point(15, 20),
                    new Point(-15, 20)
                },
                RenderTransform = new RotateTransform(rollAngle, 0, circleRad)
            };

            Canvas.SetTop(triangle, -circleRad);
            Canvas.SetTop(trapezoid, -circleRad);
            Canvas_HUD.Children.Add(triangle);
            Canvas_HUD.Children.Add(trapezoid);
        }

        private void DrawRoll(double rollAngle)
        {
            if (Grid_Viewport == null) return;

            double circleRad = Grid_Viewport.Bounds.Height / 3;

            var tickList = new List<KeyValuePair<double, bool>>
            {
                new(10, false),
                new(20, false),
                new(30, true),
                new(45, false),
                new(60, true)
            };

            DrawZeroRollTick(circleRad);
            foreach (var tick in tickList)
            {
                DrawRollTick(circleRad, tick.Key, tick.Value);
                DrawRollTick(circleRad, -tick.Key, tick.Value);
            }

            DrawRollIndicator(circleRad, rollAngle);
        }

        #endregion

        #region Aircraft Symbol

        private void DrawAircraft()
        {
            if (Canvas_HUD == null) return;

            double segmentLength = 6;
            var waterline = new Polyline
            {
                Stroke = Brushes.White,
                StrokeThickness = 2,
                Points = new Points
                {
                    new Point(-4 * segmentLength, 0),
                    new Point(-2 * segmentLength, 0),
                    new Point(-segmentLength, segmentLength),
                    new Point(0, 0),
                    new Point(segmentLength, segmentLength),
                    new Point(2 * segmentLength, 0),
                    new Point(4 * segmentLength, 0)
                }
            };
            Canvas_HUD.Children.Add(waterline);
        }

        #endregion

        #region Helper Methods

        private Polygon CreateTriangle(double x1, double y1, double x2, double y2, double x3, double y3)
        {
            return new Polygon
            {
                Stroke = Brushes.White,
                Fill = Brushes.White,
                StrokeThickness = 1,
                Points = new Points
                {
                    new Point(x1, y1),
                    new Point(x2, y2),
                    new Point(x3, y3)
                }
            };
        }

        private TextBlock CreateTextLabel(string text, double fontSize, FontWeight fontWeight, string? fontFamily = null)
        {
            var textBlock = new TextBlock
            {
                Text = text,
                Foreground = Brushes.White,
                FontSize = fontSize,
                FontWeight = fontWeight
            };

            if (fontFamily != null)
            {
                textBlock.FontFamily = new FontFamily(fontFamily);
            }

            return textBlock;
        }

        #endregion
    }
}
