using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using wam.Helpers;

namespace wam
{
    /// <summary>
    /// A transparent overlay window that renders endless falling snow over the entire application.
    /// Uses Win32 extended window styles to enable click-through (mouse events pass to windows below).
    /// Simplified version: No accumulation, just beautiful falling snowflakes.
    /// </summary>
    public partial class SnowOverlayWindow : Window
    {
        #region Win32 Interop for Click-Through

        /*
         * WHY WS_EX_TRANSPARENT IS NECESSARY:
         * ===================================
         *
         * In Windows, when a window receives a mouse click, the OS performs "hit testing" to determine
         * which window should receive the input. By default, even a transparent WPF window will
         * intercept mouse clicks because the window's rectangular region still exists.
         *
         * WS_EX_TRANSPARENT (0x00000020):
         * - This extended window style tells Windows to SKIP this window during hit-testing.
         * - Mouse clicks "pass through" to whatever window is underneath.
         * - Without this flag, users would click on the snow overlay and the main window
         *   wouldn't receive the input, making buttons and controls unresponsive.
         *
         * WS_EX_LAYERED (0x00080000):
         * - Required for windows with per-pixel alpha/transparency.
         * - WPF sets this automatically when AllowsTransparency="True", but we explicitly
         *   include it for clarity and to ensure compatibility.
         *
         * Together, these styles create a "ghost" window that is visible but doesn't
         * interact with mouse input - perfect for visual overlays like our snow effect.
         */

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_LAYERED = 0x00080000;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        private void MakeWindowClickThrough()
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT | WS_EX_LAYERED);
        }

        #endregion

        #region Snowflake Class

        /// <summary>
        /// Represents a single snowflake particle.
        /// Designed for object pooling - properties are mutable for reuse.
        /// </summary>
        private class Snowflake
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double Speed { get; set; }
            public double Size { get; set; }
            public double Opacity { get; set; }
            public double SwayOffset { get; set; }
            public double SwaySpeed { get; set; }
            public Ellipse? Visual { get; set; }

            /// <summary>
            /// Resets snowflake to top of screen with new random properties.
            /// OBJECT POOLING: No 'new' allocations - just modify existing properties.
            /// </summary>
            public void ResetToTop(double containerWidth, double minSpeed, double maxSpeed,
                                   double minSize, double maxSize, Random random)
            {
                X = random.NextDouble() * containerWidth;
                Y = -Size - random.NextDouble() * 50; // Start above visible area
                Speed = minSpeed + random.NextDouble() * (maxSpeed - minSpeed);
                Size = minSize + random.NextDouble() * (maxSize - minSize);
                Opacity = 0.4 + random.NextDouble() * 0.6;
                SwayOffset = random.NextDouble() * Math.PI * 2;
                SwaySpeed = 0.5 + random.NextDouble() * 1.5;

                if (Visual != null)
                {
                    Visual.Width = Size;
                    Visual.Height = Size;
                    Visual.Opacity = Opacity;
                }
            }
        }

        #endregion

        #region Fields

        private readonly List<Snowflake> _snowflakes = new();
        private readonly Random _random = new();
        private readonly DispatcherTimer _animationTimer;
        private readonly Brush _snowBrush;

        private Window? _ownerWindow;
        private double _time;
        private bool _isRunning;

        // Configuration values
        private int _snowflakeCount = 80;
        private double _minSpeed = 1.5;
        private double _maxSpeed = 5.0;
        private double _minSize = 3.0;
        private double _maxSize = 10.0;

        // Density multiplier (0.4 = 40% more than base count from config)
        private const double DENSITY_FACTOR = 0.4;

        #endregion

        #region Constructor

        public SnowOverlayWindow()
        {
            InitializeComponent();

            // Create frozen brush for performance
            _snowBrush = new SolidColorBrush(Colors.White);
            _snowBrush.Freeze();

            // Setup animation timer (~50ms = 20 FPS, smooth but CPU-friendly)
            _animationTimer = new DispatcherTimer(DispatcherPriority.Render)
            {
                Interval = TimeSpan.FromMilliseconds(50)
            };
            _animationTimer.Tick += OnAnimationTick;

            // Event handlers
            SourceInitialized += OnSourceInitialized;
            Loaded += OnLoaded;
            Closed += OnClosed;
        }

        #endregion

        #region Event Handlers

        private void OnSourceInitialized(object? sender, EventArgs e)
        {
            MakeWindowClickThrough();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            CreateSnowflakePool();
        }

        private void OnClosed(object? sender, EventArgs e)
        {
            Stop();
            UnbindFromOwner();
        }

        #endregion

        #region Owner Window Binding

        public void BindToOwner(Window owner)
        {
            _ownerWindow = owner;
            Owner = owner;

            SyncWithOwner();

            _ownerWindow.LocationChanged += OnOwnerLayoutChanged;
            _ownerWindow.SizeChanged += OnOwnerLayoutChanged;
            _ownerWindow.StateChanged += OnOwnerStateChanged;
        }

        private void UnbindFromOwner()
        {
            if (_ownerWindow != null)
            {
                _ownerWindow.LocationChanged -= OnOwnerLayoutChanged;
                _ownerWindow.SizeChanged -= OnOwnerLayoutChanged;
                _ownerWindow.StateChanged -= OnOwnerStateChanged;
                _ownerWindow = null;
            }
        }

        private void OnOwnerLayoutChanged(object? sender, EventArgs e)
        {
            SyncWithOwner();
        }

        private void OnOwnerStateChanged(object? sender, EventArgs e)
        {
            if (_ownerWindow == null) return;

            if (_ownerWindow.WindowState == WindowState.Minimized)
            {
                Hide();
            }
            else
            {
                Show();
                SyncWithOwner();
            }
        }

        private void SyncWithOwner()
        {
            if (_ownerWindow == null) return;

            Left = _ownerWindow.Left;
            Top = _ownerWindow.Top;
            Width = _ownerWindow.ActualWidth;
            Height = _ownerWindow.ActualHeight;
        }

        #endregion

        #region Snow Effect Logic

        /// <summary>
        /// Initializes the snow effect with configuration.
        /// </summary>
        public void Initialize(AppConfig? config = null)
        {
            config ??= ConfigService.LoadConfig();

            // Apply density boost
            int baseCount = config.SnowflakeCount;
            _snowflakeCount = (int)(baseCount * (1.0 + DENSITY_FACTOR));
            _snowflakeCount = Math.Clamp(_snowflakeCount, 30, 200);

            _minSpeed = config.MinSpeed;
            _maxSpeed = config.MaxSpeed;
            _minSize = config.MinSize;
            _maxSize = config.MaxSize;

            System.Diagnostics.Debug.WriteLine($"Snow initialized: {_snowflakeCount} flakes (base: {baseCount}, density factor: {DENSITY_FACTOR})");

            if (_snowflakes.Count > 0)
            {
                CreateSnowflakePool();
            }
        }

        /// <summary>
        /// Creates the pool of snowflakes (object pooling pattern).
        /// </summary>
        private void CreateSnowflakePool()
        {
            SnowCanvas.Children.Clear();
            _snowflakes.Clear();

            double containerWidth = ActualWidth > 0 ? ActualWidth : 1200;
            double containerHeight = ActualHeight > 0 ? ActualHeight : 800;

            for (int i = 0; i < _snowflakeCount; i++)
            {
                var snowflake = new Snowflake
                {
                    X = _random.NextDouble() * containerWidth,
                    Y = _random.NextDouble() * containerHeight, // Spread across screen initially
                    Speed = _minSpeed + _random.NextDouble() * (_maxSpeed - _minSpeed),
                    Size = _minSize + _random.NextDouble() * (_maxSize - _minSize),
                    Opacity = 0.4 + _random.NextDouble() * 0.6,
                    SwayOffset = _random.NextDouble() * Math.PI * 2,
                    SwaySpeed = 0.5 + _random.NextDouble() * 1.5
                };

                var ellipse = new Ellipse
                {
                    Fill = _snowBrush,
                    Width = snowflake.Size,
                    Height = snowflake.Size,
                    Opacity = snowflake.Opacity,
                    IsHitTestVisible = false
                };

                snowflake.Visual = ellipse;
                SnowCanvas.Children.Add(ellipse);
                _snowflakes.Add(snowflake);
            }
        }

        public void Start()
        {
            if (_isRunning) return;

            _isRunning = true;
            _time = 0;
            _animationTimer.Start();
        }

        public void Stop()
        {
            _isRunning = false;
            _animationTimer.Stop();
        }

        /// <summary>
        /// Animation tick - simple falling snow logic.
        /// Each snowflake falls down, and when it goes below screen, resets to top.
        /// </summary>
        private void OnAnimationTick(object? sender, EventArgs e)
        {
            if (!_isRunning || ActualHeight <= 0) return;

            _time += 0.05;

            foreach (var snowflake in _snowflakes)
            {
                // Move down
                snowflake.Y += snowflake.Speed;

                // Horizontal sway using sine wave
                double sway = Math.Sin(_time * snowflake.SwaySpeed + snowflake.SwayOffset) * 2;
                double displayX = snowflake.X + sway;

                // Simple reset: if below screen, teleport to top
                if (snowflake.Y > ActualHeight)
                {
                    snowflake.ResetToTop(ActualWidth, _minSpeed, _maxSpeed, _minSize, _maxSize, _random);
                }

                // Update visual position
                if (snowflake.Visual != null)
                {
                    Canvas.SetLeft(snowflake.Visual, displayX);
                    Canvas.SetTop(snowflake.Visual, snowflake.Y);
                }
            }
        }

        #endregion
    }
}
