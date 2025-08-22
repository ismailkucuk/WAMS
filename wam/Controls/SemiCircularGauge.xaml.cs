using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace wam.Controls
{
	public partial class SemiCircularGauge : UserControl
	{
		public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
			name: nameof(Title), propertyType: typeof(string), ownerType: typeof(SemiCircularGauge), typeMetadata: new PropertyMetadata("Utilization", OnVisualPropertyChanged));

		public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
			name: nameof(Value), propertyType: typeof(double), ownerType: typeof(SemiCircularGauge), typeMetadata: new PropertyMetadata(0d, OnVisualPropertyChanged));

		public static readonly DependencyProperty PeakValueProperty = DependencyProperty.Register(
			name: nameof(PeakValue), propertyType: typeof(double), ownerType: typeof(SemiCircularGauge), typeMetadata: new PropertyMetadata(0d, OnVisualPropertyChanged));

		public static readonly DependencyProperty ForegroundBrushProperty = DependencyProperty.Register(
			name: nameof(ForegroundBrush), propertyType: typeof(Brush), ownerType: typeof(SemiCircularGauge), typeMetadata: new PropertyMetadata(new SolidColorBrush(Color.FromRgb(74, 144, 226)), OnVisualPropertyChanged));

		public string Title
		{
			get => (string)GetValue(TitleProperty);
			set => SetValue(TitleProperty, value);
		}

		public double Value
		{
			get => (double)GetValue(ValueProperty);
			set => SetValue(ValueProperty, value);
		}

		public Brush ForegroundBrush
		{
			get => (Brush)GetValue(ForegroundBrushProperty);
			set => SetValue(ForegroundBrushProperty, value);
		}

		// Peak (max seen) value within 0-100
		public double PeakValue
		{
			get => (double)GetValue(PeakValueProperty);
			set => SetValue(PeakValueProperty, value);
		}

		public SemiCircularGauge()
		{
			InitializeComponent();
			Loaded += (s, e) => Redraw();
			SizeChanged += (s, e) => Redraw();
		}

		private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var gauge = (SemiCircularGauge)d;
			gauge.Redraw();
		}

		private void Redraw()
		{
			try
			{
				TitleText.Text = Title;
				ValueText.Text = $"{Math.Max(0, Math.Min(100, Value)):0.#} %";

				double w = GaugeHost.ActualWidth > 0 ? GaugeHost.ActualWidth : ActualWidth;
				double h = GaugeHost.ActualHeight > 0 ? GaugeHost.ActualHeight : ActualHeight;
				if (w <= 0 || h <= 0)
					return;

				double padding = 12;
				double radius = Math.Min(w, h * 2) / 2 - padding;
				Point center = new Point(w / 2, radius + padding);

				// Track arc (semi-circle 180°)
				TrackPath.Data = CreateArcGeometry(center, radius, -180, 0);
				// Use the same color as defined in the XAML theme resources

				// Progress arc from -180° to angle based on Value (end at needle)
				double clamped = Math.Max(0, Math.Min(100, Value));
				double angle = -180 + 180 * clamped / 100.0;
				ProgressPath.Data = CreateArcGeometry(center, radius, -180, angle);
				ProgressPath.Stroke = ForegroundBrush;

				// Needle color by thresholds
				Brush needleBrush = ForegroundBrush;
				var v = Math.Max(0, Math.Min(100, Value));
				if (v < 30) needleBrush = new SolidColorBrush(Color.FromRgb(16, 124, 16)); // green
				else if (v < 70) needleBrush = new SolidColorBrush(Color.FromRgb(255, 140, 0)); // orange
				else needleBrush = new SolidColorBrush(Color.FromRgb(209, 52, 56)); // red

				// Needle
				double needleAngleRad = angle * Math.PI / 180.0;
				double nx = center.X + Math.Cos(needleAngleRad) * (radius - 6);
				double ny = center.Y + Math.Sin(needleAngleRad) * (radius - 6);
				Needle.X1 = center.X; Needle.Y1 = center.Y;
				Needle.X2 = nx; Needle.Y2 = ny;
				Needle.Stroke = needleBrush;

				// Top notch (peak marker at max reached)
				double peakClamped = Math.Max(0, Math.Min(100, PeakValue));
				double peakAngle = -180 + 180 * peakClamped / 100.0;
				double topRad = peakAngle * Math.PI / 180.0;
				double tx1 = center.X + Math.Cos(topRad) * (radius - 4);
				double ty1 = center.Y + Math.Sin(topRad) * (radius - 4);
				double tx2 = center.X + Math.Cos(topRad) * (radius + 4);
				double ty2 = center.Y + Math.Sin(topRad) * (radius + 4);
				TopNotch.X1 = tx1; TopNotch.Y1 = ty1; TopNotch.X2 = tx2; TopNotch.Y2 = ty2;
			}
			catch { }
		}

		private static PathGeometry CreateArcGeometry(Point center, double radius, double startAngleDeg, double endAngleDeg)
		{
			bool largeArc = Math.Abs(endAngleDeg - startAngleDeg) > 180;
			double startRad = startAngleDeg * Math.PI / 180.0;
			double endRad = endAngleDeg * Math.PI / 180.0;
			Point start = new Point(center.X + Math.Cos(startRad) * radius, center.Y + Math.Sin(startRad) * radius);
			Point end = new Point(center.X + Math.Cos(endRad) * radius, center.Y + Math.Sin(endRad) * radius);

			var figure = new PathFigure { StartPoint = start, IsClosed = false, IsFilled = false };
			var seg = new ArcSegment
			{
				Point = end,
				Size = new Size(radius, radius),
				IsLargeArc = largeArc,
				SweepDirection = SweepDirection.Clockwise
			};
			figure.Segments.Add(seg);
			var geo = new PathGeometry();
			geo.Figures.Add(figure);
			return geo;
		}
	}
}

