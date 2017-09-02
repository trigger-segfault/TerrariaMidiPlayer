using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;

namespace TerrariaMidiPlayer.Util {
	/**<summary>Simulates changes to the mouse.</summary>*/
	public class MouseControl {

		/**<summary>Moves the mouse and clicks for a duration.</summary>*/
		public static void SimulateClick(int x, int y, int duration) {
			MoveMouse(x, y);
			CppImports.MouseDown();
			Thread.Sleep(duration);
			CppImports.MouseUp();
		}
		/**<summary>Moves the mouse.</summary>*/
		public static void MoveMouse(int x, int y) {
			Cursor.Position = new Point(x, y);
		}
		/**<summary>Clicks the mouse.</summary>*/
		public static void MouseClick() {
			CppImports.MouseDown();
			CppImports.MouseUp();
		}
		/**<summary>Presses the mouse down.</summary>*/
		public static void MouseDown() {
			CppImports.MouseDown();
		}
		/**<summary>Releases the mouse.</summary>*/
		public static void MouseUp() {
			CppImports.MouseUp();
		}
	}
}
