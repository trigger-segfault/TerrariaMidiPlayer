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

	//https://stackoverflow.com/questions/10355286/programmatically-mouse-click-in-another-window

	public class MouseControl {

		[DllImport("user32.dll")]
		static extern bool ClientToScreen(IntPtr hWnd, ref Point lpPoint);

		[DllImport("user32.dll")]
		internal static extern uint SendInput(uint nInputs, [MarshalAs(UnmanagedType.LPArray), In] INPUT[] pInputs, int cbSize);

#pragma warning disable 649
		internal struct INPUT {
			public UInt32 Type;
			public MOUSEKEYBDHARDWAREINPUT Data;
		}

		[StructLayout(LayoutKind.Explicit)]
		internal struct MOUSEKEYBDHARDWAREINPUT {
			[FieldOffset(0)]
			public MOUSEINPUT Mouse;
		}

		internal struct MOUSEINPUT {
			public Int32 X;
			public Int32 Y;
			public UInt32 MouseData;
			public UInt32 Flags;
			public UInt32 Time;
			public IntPtr ExtraInfo;
		}

#pragma warning restore 649

		public static void SimulateClick(int x, int y, int duration) {
			MoveMouse(x, y);
			MouseDown();
			Thread.Sleep(duration);
			MouseUp();
		}

		public static void MoveMouse(int x, int y) {
			Cursor.Position = new Point(x, y);
		}

		public static void MouseClick() {
			var inputMouseDown = new INPUT();
			inputMouseDown.Type = 0; /// input type mouse
			inputMouseDown.Data.Mouse.Flags = 0x0002; /// left button down

			var inputMouseUp = new INPUT();
			inputMouseUp.Type = 0; /// input type mouse
			inputMouseUp.Data.Mouse.Flags = 0x0004; /// left button up
			
			var inputs = new INPUT[] { inputMouseDown, inputMouseUp };
			SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
		}

		public static void MouseDown() {
			var inputMouseDown = new INPUT();
			inputMouseDown.Type = 0; /// input type mouse
			inputMouseDown.Data.Mouse.Flags = 0x0002; /// left button down
			
			var inputs = new INPUT[] { inputMouseDown };
			SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
		}

		public static void MouseUp() {
			var inputMouseUp = new INPUT();
			inputMouseUp.Type = 0; /// input type mouse
			inputMouseUp.Data.Mouse.Flags = 0x0004; /// left button up
			
			var inputs = new INPUT[] { inputMouseUp };
			SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
		}

	}
}
