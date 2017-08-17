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

namespace TerrariaMidiPlayer.Controls {

	/// <summary>
	/// Interaction logic for KeybindReader.xaml
	/// </summary>
	public partial class KeybindReader : UserControl {

		Key key;
		ModifierKeys modifiers;
		Key newKey;
		bool leftCtrl;
		bool rightCtrl;
		bool leftShift;
		bool rightShift;
		bool leftAlt;
		bool rightAlt;

		
		public static readonly RoutedEvent KeybindChangedEvent = EventManager.RegisterRoutedEvent("KeybindChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(KeybindReader));

		public event RoutedEventHandler KeybindChanged;

		public KeybindReader() {
			InitializeComponent();
			key = Key.None;
			modifiers = ModifierKeys.None;
			newKey = Key.None;
			buttonKeybind.Content = Keybind.None.ToCharString();

			leftCtrl = false;
			rightCtrl = false;
			leftShift = false;
			rightShift = false;
			leftAlt = false;
			rightAlt = false;
		}

		private void UpdateKeybind() {
			buttonKeybind.Content = new Keybind(key, modifiers).ToCharString();
			buttonKeybind.IsChecked = false;
		}
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
