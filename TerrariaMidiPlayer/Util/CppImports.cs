using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace TerrariaMidiPlayer.Util {
	/**<summary>A collection of DllImported functions and wrapper functions.</summary>*/
	public static class CppImports {
		//============ ENUMS =============
		#region Enums

		public enum MapTypes : uint {
			MAPVK_VK_TO_VSC = 0x0,
			MAPVK_VSC_TO_VK = 0x1,
			MAPVK_VK_TO_CHAR = 0x2,
			MAPVK_VSC_TO_VK_EX = 0x3
		}

		public enum ShowWindowEnum : int {
			Hide = 0,
			ShowNormal = 1, ShowMinimized = 2, ShowMaximized = 3,
			Maximize = 3, ShowNormalNoActivate = 4, Show = 5,
			Minimize = 6, ShowMinNoActivate = 7, ShowNoActivate = 8,
			Restore = 9, ShowDefault = 10, ForceMinimized = 11
		}

		#endregion
		//=========== STRUCTS ============
		#region Structs

		[StructLayout(LayoutKind.Sequential)]
		public struct RECT {
			public int left;
			public int top;
			public int right;
			public int bottom;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct POINT {
			public int x;
			public int y;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct WINDOWPLACEMENT {
			public int length;
			public int flags;
			public int showCmd;
			public System.Drawing.Point ptMinPosition;
			public System.Drawing.Point ptMaxPosition;
			public System.Drawing.Rectangle rcNormalPosition;
		}

#pragma warning disable 649

		public struct INPUT {
			public uint Type;
			public MOUSEKEYBDHARDWAREINPUT Data;
		}

		[StructLayout(LayoutKind.Explicit)]
		public struct MOUSEKEYBDHARDWAREINPUT {
			[FieldOffset(0)]
			public MOUSEINPUT Mouse;
		}

		public struct MOUSEINPUT {
			public int X;
			public int Y;
			public uint MouseData;
			public uint Flags;
			public uint Time;
			public IntPtr ExtraInfo;
		}

#pragma warning restore 649

		#endregion
		//========== FUNCTIONS ===========
		#region Functions

		[DllImport("user32.dll")]
		public static extern short GetKeyState(int keyCode);
		
		[DllImport("user32.dll")]
		public static extern int ToUnicode(
			uint wVirtKey,
			uint wScanCode,
			byte[] lpKeyState,
			[Out, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 4)]
			StringBuilder pwszBuff,
			int cchBuff,
			uint wFlags);

		[DllImport("user32.dll")]
		public static extern bool GetKeyboardState(byte[] lpKeyState);

		[DllImport("user32.dll")]
		public static extern uint MapVirtualKey(uint uCode, MapTypes uMapType);
		
		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetClientRect(IntPtr hWnd, ref RECT lpRect);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool ShowWindowAsync(HandleRef hHandle, int nCmdShow);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool ShowWindow(HandleRef hHandle, int nCmdShow);

		[DllImport("user32.dll")]
		public static extern IntPtr GetForegroundWindow();

		[DllImport("user32.dll")]
		public static extern int SetForegroundWindow(IntPtr hWnd);

		[DllImport("user32.dll")]
		public static extern void SwitchToThisWindow(IntPtr hWnd, [MarshalAs(UnmanagedType.Bool)]bool fAltTab);

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

		[DllImport("user32.dll")]
		public static extern bool ClientToScreen(IntPtr hWnd, ref Point lpPoint);
		
		[DllImport("user32.dll")]
		public static extern uint SendInput(uint nInputs, [MarshalAs(UnmanagedType.LPArray), In] INPUT[] pInputs, int cbSize);
		
		#endregion
		//============ OTHERS ============
		#region Others

		//https://stackoverflow.com/questions/31020626/how-to-detect-if-keyboard-has-numeric-block
		/**<summary>Returns true if the computer's keyboard has a numpad.</summary>*/
		public static bool KeyboardHasNumpad {
			get { return (((ushort)GetKeyState(0x90)) & 0xffff) != 0; }
		}

		/**<summary>Gets the chracter from the keyboard key.</summary>*/
		public static char GetCharFromKey(Key key) {
			char ch = ' ';

			int virtualKey = KeyInterop.VirtualKeyFromKey(key);
			byte[] keyboardState = new byte[256];
			GetKeyboardState(keyboardState);
			keyboardState[(int)System.Windows.Forms.Keys.ControlKey] = 0;
			keyboardState[(int)System.Windows.Forms.Keys.ShiftKey] = 0;
			keyboardState[(int)System.Windows.Forms.Keys.Menu] = 0;
			keyboardState[(int)Key.LeftCtrl] = 0;
			keyboardState[(int)Key.RightCtrl] = 0;
			keyboardState[(int)Key.LeftShift] = 0;
			keyboardState[(int)Key.RightShift] = 0;
			keyboardState[(int)Key.LeftAlt] = 0;
			keyboardState[(int)Key.RightAlt] = 0;

			uint scanCode = MapVirtualKey((uint)virtualKey, MapTypes.MAPVK_VK_TO_VSC);
			StringBuilder stringBuilder = new StringBuilder(2);

			int result = ToUnicode((uint)virtualKey, scanCode, keyboardState, stringBuilder, stringBuilder.Capacity, 0);
			switch (result) {
				case -1:
				case 0: break;
				case 1:
				default: ch = stringBuilder[0]; break;
			}
			return ch;
		}
		/**<summary>Restores the window to either maximized or regular state.</summary>*/
		public static void RestoreFromMinimzied(IntPtr windowHandle) {
			const int WPF_RESTORETOMAXIMIZED = 0x2;
			WINDOWPLACEMENT placement = new WINDOWPLACEMENT();
			placement.length = Marshal.SizeOf(placement);
			GetWindowPlacement(windowHandle, ref placement);

			if ((placement.flags & WPF_RESTORETOMAXIMIZED) == WPF_RESTORETOMAXIMIZED)
				ShowWindow(new HandleRef(null, windowHandle), (int)ShowWindowEnum.ShowMaximized);
			else
				ShowWindow(new HandleRef(null, windowHandle), (int)ShowWindowEnum.ShowNormal);
		}
		/**<summary>Gets the client area of the window based on the screen.</summary>*/
		public static Rect GetClientArea(IntPtr windowHandle) {
			Rect clientArea; ;
			RECT lpRect = new RECT();
			IntPtr hWnd = windowHandle;
			if (GetClientRect(hWnd, ref lpRect)) {
				clientArea = new Rect(lpRect.left, lpRect.top, lpRect.right, lpRect.bottom);
				POINT lpPoint = new POINT();
				lpPoint.x = (int)clientArea.X;
				lpPoint.y = (int)clientArea.Y;
				if (ClientToScreen(hWnd, ref lpPoint)) {
					clientArea.Location = new Point(lpPoint.x, lpPoint.y);
					return clientArea;
				}
				else {
					return new Rect(0, 0, 0, 0);
				}
			}
			else {
				return new Rect(0, 0, 0, 0);
			}
		}
		/**<summary>Gets if the window has focus.</summary>*/
		public static bool WindowHasFocus(IntPtr windowHandle) {
			return GetForegroundWindow() == windowHandle;
		}
		/**<summary>Focuses on the window.</summary>*/
		public static void FocusWindow(IntPtr windowHandle) {
			SetForegroundWindow(windowHandle);
		}

		//https://stackoverflow.com/questions/10355286/programmatically-mouse-click-in-another-window
		/**<summary>Simulates the mouse pressing down.</summary>*/
		public static void MouseDown() {
			var inputMouseDown = new INPUT();
			inputMouseDown.Type = 0; /// input type mouse
			inputMouseDown.Data.Mouse.Flags = 0x0002; /// left button down

			var inputs = new INPUT[] { inputMouseDown };
			SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
		}
		/**<summary>Simulates the mouse releasing.</summary>*/
		public static void MouseUp() {
			var inputMouseUp = new INPUT();
			inputMouseUp.Type = 0; /// input type mouse
			inputMouseUp.Data.Mouse.Flags = 0x0004; /// left button up

			var inputs = new INPUT[] { inputMouseUp };
			SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
		}

		#endregion
	}
}
