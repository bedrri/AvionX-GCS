using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Markup.Xaml;
using AvionX.Models;

namespace AvionX.Views.Controls
{
    /// <summary>
    /// Simple Map View - Lightweight GPS Tracking
    ///
    /// DESIGN PATTERN: Custom Control with Canvas Rendering
    /// - No external map dependencies (pure Canvas)
    /// - GPS coordinate → pixel conversion
    /// - Real-time position tracking
    ///
    /// AVIATION GCS FEATURES:
    /// - Lat/Lon grid overlay
    /// - Drone position marker (red triangle)
    /// - Home location marker (yellow circle)
    /// - Flight path polyline (blue)
    /// - Distance to home calculation
    /// </summary>
    public partial class SimpleMapView : UserControl
    {
        // UI References
        private Canvas? _mapCanvas;
        private Canvas? _terrainCanvas;
        private TextBlock? _latitudeText;
        private TextBlock? _longitudeText;
        private TextBlock? _distanceText;

        // Map state
        private readonly List<GpsCoordinate> _flightPath = new();
        private GpsCoordinate? _homeLocation;
        private const int MaxPathPoints = 300;

        // Map projection parameters (meters per degree at equator)
        private const double MetersPerDegreeLat = 111320; // ~111km
        private double _metersPerDegreeLon = 111320; // Varies with latitude
        private const double MapScale = 5.0; // pixels per meter

        /// <summary>
        /// Drone Latitude Property
        /// </summary>
        public static readonly StyledProperty<double> DroneLatitudeProperty =
            AvaloniaProperty.Register<SimpleMapView, double>(nameof(DroneLatitude), defaultValue: 41.0082);

        public double DroneLatitude
        {
            get => GetValue(DroneLatitudeProperty);
            set => SetValue(DroneLatitudeProperty, value);
        }

        /// <summary>
        /// Drone Longitude Property
        /// </summary>
        public static readonly StyledProperty<double> DroneLongitudeProperty =
            AvaloniaProperty.Register<SimpleMapView, double>(nameof(DroneLongitude), defaultValue: 28.9784);

        public double DroneLongitude
        {
            get => GetValue(DroneLongitudeProperty);
            set => SetValue(DroneLongitudeProperty, value);
        }

        /// <summary>
        /// Drone Altitude Property
        /// </summary>
        public static readonly StyledProperty<double> DroneAltitudeProperty =
            AvaloniaProperty.Register<SimpleMapView, double>(nameof(DroneAltitude), defaultValue: 50.0);

        public double DroneAltitude
        {
            get => GetValue(DroneAltitudeProperty);
            set => SetValue(DroneAltitudeProperty, value);
        }

        /// <summary>
        /// Drone Heading Property (degrees, 0=North, 90=East)
        /// </summary>
        public static readonly StyledProperty<double> DroneHeadingProperty =
            AvaloniaProperty.Register<SimpleMapView, double>(nameof(DroneHeading), defaultValue: 0.0);

        public double DroneHeading
        {
            get => GetValue(DroneHeadingProperty);
            set => SetValue(DroneHeadingProperty, value);
        }

        public SimpleMapView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            // Get UI references
            _mapCanvas = this.Find<Canvas>("PART_MapCanvas");
            _terrainCanvas = this.Find<Canvas>("PART_TerrainCanvas");
            _latitudeText = this.Find<TextBlock>("PART_LatitudeText");
            _longitudeText = this.Find<TextBlock>("PART_LongitudeText");
            _distanceText = this.Find<TextBlock>("PART_DistanceText");

            Console.WriteLine($"[SimpleMap] InitializeComponent: Canvas={(_mapCanvas != null ? "OK" : "NULL")}, Terrain={(_terrainCanvas != null ? "OK" : "NULL")}");

            this.Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Console.WriteLine($"[SimpleMap] OnLoaded called - Lat={DroneLatitude}, Lon={DroneLongitude}");

            // Don't set home location here - wait for first valid GPS data in OnPositionChanged
            // This prevents setting home to (0,0) on startup

            Redraw();
        }

        /// <summary>
        /// Property changed handlers
        /// </summary>
        static SimpleMapView()
        {
            DroneLatitudeProperty.Changed.AddClassHandler<SimpleMapView>((x, e) => x.OnPositionChanged());
            DroneLongitudeProperty.Changed.AddClassHandler<SimpleMapView>((x, e) => x.OnPositionChanged());
            DroneHeadingProperty.Changed.AddClassHandler<SimpleMapView>((x, e) => x.Redraw());
        }

