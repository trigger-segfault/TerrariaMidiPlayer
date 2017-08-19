using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

namespace TerrariaMidiPlayer {
	public struct Keybind {
		
		private enum MapType : uint {
			MAPVK_VK_TO_VSC = 0x0,
			MAPVK_VSC_TO_VK = 0x1,
			MAPVK_VK_TO_CHAR = 0x2,
			MAPVK_VSC_TO_VK_EX = 0x3,
		}

		[DllImport("user32.dll")]
		private static extern int ToUnicode(
			uint wVirtKey,
			uint wScanCode,
			byte[] lpKeyState,
			[Out, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 4)]
			StringBuilder pwszBuff,
			int cchBuff,
			uint wFlags);

		[DllImport("user32.dll")]
		private static extern bool GetKeyboardState(byte[] lpKeyState);

		[DllImport("user32.dll")]
		private static extern uint MapVirtualKey(uint uCode, MapType uMapType);

		private static char GetCharFromKey(Key key) {
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

			uint scanCode = MapVirtualKey((uint)virtualKey, MapType.MAPVK_VK_TO_VSC);
			StringBuilder stringBuilder = new StringBuilder(2);

			int result = ToUnicode((uint)virtualKey, scanCode, keyboardState, stringBuilder, stringBuilder.Capacity, 0);
			switch (result) {
				case -1:
					break;
				case 0:
					break;
				case 1: {
						ch = stringBuilder[0];
						break;
					}
				default: {
						ch = stringBuilder[0];
						break;
					}
			}
			return ch;
		}

		public static Keybind None {
			get { return new Keybind(Key.None); }
		}

		public Key Key;
		public ModifierKeys Modifiers;

		public Keybind(Key key) {
			Key = key;
			Modifiers = ModifierKeys.None;
		}
		public Keybind(Key key, ModifierKeys modifiers) {
			Key = key;
			Modifiers = modifiers;
		}
		public bool IsDown(System.Windows.Input.KeyEventArgs e) {
			return (e.Key == Key && Keyboard.Modifiers == Modifiers);
		}
		public bool IsDown(System.Windows.Forms.KeyEventArgs e) {
			Keys mods = Keys.None;
			if (Modifiers.HasFlag(ModifierKeys.Control))
				mods |= Keys.Control;
			if (Modifiers.HasFlag(ModifierKeys.Shift))
				mods |= Keys.Shift;
			if (Modifiers.HasFlag(ModifierKeys.Alt))
				mods |= Keys.Alt;
			return (e.KeyCode == (Keys)KeyInterop.VirtualKeyFromKey(Key) && e.Modifiers == mods);
		}

		public static bool operator ==(Keybind a, Keybind b) {
			return (a.Key == b.Key && (a.Modifiers == b.Modifiers || (a.Key == Key.None && b.Key == Key.None)));
		}
		public static bool operator !=(Keybind a, Keybind b) {
			return (a.Key != b.Key || (a.Modifiers != b.Modifiers && (a.Key != Key.None || b.Key != Key.None)));
		}
		public override bool Equals(object obj) {
			if (obj is Keybind) {
				return (this == ((Keybind)obj));
			}
			return false;
		}
		public bool Equals(Keybind keybind) {
			return (this == keybind);
		}
		public override int GetHashCode() {
			return (int)Key ^ (int)Modifiers;
		}

		public string ToCharString() {
			if (Key == Key.None)
				return "<No Keybind>";

			System.Windows.Forms.Keys formsKey = (System.Windows.Forms.Keys)KeyInterop.VirtualKeyFromKey(Key);
			string displayString = "";
			if (Modifiers.HasFlag(ModifierKeys.Control)) {
				formsKey |= System.Windows.Forms.Keys.Control;
				displayString += "Ctrl+";
			}
			if (Modifiers.HasFlag(ModifierKeys.Shift)) {
				formsKey |= System.Windows.Forms.Keys.Shift;
				displayString += "Shift+";
			}
			if (Modifiers.HasFlag(ModifierKeys.Alt)) {
				formsKey |= System.Windows.Forms.Keys.Alt;
				displayString += "Alt+";
			}

			char mappedChar = Char.ToUpper(GetCharFromKey(Key));
			if (Key >= Key.NumPad0 && Key <= Key.NumPad9)
				displayString += Key.ToString();
			else if (Key >= Key.Multiply && Key <= Key.Divide && Key != Key.Separator) {
				switch (Key) {
					case Key.Multiply: displayString += "NumPad*"; break;
					case Key.Add: displayString += "NumPad+"; break;
					case Key.Subtract: displayString += "NumPad-"; break;
					case Key.Decimal: displayString += "NumPad."; break;
					case Key.Divide: displayString += "NumPad/"; break;
				}
			}
			else if (Key == Key.Pause)
				displayString += "PauseBreak";
			else if (mappedChar > 32) // Yes, exclude space
				displayString += mappedChar;
			else if (Key == Key.Back)
				displayString += "Backspace";
			else if (Key == Key.System)
				displayString += "Alt";
			else
				displayString += Key.ToString();
			return displayString;
		}

		public override string ToString() {
			string str = "";
			if (Modifiers.HasFlag(ModifierKeys.Control))
				str += "Ctrl+";
			if (Modifiers.HasFlag(ModifierKeys.Shift))
				str += "Shift+";
			if (Modifiers.HasFlag(ModifierKeys.Alt))
				str += "Alt+";
			str += Key.ToString();
			return str;
		}

		public static bool TryParse(string s, out Keybind keybind) {
			Key key = Key.None;
			ModifierKeys modifiers = ModifierKeys.None;
			for (int i = 0; i < 3; i++) {
				if (!modifiers.HasFlag(ModifierKeys.Control) && s.StartsWith("Ctrl+", StringComparison.InvariantCultureIgnoreCase)) {
					modifiers |= ModifierKeys.Control;
					s = s.Substring(5);
				}
				if (!modifiers.HasFlag(ModifierKeys.Shift) && s.StartsWith("Shift+", StringComparison.InvariantCultureIgnoreCase)) {
					modifiers |= ModifierKeys.Shift;
					s = s.Substring(6);
				}
				if (!modifiers.HasFlag(ModifierKeys.Alt) && s.StartsWith("Alt+", StringComparison.InvariantCultureIgnoreCase)) {
					modifiers |= ModifierKeys.Alt;
					s = s.Substring(4);
				}
			}
			if (Enum.TryParse<Key>(s, out key)) {
				keybind = new Keybind(key, modifiers);
				return true;
			}
			keybind = Keybind.None;
			return false;
		}
	}
}
