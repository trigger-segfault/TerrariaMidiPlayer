using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace TerrariaMidiPlayer.Util {
	/**<summary>Used to keep track of the Terraria window.</summary>*/
	public static class TerrariaWindowLocator {
		//=========== MEMBERS ============
		#region Members

		/**<summary>The object for the Terraria process.</summary>*/
		private static Process process = null;
		/**<summary>True if the Terraria window has focus.</summary>*/
		private static bool focus = false;
		/**<summary>The client area of the Terraria window.</summary>*/
		private static Rect clientArea = new Rect(0, 0, 0, 0);

		#endregion
		//========== PROPERTIES ==========
		#region Properties

		/**<summary>True if the Terraria window is open.</summary>*/
		public static bool IsOpen {
			get { return process != null; }
		}
		/**<summary>True if the Terraria window has focus.</summary>*/
		public static bool HasFocus {
			get { return focus; }
		}
		/**<summary>The client area of the Terraria window.</summary>*/
		public static Rect ClientArea {
			get { return clientArea; }
		}

		#endregion
		//============ CHECKS ============
		#region Checks

		/**<summary>Updates the info on the Terraria window.</summary>*/
		public static bool Update(bool fullUpdate) {
			try {
				if (fullUpdate)
					process = Process.GetProcessesByName(Config.ExecutableName).FirstOrDefault();
				if (process != null) {
					clientArea = CppImports.GetClientArea(process.MainWindowHandle);
					focus = CppImports.WindowHasFocus(process.MainWindowHandle);
					return (clientArea != new Rect(0, 0, 0, 0));
				}
			}
			catch (Exception) { }

			clientArea = new Rect(0, 0, 0, 0);
			focus = false;
			return false;
		}

		//https://stackoverflow.com/questions/2315561/correct-way-in-net-to-switch-the-focus-to-another-application?answertab=oldest#tab-top
		/**<summary>Focuses on the Terraria Window.</summary>*/
		public static bool Focus() {
			try {
				if (process != null) {
					// The window is hidden so try to restore it before setting focus.
					CppImports.RestoreFromMinimzied(process.MainWindowHandle);
					
					// Set user the focus to the window
					CppImports.FocusWindow(process.MainWindowHandle);

					// Reacquire the client rect
					clientArea = CppImports.GetClientArea(process.MainWindowHandle);
					focus = CppImports.WindowHasFocus(process.MainWindowHandle);
					return (clientArea != new Rect(0, 0, 0, 0));
				}
				return false;
			}
			catch (Exception) {
				return false;
			}
		}
		/**<summary>Checks if the Terraria window is focused.</summary>*/
		public static bool CheckIfFocused() {
			Update(true);
			return focus;
		}

		#endregion
	}
}
