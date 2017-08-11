using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
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
using Keys = System.Windows.Forms.Keys;

namespace TerrariaMidiPlayer {


	public struct Keybind {

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

	/// <summary>
	/// Interaction logic for KeybindReader.xaml
	/// </summary>
	public partial class KeybindReader : UserControl {

		public enum MapType : uint {
			MAPVK_VK_TO_VSC = 0x0,
			MAPVK_VSC_TO_VK = 0x1,
			MAPVK_VK_TO_CHAR = 0x2,
			MAPVK_VSC_TO_VK_EX = 0x3,
		}

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
		public static extern uint MapVirtualKey(uint uCode, MapType uMapType);

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

		Key key;
		ModifierKeys modifiers;
		Key newKey;
		bool leftCtrl;
		bool rightCtrl;
		bool leftShift;
		bool rightShift;
		bool leftAlt;
		bool rightAlt;
		const string UnsetText = "<No Keybind>";

		
		public static readonly RoutedEvent KeybindChangedEvent = EventManager.RegisterRoutedEvent("KeybindChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(KeybindReader));

		public event RoutedEventHandler KeybindChanged;

		public KeybindReader() {
			InitializeComponent();
			key = Key.None;
			modifiers = ModifierKeys.None;
			newKey = Key.None;
			buttonKeybind.Content = UnsetText;

			leftCtrl = false;
			rightCtrl = false;
			leftShift = false;
			rightShift = false;
			leftAlt = false;
			rightAlt = false;
		}

		private void UpdateKeybind() {
			if (key == Key.None) {
				buttonKeybind.Content = UnsetText;
			}
			else {
				System.Windows.Forms.Keys formsKey = (System.Windows.Forms.Keys)key;
				string displayString = "";
				if (modifiers.HasFlag(ModifierKeys.Control)) {
					formsKey |= System.Windows.Forms.Keys.Control;
					displayString += "Ctrl+";
				}
				if (modifiers.HasFlag(ModifierKeys.Shift)) {
					formsKey |= System.Windows.Forms.Keys.Shift;
					displayString += "Shift+";
				}
				if (modifiers.HasFlag(ModifierKeys.Alt)) {
					formsKey |= System.Windows.Forms.Keys.Alt;
					displayString += "Alt+";
				}

				char mappedChar = Char.ToUpper(GetCharFromKey(key));
				if (key >= Key.NumPad0 && key <= Key.NumPad9)
					mappedChar = '\0';
				if (mappedChar > 32) { // Yes, exclude space
					displayString += mappedChar;
				}
				else if (key == Key.System) {
					displayString += "Alt";
				}
				else {
					displayString += key.ToString();
				}
				//string keyDisplayString = TypeDescriptor.GetConverter(typeof(System.Windows.Forms.Keys)).ConvertToString(formsKey);
				buttonKeybind.Content = displayString;
			}
			buttonKeybind.IsChecked = false;
		}

		/*public KeyGesture Keybind {
			get { return keybind; }
			set {
				buttonKeybind.IsChecked = false;
				keybind = value;
				if (keybind != null)
					buttonKeybind.Content = keybind.GetDisplayStringForCulture(CultureInfo.CurrentCulture);
				else
					buttonKeybind.Content = UnsetText;
			}
		}*/
		public Key Key {
			get { return key; }
			set {
				buttonKeybind.IsChecked = false;
				key = value;
				UpdateKeybind();
			}
		}
		public ModifierKeys Modifiers {
			get { return modifiers; }
			set {
				buttonKeybind.IsChecked = false;
				modifiers = value;
				UpdateKeybind();
			}
		}

		public Keybind Keybind {
			get { return new Keybind(key, modifiers); }
			set {
				buttonKeybind.IsChecked = false;
				key = value.Key;
				modifiers = value.Modifiers;
				UpdateKeybind();
			}
		}

		public bool IsReading {
			get { return buttonKeybind.IsChecked.Value; }
		}

		private void OnLoaded(object sender, RoutedEventArgs e) {
			if (!DesignerProperties.GetIsInDesignMode(this)) {
				var window = Window.GetWindow(this);
				window.PreviewKeyDown += OnPreviewKeyDown;
				window.PreviewKeyUp += OnPreviewKeyUp;
			}
		}

		private void OnButtonClicked(object sender, RoutedEventArgs e) {
			if (buttonKeybind.IsChecked.Value) {
				newKey = Key.None;
				leftCtrl = false;
				rightCtrl = false;
				leftShift = false;
				rightShift = false;
				leftAlt = false;
				rightAlt = false;
				buttonKeybind.Content = "<Press Any Key>";
			}
			else {
				key = Key.None;
				modifiers = ModifierKeys.None;
				UpdateKeybind();
				RaiseEvent(new RoutedEventArgs(KeybindReader.KeybindChangedEvent));
			}
		}

		private void OnPreviewKeyDown(object sender, KeyEventArgs e) {
			if (buttonKeybind.IsChecked.Value) {
				Key k = (e.SystemKey != Key.None ? e.SystemKey : e.Key);
				bool modifier = true;
				switch (k) {
					case Key.LeftCtrl:
						leftCtrl = true; break;
					case Key.RightCtrl:
						rightCtrl = true; break;
					case Key.LeftShift:
						leftShift = true; break;
					case Key.RightShift:
						rightShift = true; break;
					case Key.LeftAlt:
						leftAlt = true; break;
					case Key.RightAlt:
						rightAlt = true; break;
					case Key.System:
						leftAlt = true; rightAlt = true; break;
					default:
						modifier = false; break;
				}
				newKey = k;
				if (!modifier && k != Key.None) {
					modifiers = ModifierKeys.None;
					if ((leftCtrl || rightCtrl) && (e.Key != Key.LeftCtrl && e.Key != Key.RightCtrl))
						modifiers |= ModifierKeys.Control;
					if ((leftShift || rightShift) && (e.Key != Key.LeftShift && e.Key != Key.RightShift))
						modifiers |= ModifierKeys.Shift;
					if ((leftAlt || rightAlt) && (e.Key != Key.LeftAlt && e.Key != Key.RightAlt))
						modifiers |= ModifierKeys.Alt;
					key = k;
					UpdateKeybind();
					RaiseEvent(new RoutedEventArgs(KeybindReader.KeybindChangedEvent));
				}
				e.Handled = true;
			}
		}

		private void OnPreviewKeyUp(object sender, KeyEventArgs e) {
			if (buttonKeybind.IsChecked.Value) {
				Key k = (e.SystemKey != Key.None ? e.SystemKey : e.Key);
				bool modifier = true;
				bool modifierCleared = false;
				switch (k) {
					case Key.LeftCtrl:
						leftCtrl = false; modifierCleared = rightCtrl; break;
					case Key.RightCtrl:
						rightCtrl = false; modifierCleared = leftCtrl; break;
					case Key.LeftShift:
						leftShift = false; modifierCleared = rightShift; break;
					case Key.RightShift:
						rightShift = false; modifierCleared = leftShift; break;
					case Key.LeftAlt:
						leftAlt = false; modifierCleared = rightAlt; break;
					case Key.RightAlt:
						rightAlt = false; modifierCleared = leftAlt; break;
					case Key.System:
						leftAlt = false; rightAlt = false; break;
					default:
						modifier = false;
						break;
				}
				if (k != Key.None && (!modifier || k == newKey)) {
					modifiers = ModifierKeys.None;
					if ((leftCtrl || rightCtrl) && (e.Key != Key.LeftCtrl && e.Key != Key.RightCtrl))
						modifiers |= ModifierKeys.Control;
					if ((leftShift || rightShift) && (e.Key != Key.LeftShift && e.Key != Key.RightShift))
						modifiers |= ModifierKeys.Shift;
					if ((leftAlt || rightAlt) && (e.Key != Key.LeftAlt && e.Key != Key.RightAlt))
						modifiers |= ModifierKeys.Alt;
					key = k;
					UpdateKeybind();
					RaiseEvent(new RoutedEventArgs(KeybindReader.KeybindChangedEvent));
				}
				e.Handled = true;
			}
		}
	}
}
