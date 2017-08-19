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
	public static class TerrariaWindowLocator {

		[StructLayout(LayoutKind.Sequential)]
		private struct RECT {
			public int left;
			public int top;
			public int right;
			public int bottom;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct POINT {
			public int x;
			public int y;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct WINDOWPLACEMENT {
			public int length;
			public int flags;
			public int showCmd;
			public System.Drawing.Point ptMinPosition;
			public System.Drawing.Point ptMaxPosition;
			public System.Drawing.Rectangle rcNormalPosition;
		}

		private enum ShowWindowEnum : int {
			Hide = 0,
			ShowNormal = 1, ShowMinimized = 2, ShowMaximized = 3,
			Maximize = 3, ShowNormalNoActivate = 4, Show = 5,
			Minimize = 6, ShowMinNoActivate = 7, ShowNoActivate = 8,
			Restore = 9, ShowDefault = 10, ForceMinimized = 11
		}

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool GetClientRect(IntPtr hWnd, ref RECT lpRect);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool ShowWindowAsync(HandleRef hHandle, int nCmdShow);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool ShowWindow(HandleRef hHandle, int nCmdShow);

		[DllImport("user32.dll")]
		private static extern IntPtr GetForegroundWindow();

		[DllImport("user32.dll")]
		private static extern int SetForegroundWindow(IntPtr hWnd);

		[DllImport("user32.dll")]
		private static extern void SwitchToThisWindow(IntPtr hWnd, [MarshalAs(UnmanagedType.Bool)]bool fAltTab);

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

		private static void RestoreFromMinimzied() {
			const int WPF_RESTORETOMAXIMIZED = 0x2;
			WINDOWPLACEMENT placement = new WINDOWPLACEMENT();
			placement.length = Marshal.SizeOf(placement);
			GetWindowPlacement(process.MainWindowHandle, ref placement);

			if ((placement.flags & WPF_RESTORETOMAXIMIZED) == WPF_RESTORETOMAXIMIZED)
				ShowWindow(new HandleRef(null, process.MainWindowHandle), (int)ShowWindowEnum.ShowMaximized);
			else
				ShowWindow(new HandleRef(null, process.MainWindowHandle), (int)ShowWindowEnum.ShowNormal);
		}

		//-------------------------------------------------------

		static Process process = null;
		static Rect clientArea = new Rect(0, 0, 0, 0);
		static bool focus = false;
		//static bool hasWritten = false;
		public static string ExeName = "Terraria";

		public static Rect ClientArea {
			get { return clientArea; }
		}

		public static bool IsOpen {
			get { return process != null; }
		}
		public static bool HasFocus {
			get { return focus; }
		}

		public static bool Update(bool fullUpdate) {
			try {
				if (fullUpdate)
					process = Process.GetProcessesByName(ExeName).FirstOrDefault();
				if (process != null) {
					RECT lpRect = new RECT();
					IntPtr hWnd = process.MainWindowHandle;
					if (GetClientRect(hWnd, ref lpRect)) {
						clientArea = new Rect(lpRect.left, lpRect.top, lpRect.right, lpRect.bottom);
						POINT lpPoint = new POINT();
						lpPoint.x = (int)clientArea.X;
						lpPoint.y = (int)clientArea.Y;
						if (ClientToScreen(hWnd, ref lpPoint)) {
							clientArea.Location = new Point(lpPoint.x, lpPoint.y);
							focus = (GetForegroundWindow() == process.MainWindowHandle);
						}
						else {
							return false;
						}
					}
					else {
						return false;
					}
					return true;
				}
				else {
					clientArea = new Rect(0, 0, 0, 0);
					focus = false;
					return true;
				}
			}
			catch (Exception ex) {
				clientArea = new Rect(0, 0, 0, 0);
				focus = false;
				return false;
			}
		}

		//https://stackoverflow.com/questions/2315561/correct-way-in-net-to-switch-the-focus-to-another-application?answertab=oldest#tab-top

		public static bool Focus() {
			try {
				if (process != null) {
					// The window is hidden so try to restore it before setting focus.
					//ShowWindow(new HandleRef(null, process.MainWindowHandle), (int)ShowWindowEnum.Restore);
					RestoreFromMinimzied();

					// Get the hWnd of the process
					IntPtr hWnd = process.MainWindowHandle;

					// Set user the focus to the window
					SetForegroundWindow(hWnd);

					// Reacquire the client rect
					RECT lpRect = new RECT();
					if (GetClientRect(hWnd, ref lpRect)) {
						clientArea = new Rect(lpRect.left, lpRect.top, lpRect.right, lpRect.bottom);
						POINT lpPoint = new POINT();
						lpPoint.x = (int)clientArea.X;
						lpPoint.y = (int)clientArea.Y;
						if (ClientToScreen(hWnd, ref lpPoint)) {
							clientArea.Location = new Point(lpPoint.x, lpPoint.y);
							focus = (GetForegroundWindow() == process.MainWindowHandle);
						}
						else {
							return false;
						}
					}
					else {
						return false;
					}
					return true;
				}
				return false;
			}
			catch (Exception ex) {
				return false;
			}
		}

		public static bool CheckIfFocused() {
			Update(true);
			return HasFocus;
		}
	}
}
