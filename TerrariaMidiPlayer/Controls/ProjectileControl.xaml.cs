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
	/**<summary>A control for changing projectile direction.</summary>*/
	public partial class ProjectileControl : UserControl {
		//========== CONSTANTS ===========
		#region Constants

		/**<summary>The radius of the circle.</summary>*/
		const double Radius = 40;

		#endregion
		//=========== MEMBERS ============
		#region Members

		/**<summary>True if the control has loaded.</summary>*/
		bool loaded = false;
		/**<summary>True if the angle is being changed.</summary>*/
		bool rotating = false;
		/**<summary>The angle of projectiles.</summary>*/
		double angle = 0;
		/**<summary>The range of projectiles.</summary>*/
		double range = 360;

		#endregion
		//========= CONSTRUCTORS =========
		#region Constructors

		/**<summary>Constructs the projectile control.</summary>*/
		public ProjectileControl() {
			InitializeComponent();

			RenderArc();
		}

		#endregion
		//============ EVENTS ============
		#region Events

		/**<summary>The projectiles changed routed event.</summary>*/
		public static readonly RoutedEvent ProjectilesChangedEvent = EventManager.RegisterRoutedEvent("ProjectilesChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ProjectileControl));
		/**<summary>Called when the projectiles has been changed.</summary>*/
		public event RoutedEventHandler ProjectilesChanged {
			add { AddHandler(ProjectilesChangedEvent, value); }
			remove { RemoveHandler(ProjectilesChangedEvent, value); }
		}

		#endregion
		//========== PROPERTIES ==========
		#region Properties

		/**<summary>The angle of projectiles.</summary>*/
		public int Angle {
			get { return numericAngle.Value; }
			set {
				angle = value;
				numericAngle.Value = value;
				RenderArc();
			}
		}
		/**<summary>The range of projectiles.</summary>*/
		public int Range {
			get { return numericRange.Value; }
			set {
				range = value;
				numericRange.Value = value;
				RenderArc();
			}
		}

		#endregion
		//============ EVENTS ============
		#region Events
			
		private void OnControlLoaded(object sender, RoutedEventArgs e) {
			loaded = true;
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
			RaiseEvent(new RoutedEventArgs(ProjectilesChangedEvent));
		}
		private void OnMouseWheel(object sender, MouseWheelEventArgs e) {
			if (e.Delta > 0 && range < 360) {
				range = Math.Floor(Math.Min(360, range + 5));
				numericRange.Value = (int)range;
				RenderArc();
				RaiseEvent(new RoutedEventArgs(ProjectilesChangedEvent));
			}
			else if (e.Delta < 0 && range > 0) {
				range = Math.Floor(Math.Max(0, range - 5));
				numericRange.Value = (int)range;
				RenderArc();
				RaiseEvent(new RoutedEventArgs(ProjectilesChangedEvent));
			}
		}
		private void OnAngleValueChanged(object sender, ValueChangedEventArgs<int> e) {
			if (!loaded)
				return;
			angle = e.New;
			RenderArc();
			RaiseEvent(new RoutedEventArgs(ProjectilesChangedEvent));
		}
		private void OnRangeValueChanged(object sender, ValueChangedEventArgs<int> e) {
			if (!loaded)
				return;
			range = e.New;
			RenderArc();
			RaiseEvent(new RoutedEventArgs(ProjectilesChangedEvent));
		}
		
		#endregion
		//=========== HELPERS ============
		#region Helpers

		//http://timokorinth.de/creating-circular-progress-bar-wpf-silverlight/
		/**<summary>Updates the path of the arc.</summary>*/
		private void RenderArc() {
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
		/**<summary>Changes degrees to a vector.</summary>*/
		private Point ComputeCartesianCoordinate(double angle, double radius) {
			double angleRad = (Math.PI / 180.0) * (angle - 90);

			double x = radius * Math.Cos(angleRad);
			double y = radius * Math.Sin(angleRad);

			return new Point(x, y);
		}

		#endregion
	}
}
