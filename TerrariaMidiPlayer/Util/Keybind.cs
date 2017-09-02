using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

namespace TerrariaMidiPlayer.Util {
	/**<summary>A combinations of a key and modifiers to perform actions with.</summary>*/
	public struct Keybind {
		//========== CONSTANTS ===========
		#region Constants

		/**<summary>The default unset keybind.</summary>*/
		public static Keybind None {
			get { return new Keybind(Key.None); }
		}

		#endregion
		//=========== MEMBERS ============
		#region Members

		/**<summary>The key for the keybind.</summary>*/
		public Key Key;
		/**<summary>The modifiers for the keybind.</summary>*/
		public ModifierKeys Modifiers;

		#endregion
		//========= CONSTRUCTORS =========
		#region Constructors

		/**<summary>Constructs a keybind with the specified key.</summary>*/
		public Keybind(Key key) {
			Key = key;
			Modifiers = ModifierKeys.None;
		}
		/**<summary>Constructs a keybind with the specified key and modifiers.</summary>*/
		public Keybind(Key key, ModifierKeys modifiers) {
			Key = key;
			Modifiers = modifiers;
		}

		#endregion
		//=========== TESTING ============
		#region Testing

		/**<summary>Tests if the keybind is down with a WPF key event.</summary>*/
		public bool IsDown(System.Windows.Input.KeyEventArgs e) {
			return (e.Key == Key && Keyboard.Modifiers == Modifiers);
		}
		/**<summary>Tests if the keybind is down with a Windows Forms key event.</summary>*/
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

		/**<summary>Gets if the keybinds are equal. Modifiers do not apply when the key is none.</summary>*/
		public static bool operator ==(Keybind a, Keybind b) {
			return (a.Key == b.Key && (a.Modifiers == b.Modifiers || (a.Key == Key.None && b.Key == Key.None)));
		}
		/**<summary>Gets if the keybinds are unequal. Modifiers do not apply when the key is none.</summary>*/
		public static bool operator !=(Keybind a, Keybind b) {
			return (a.Key != b.Key || (a.Modifiers != b.Modifiers && (a.Key != Key.None || b.Key != Key.None)));
		}
		/**<summary>Gets if the keybinds are equal. Modifiers do not apply when the key is none.</summary>*/
		public override bool Equals(object obj) {
			if (obj is Keybind) {
				return (this == ((Keybind)obj));
			}
			return false;
		}
		/**<summary>Gets if the keybinds are equal. Modifiers do not apply when the key is none.</summary>*/
		public bool Equals(Keybind keybind) {
			return (this == keybind);
		}
		/**<summary>Gets the hash code of the keybind.</summary>*/
		public override int GetHashCode() {
			return (int)Key | ((int)Modifiers << 8);
		}

		#endregion
		//=========== STRINGS ============
		#region Strings

		/**<summary>Gets the human-readable string of the keybind.</summary>*/
		public string ToProperString() {
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

			char mappedChar = Char.ToUpper(CppImports.GetCharFromKey(Key));
			if (Key >= Key.Multiply && Key <= Key.Divide && Key != Key.Separator) {
				switch (Key) {
					case Key.Multiply:	displayString += "NumPad*"; break;
					case Key.Add:		displayString += "NumPad+"; break;
					case Key.Subtract:	displayString += "NumPad-"; break;
					case Key.Decimal:	displayString += "NumPad."; break;
					case Key.Divide:	displayString += "NumPad/"; break;
				}
			}
			else if (Key >= Key.NumPad0 && Key <= Key.NumPad9)
				displayString += Key.ToString();
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

		/**<summary>Gets the parsable string of the keybind.</summary>*/
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
		/**<summary>Tries to parse the keybind.</summary>*/
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

		#endregion
	}
}
