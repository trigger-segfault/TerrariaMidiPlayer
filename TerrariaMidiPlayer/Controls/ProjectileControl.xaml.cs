using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TerrariaMidiPlayer.Controls {
	/// <summary>
	/// Interaction logic for ProjectileControl2.xaml
	/// </summary>
	public partial class ProjectileControl : UserControl {
		const double Radius = 40;

		bool loaded;
		bool rotating;
		double angle;
		double range;

		public ProjectileControl() {
			InitializeComponent();
			loaded = false;
			rotating = false;
			angle = 0;
			range = 360;
			RenderArc();

			// TODO: Capture Mouse from outside circle, because range of 0 is hard to click on
		}

		public int Angle {
			get { return numericAngle.Value; }
			set {
				angle = value;
				numericAngle.Value = value;
				RenderArc();
			}
		}
		public int Range {
			get { return numericRange.Value; }
			set {
				range = value;
				numericRange.Value = value;
				RenderArc();
			}
		}

		public static readonly RoutedEvent ProjectilesChangedEvent = EventManager.RegisterRoutedEvent("ProjectilesChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ProjectileControl));

		public event RoutedEventHandler ProjectilesChanged;

		private void OnLoaded(object sender, RoutedEventArgs e) {
			loaded = true;
		}

		private void OnMouseWheel(object sender, MouseWheelEventArgs e) {
			if (e.Delta > 0 && range < 360) {
				range = Math.Floor(Math.Min(360, range + 5));
				numericRange.Value = (int)range;
				RenderArc();
				RaiseEvent(new RoutedEventArgs(ProjectileControl.ProjectilesChangedEvent));
			}
			else if (e.Delta < 0 && range > 0) {
				range = Math.Floor(Math.Max(0, range - 5));
				numericRange.Value = (int)range;
				RenderArc();
				RaiseEvent(new RoutedEventArgs(ProjectileControl.ProjectilesChangedEvent));
			}
		}

		private void OnMouseDown(object sender, MouseButtonEventArgs e) {
			circle.CaptureMouse();
			rotating = true;
		}

		private void OnMouseUp(object sender, MouseButtonEventArgs e) {
			circle.ReleaseMouseCapture();
			rotating = false;
		}

		private void OnMouseMove(object sender, MouseEventArgs e) {
			if (!rotating)
				return;
			Point mouse = e.GetPosition(path);
			angle = (Math.Atan2(mouse.Y - Radius + 1, mouse.X - Radius + 1) / Math.PI * 180 + 90 + 360) % 360;
			numericAngle.Value = (int)angle;
			RenderArc();
			RaiseEvent(new RoutedEventArgs(ProjectileControl.ProjectilesChangedEvent));
		}

		//http://timokorinth.de/creating-circular-progress-bar-wpf-silverlight/

		public void RenderArc() {
			Point startPoint = ComputeCartesianCoordinate(angle - range / 2, Radius);// new Point(Radius, 0);
			startPoint.X += Radius + 1;
			startPoint.Y += Radius + 1;
			Point endPoint = ComputeCartesianCoordinate(angle + range / 2 + (range == 360 ? -0.01 : 0), Radius);
			endPoint.X += Radius + 1;
			endPoint.Y += Radius + 1;
			Point originPoint = new Point(Radius + 1, Radius + 1);

			path.Width = Radius * 2 + 2;
			path.Height = Radius * 2 + 2;

			bool largeArc = range > 180.0;

			Size outerArcSize = new Size(Radius, Radius);

			figure.StartPoint = startPoint;

			arc.Point = endPoint;
			arc.Size = outerArcSize;
			arc.IsLargeArc = largeArc;

			line1.Point = originPoint;
			line2.Point = startPoint;
		}
		private Point ComputeCartesianCoordinate(double angle, double radius) {
			double angleRad = (Math.PI / 180.0) * (angle - 90);

			double x = radius * Math.Cos(angleRad);
			double y = radius * Math.Sin(angleRad);

			return new Point(x, y);
		}

		private void OnAngleValueChanged(object sender, RoutedEventArgs e) {
			if (!loaded)
				return;
			angle = numericAngle.Value;
			RenderArc();
			RaiseEvent(new RoutedEventArgs(ProjectileControl.ProjectilesChangedEvent));
		}

		private void OnRangeValueChanged(object sender, RoutedEventArgs e) {
			if (!loaded)
				return;
			range = numericRange.Value;
			RenderArc();
			RaiseEvent(new RoutedEventArgs(ProjectileControl.ProjectilesChangedEvent));
		}
	}
}