        private void OnPositionChanged()
        {
            // Set home location on first valid position (when connection starts)
            if (_homeLocation == null && DroneLatitude != 0 && DroneLongitude != 0)
            {
                _homeLocation = new GpsCoordinate(DroneLatitude, DroneLongitude);
                _metersPerDegreeLon = MetersPerDegreeLat * Math.Cos(DroneLatitude * Math.PI / 180);
                Console.WriteLine($"[SimpleMap] Home location set: {_homeLocation}");
            }

            // Add current position to flight path
            var currentPos = new GpsCoordinate(DroneLatitude, DroneLongitude, DroneAltitude);
            _flightPath.Add(currentPos);

            // Limit path history
            if (_flightPath.Count > MaxPathPoints)
            {
                _flightPath.RemoveAt(0);
            }

            // Update display
            UpdateCoordinateDisplay();
            Redraw();
        }

        /// <summary>
        /// Redraw entire map
        /// </summary>
        private void Redraw()
        {
            if (_mapCanvas == null || _mapCanvas.Bounds.Width == 0) return;

            try
            {
                _mapCanvas.Children.Clear();

                double canvasWidth = _mapCanvas.Bounds.Width;
                double canvasHeight = _mapCanvas.Bounds.Height;

                // Draw terrain if not already drawn
                if (_terrainCanvas != null && _terrainCanvas.Children.Count == 0)
                {
                    DrawTerrainBackground();
                }

                // Update terrain position (move terrain as drone moves)
                UpdateTerrainPosition(canvasWidth, canvasHeight);

                // Draw grid
                DrawGrid(canvasWidth, canvasHeight);

                // Draw home marker
                if (_homeLocation != null)
                {
                    DrawHomeMarker(canvasWidth, canvasHeight);
                }

                // Draw flight path
                if (_flightPath.Count > 1)
                {
                    DrawFlightPath(canvasWidth, canvasHeight);
                }

                // Draw drone marker
                DrawDroneMarker(canvasWidth, canvasHeight);

                Console.WriteLine($"[SimpleMap] Redrawn: {_flightPath.Count} path points");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SimpleMap] Redraw error: {ex.Message}");
            }
        }

        /// <summary>
        /// Update terrain canvas position to follow drone movement
        /// Creates parallax/scrolling map effect
        /// </summary>
        private void UpdateTerrainPosition(double canvasWidth, double canvasHeight)
        {
            if (_terrainCanvas == null || _homeLocation == null) return;

            // Calculate drone offset from home in meters
            double latDiff = DroneLatitude - _homeLocation.Latitude;
            double lonDiff = DroneLongitude - _homeLocation.Longitude;

            double metersNorth = latDiff * MetersPerDegreeLat;
            double metersEast = lonDiff * _metersPerDegreeLon;

            // Convert to pixels
            double offsetX = metersEast * MapScale;
            double offsetY = -metersNorth * MapScale; // Y inverted

            // Center terrain initially (since terrain is 5x larger, offset by 2x to center)
            double centerOffsetX = canvasWidth * 2;  // (5-1)/2 = 2
            double centerOffsetY = canvasHeight * 2;

            double finalX = -offsetX - centerOffsetX;
            double finalY = -offsetY - centerOffsetY;

            // Apply translation (terrain moves opposite to drone)
            _terrainCanvas.RenderTransform = new TranslateTransform(finalX, finalY);

            if (_flightPath.Count % 20 == 0) // Log every 20 frames
            {
                Console.WriteLine($"[SimpleMap] Terrain pos: offset=({offsetX:F1}, {offsetY:F1}) final=({finalX:F1}, {finalY:F1}) children={_terrainCanvas.Children.Count}");
            }
        }

        /// <summary>
        /// Convert GPS coordinates to canvas pixel coordinates
        /// Camera follows drone - calculates position relative to drone (which stays at center)
        /// </summary>
        private (double x, double y) GpsToPixel(double lat, double lon, double canvasWidth, double canvasHeight)
        {
            if (_homeLocation == null)
            {
                return (canvasWidth / 2, canvasHeight / 2);
            }

            // Calculate offset from DRONE (not home) in meters
            // This makes everything positioned relative to the drone
            double latDiff = lat - DroneLatitude;
            double lonDiff = lon - DroneLongitude;

            double metersNorth = latDiff * MetersPerDegreeLat;
            double metersEast = lonDiff * _metersPerDegreeLon;

            // Convert meters to pixels (with scaling)
            double pixelX = canvasWidth / 2 + (metersEast * MapScale);
            double pixelY = canvasHeight / 2 - (metersNorth * MapScale); // Y axis inverted

            return (pixelX, pixelY);
        }

        /// <summary>
        /// Draw GPS grid overlay
        /// </summary>
        private void DrawGrid(double canvasWidth, double canvasHeight)
        {
            // Grid line color
            var gridBrush = new SolidColorBrush(Color.FromRgb(40, 40, 40));

            // Vertical center line (longitude)
            var centerLineV = new Line
            {
                StartPoint = new Point(canvasWidth / 2, 0),
                EndPoint = new Point(canvasWidth / 2, canvasHeight),
                Stroke = gridBrush,
                StrokeThickness = 1,
                StrokeDashArray = new AvaloniaList<double> { 5, 5 }
            };
            _mapCanvas?.Children.Add(centerLineV);

            // Horizontal center line (latitude)
            var centerLineH = new Line
            {
                StartPoint = new Point(0, canvasHeight / 2),
                EndPoint = new Point(canvasWidth, canvasHeight / 2),
                Stroke = gridBrush,
                StrokeThickness = 1,
                StrokeDashArray = new AvaloniaList<double> { 5, 5 }
            };
            _mapCanvas?.Children.Add(centerLineH);

            // Distance circles (50m, 100m, 150m)
            DrawDistanceCircle(canvasWidth, canvasHeight, 50, gridBrush);
            DrawDistanceCircle(canvasWidth, canvasHeight, 100, gridBrush);
            DrawDistanceCircle(canvasWidth, canvasHeight, 150, gridBrush);
        }

        /// <summary>
        /// Draw distance circle from home
        /// </summary>
        private void DrawDistanceCircle(double canvasWidth, double canvasHeight, double radiusMeters, IBrush brush)
        {
            double radiusPixels = radiusMeters * MapScale;

            var circle = new Ellipse
            {
                Width = radiusPixels * 2,
                Height = radiusPixels * 2,
                Stroke = brush,
                StrokeThickness = 1,
                StrokeDashArray = new AvaloniaList<double> { 3, 3 }
            };

            Canvas.SetLeft(circle, canvasWidth / 2 - radiusPixels);
            Canvas.SetTop(circle, canvasHeight / 2 - radiusPixels);

            _mapCanvas?.Children.Add(circle);

            // Label
            var label = new TextBlock
            {
                Text = $"{radiusMeters}m",
                Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                FontSize = 9
            };
            Canvas.SetLeft(label, canvasWidth / 2 + 5);
            Canvas.SetTop(label, canvasHeight / 2 - radiusPixels);
            _mapCanvas?.Children.Add(label);
        }

        /// <summary>
        /// Draw home location marker (yellow circle)
        /// </summary>
        private void DrawHomeMarker(double canvasWidth, double canvasHeight)
        {
            if (_homeLocation == null) return;

            var (x, y) = GpsToPixel(_homeLocation.Latitude, _homeLocation.Longitude, canvasWidth, canvasHeight);

            // Yellow circle
            var homeMarker = new Ellipse
            {
                Width = 12,
                Height = 12,
                Fill = new SolidColorBrush(Color.FromRgb(255, 215, 0)), // Gold
                Stroke = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                StrokeThickness = 2
            };

            Canvas.SetLeft(homeMarker, x - 6);
            Canvas.SetTop(homeMarker, y - 6);
            _mapCanvas?.Children.Add(homeMarker);

            // "H" label
            var label = new TextBlock
            {
                Text = "H",
                Foreground = Brushes.White,
                FontSize = 10,
                FontWeight = FontWeight.Bold
            };
            Canvas.SetLeft(label, x - 4);
            Canvas.SetTop(label, y - 7);
            _mapCanvas?.Children.Add(label);
        }

        /// <summary>
        /// Draw flight path polyline (blue)
        /// </summary>
        private void DrawFlightPath(double canvasWidth, double canvasHeight)
        {
            var pathGeometry = new StreamGeometry();

            using (var context = pathGeometry.Open())
            {
                bool firstPoint = true;

                foreach (var point in _flightPath)
                {
                    var (x, y) = GpsToPixel(point.Latitude, point.Longitude, canvasWidth, canvasHeight);

                    if (firstPoint)
                    {
                        context.BeginFigure(new Point(x, y), false);
                        firstPoint = false;
                    }
                    else
                    {
                        context.LineTo(new Point(x, y));
                    }
                }
            }

            var path = new Path
            {
                Data = pathGeometry,
                Stroke = new SolidColorBrush(Color.FromArgb(180, 0, 255, 0)), // Bright lime green with transparency
                StrokeThickness = 2
            };

            _mapCanvas?.Children.Add(path);
        }

        /// <summary>
        /// Draw drone position marker (red triangle)
        /// Drone is ALWAYS at canvas center - camera follows drone
        /// Triangle rotates based on actual movement direction (calculated from GPS path)
        /// </summary>
        private void DrawDroneMarker(double canvasWidth, double canvasHeight)
        {
            // Drone stays at center - we're following it!
            double x = canvasWidth / 2;
            double y = canvasHeight / 2;

            // Calculate ACTUAL movement direction from last two GPS points
            double movementHeading = DroneHeading; // Default to telemetry heading
            if (_flightPath.Count >= 2)
            {
                var previous = _flightPath[_flightPath.Count - 2];
                var current = _flightPath[_flightPath.Count - 1];

                // Calculate bearing from previous to current position
                movementHeading = previous.BearingTo(current);
            }

            // Red triangle pointing up (aircraft)
            var triangle = new Polygon
            {
                Points = new Points
                {
                    new Point(0, -10),   // Top (nose)
                    new Point(-7, 10),   // Bottom left (tail left)
                    new Point(7, 10)     // Bottom right (tail right)
                },
                Fill = new SolidColorBrush(Color.FromRgb(255, 50, 50)), // Red
                Stroke = Brushes.White,
                StrokeThickness = 2,
                // Rotate based on MOVEMENT heading (not telemetry heading)
                RenderTransform = new RotateTransform(movementHeading),
                RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative) // Pivot at center
            };

            Canvas.SetLeft(triangle, x);
            Canvas.SetTop(triangle, y);
            _mapCanvas?.Children.Add(triangle);

            // Debug log every 20 frames
            if (_flightPath.Count % 20 == 0)
            {
                Console.WriteLine($"[SimpleMap] Telemetry heading: {DroneHeading:F1}°, Movement heading: {movementHeading:F1}°");
            }
        }

        /// <summary>
        /// Update coordinate text display
        /// </summary>
        private void UpdateCoordinateDisplay()
        {
            if (_latitudeText != null)
                _latitudeText.Text = $"{DroneLatitude:F6}°";

            if (_longitudeText != null)
                _longitudeText.Text = $"{DroneLongitude:F6}°";

            // Calculate distance to home
            if (_homeLocation != null && _distanceText != null)
            {
                var currentPos = new GpsCoordinate(DroneLatitude, DroneLongitude);
                double distance = _homeLocation.DistanceTo(currentPos);
                _distanceText.Text = $"{distance:F0}m";
            }
        }

        /// <summary>
        /// Clear flight path
        /// </summary>
        public void ClearFlightPath()
        {
            _flightPath.Clear();
            Redraw();
        }

        /// <summary>
        /// Draw terrain background (procedural map-like pattern)
        /// Creates a realistic terrain appearance with roads, water, and buildings
        /// </summary>
        private void DrawTerrainBackground()
        {
            if (_terrainCanvas == null || _terrainCanvas.Bounds.Width == 0) return;

            try
            {
                // Make terrain MUCH larger than canvas to allow scrolling
                double width = _terrainCanvas.Bounds.Width * 5;  // 5x larger
                double height = _terrainCanvas.Bounds.Height * 5;

                // Try to load satellite image (try both .jpg and .png)
                try
                {
                    Bitmap? bitmap = null;

                    // Try different file extensions
                    string[] extensions = { ".jpg", ".jpeg", ".png" };
                    foreach (var ext in extensions)
                    {
                        try
                        {
                            var uri = new Uri($"avares://AvionX/Assets/satellite-map{ext}");
                            var assets = AssetLoader.Open(uri);
                            bitmap = new Bitmap(assets);
                            Console.WriteLine($"[SimpleMap] Satellite map loaded: satellite-map{ext}");
                            break;
                        }
                        catch
                        {
                            // Try next extension
                        }
                    }

                    if (bitmap != null)
                    {
                        var satelliteImage = new Image
                        {
                            Source = bitmap,
                            Width = width,
                            Height = height,
                            Stretch = Stretch.UniformToFill
                        };

                        _terrainCanvas.Children.Add(satelliteImage);
                        Console.WriteLine("[SimpleMap] Satellite map displayed successfully");
                        return; // Exit if satellite loaded successfully
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SimpleMap] Could not load satellite map: {ex.Message}");
                    Console.WriteLine("[SimpleMap] Falling back to generated terrain");
                }

                // Fallback: Base terrain color (light greenish-gray for land)
                var terrain = new Rectangle
                {
                    Width = width,
                    Height = height,
                    Fill = new SolidColorBrush(Color.FromRgb(220, 225, 215)) // Light terrain
                };
                _terrainCanvas.Children.Add(terrain);

                // Water bodies (darker blue-gray patches)
                Random rand = new Random(42); // Fixed seed for consistent terrain
                for (int i = 0; i < 3; i++)
                {
                    var water = new Ellipse
                    {
                        Width = 80 + rand.Next(40),
                        Height = 50 + rand.Next(30),
                        Fill = new SolidColorBrush(Color.FromRgb(180, 200, 220)), // Water blue
                        Opacity = 0.6
                    };
                    Canvas.SetLeft(water, rand.Next((int)width - 100));
                    Canvas.SetTop(water, rand.Next((int)height - 80));
                    _terrainCanvas.Children.Add(water);
                }

                // Roads (darker gray lines)
                var roadBrush = new SolidColorBrush(Color.FromRgb(180, 180, 180));

                // Main road (horizontal)
                var road1 = new Rectangle
                {
                    Width = width,
                    Height = 8,
                    Fill = roadBrush,
                    Opacity = 0.7
                };
                Canvas.SetTop(road1, height * 0.6);
                _terrainCanvas.Children.Add(road1);

                // Secondary road (vertical)
                var road2 = new Rectangle
                {
                    Width = 6,
                    Height = height,
                    Fill = roadBrush,
                    Opacity = 0.7
                };
                Canvas.SetLeft(road2, width * 0.4);
                _terrainCanvas.Children.Add(road2);

                // Diagonal road
                var road3 = new Line
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(width, height * 0.7),
                    Stroke = roadBrush,
                    StrokeThickness = 6,
                    Opacity = 0.6
                };
                _terrainCanvas.Children.Add(road3);

                // Buildings (small rectangles)
                var buildingBrush = new SolidColorBrush(Color.FromRgb(160, 160, 170));
                for (int i = 0; i < 12; i++)
                {
                    var building = new Rectangle
                    {
                        Width = 8 + rand.Next(15),
                        Height = 8 + rand.Next(15),
                        Fill = buildingBrush,
                        Opacity = 0.5,
                        RadiusX = 1,
                        RadiusY = 1
                    };
                    Canvas.SetLeft(building, rand.Next((int)width - 20));
                    Canvas.SetTop(building, rand.Next((int)height - 20));
                    _terrainCanvas.Children.Add(building);
                }

                // Parks/Green areas (light green patches)
                for (int i = 0; i < 5; i++)
                {
                    var park = new Ellipse
                    {
                        Width = 30 + rand.Next(40),
                        Height = 30 + rand.Next(40),
                        Fill = new SolidColorBrush(Color.FromRgb(200, 220, 190)), // Light green
                        Opacity = 0.5
                    };
                    Canvas.SetLeft(park, rand.Next((int)width - 60));
                    Canvas.SetTop(park, rand.Next((int)height - 60));
                    _terrainCanvas.Children.Add(park);
                }

                // Grid overlay (subtle)
                var gridBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200));
                for (int i = 0; i < width; i += 50)
                {
                    var vertLine = new Line
                    {
                        StartPoint = new Point(i, 0),
                        EndPoint = new Point(i, height),
                        Stroke = gridBrush,
                        StrokeThickness = 0.5,
                        Opacity = 0.3
                    };
                    _terrainCanvas.Children.Add(vertLine);
                }

                for (int i = 0; i < height; i += 50)
                {
                    var horizLine = new Line
                    {
                        StartPoint = new Point(0, i),
                        EndPoint = new Point(width, i),
                        Stroke = gridBrush,
                        StrokeThickness = 0.5,
                        Opacity = 0.3
                    };
                    _terrainCanvas.Children.Add(horizLine);
                }

                Console.WriteLine($"[SimpleMap] Terrain background drawn: {width}x{height}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SimpleMap] Terrain drawing error: {ex.Message}");
            }
        }
    }
}
