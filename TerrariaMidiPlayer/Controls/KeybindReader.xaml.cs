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
using TerrariaMidiPlayer.Util;
using Keys = System.Windows.Forms.Keys;

namespace TerrariaMidiPlayer.Controls {
	/**<summary>Signifies changes to a keybind.</summary>*/
	public class KeybindChangedEventArgs : RoutedEventArgs {
		//=========== MEMBERS ============
		#region Members
		
		/**<summary>The previous keybind.</summary>*/
		public Keybind Previous;
		/**<summary>The new keybind.</summary>*/
		public Keybind New;

		#endregion
		//========= CONSTRUCTORS =========
		#region Constructors

		/**<summary>Constructs the keybind change event args.</summary>*/
		public KeybindChangedEventArgs(RoutedEvent routedEvent, Keybind previousBind, Keybind newBind) : base(routedEvent) {
			Previous = previousBind;
			New = newBind;
		}

		#endregion
	}

	/**<summary>A control for selecting a keybind.</summary>*/
	public partial class KeybindReader : UserControl {
		//=========== MEMBERS ============
		#region Members
		
		/**<summary>The keybind for the keybind reader.</summary>*/
		Keybind keybind = Keybind.None;
		/**<summary>The new key while reading a keybind.</summary>*/
		Key newKey = Key.None;
		/**<summary>True if left ctrl is down.</summary>*/
		bool leftCtrl = false;
		/**<summary>True if right ctrl is down.</summary>*/
		bool rightCtrl = false;
		/**<summary>True if left shift is down.</summary>*/
		bool leftShift = false;
		/**<summary>True if right shift is down.</summary>*/
		bool rightShift = false;
		/**<summary>True if left alt is down.</summary>*/
		bool leftAlt = false;
		/**<summary>True if right alt is down.</summary>*/
		bool rightAlt = false;

		#endregion
		//========= CONSTRUCTORS =========
		#region Constructors

		/**<summary>Constructs the keybind reader.</summary>*/
		public KeybindReader() {
			InitializeComponent();
			
			buttonKeybind.Content = Keybind.None.ToProperString();
		}

		#endregion
		//============ EVENTS ============
		#region Events

		/**<summary>Event handler for keybind change events.</summary>*/
		public delegate void KeybindChangedEventHandler(object sender, KeybindChangedEventArgs e);
		/**<summary>The keybind changed routed event.</summary>*/
		public static readonly RoutedEvent KeybindChangedEvent = EventManager.RegisterRoutedEvent("KeybindChanged", RoutingStrategy.Bubble, typeof(KeybindChangedEventHandler), typeof(KeybindReader));
		/**<summary>Called when the keybind has been changed.</summary>*/
		public event KeybindChangedEventHandler KeybindChanged {
			add { AddHandler(KeybindChangedEvent, value); }
			remove { RemoveHandler(KeybindChangedEvent, value); }
		}

		#endregion
		//=========== HELPERS ============
		#region Helpers

		/**<summary>Updates the keybind control.</summary>*/
		private void UpdateKeybind() {
			buttonKeybind.Content = keybind.ToProperString();
			buttonKeybind.IsChecked = false;
		}

		#endregion
		//========== PROPERTIES ==========
		#region Properties

		/**<summary>Gets the current keybind stored by the reader.</summary>*/
		public Keybind Keybind {
			get { return keybind; }
			set {
				keybind = value;
				UpdateKeybind();
			}
		}
		/**<summary>Gets the current key of the keybind stored by the reader.</summary>*/
		public Key Key {
			get { return keybind.Key; }
		}
		/**<summary>Gets the current modifiers of the keybind stored by the reader.</summary>*/
		public ModifierKeys Modifiers {
			get { return keybind.Modifiers; }
		}
		/**<summary>True if currently reading keybind input.</summary>*/
		public bool IsReading {
			get { return buttonKeybind.IsChecked.Value; }
		}

		#endregion
		//============ EVENTS ============
		#region Events
			
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
			else if (keybind != Keybind.None) {
				keybind = Keybind.None;
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
					Keybind previous = keybind;
					ModifierKeys modifiers = ModifierKeys.None;
					if ((leftCtrl || rightCtrl) && (e.Key != Key.LeftCtrl && e.Key != Key.RightCtrl))
						modifiers |= ModifierKeys.Control;
					if ((leftShift || rightShift) && (e.Key != Key.LeftShift && e.Key != Key.RightShift))
						modifiers |= ModifierKeys.Shift;
					if ((leftAlt || rightAlt) && (e.Key != Key.LeftAlt && e.Key != Key.RightAlt))
						modifiers |= ModifierKeys.Alt;
					keybind = new Keybind(k, modifiers);
					if (keybind != previous) {
						UpdateKeybind();
						RaiseEvent(new KeybindChangedEventArgs(KeybindChangedEvent, previous, keybind));
					}
				}
				e.Handled = true;
			}
		}
		private void OnPreviewKeyUp(object sender, KeyEventArgs e) {
			if (buttonKeybind.IsChecked.Value) {
				Key k = (e.SystemKey != Key.None ? e.SystemKey : e.Key);
				bool modifier = true;
				switch (k) {
					case Key.LeftCtrl:
						leftCtrl = false; break;
					case Key.RightCtrl:
						rightCtrl = false; break;
					case Key.LeftShift:
						leftShift = false; break;
					case Key.RightShift:
						rightShift = false; break;
					case Key.LeftAlt:
						leftAlt = false; break;
					case Key.RightAlt:
						rightAlt = false; break;
					case Key.System:
						leftAlt = false; rightAlt = false; break;
					default:
						modifier = false;
						break;
				}
				if (k != Key.None && (!modifier || k == newKey)) {
					Keybind previous = keybind;
					ModifierKeys modifiers = ModifierKeys.None;
					if ((leftCtrl || rightCtrl) && (e.Key != Key.LeftCtrl && e.Key != Key.RightCtrl))
						modifiers |= ModifierKeys.Control;
					if ((leftShift || rightShift) && (e.Key != Key.LeftShift && e.Key != Key.RightShift))
						modifiers |= ModifierKeys.Shift;
					if ((leftAlt || rightAlt) && (e.Key != Key.LeftAlt && e.Key != Key.RightAlt))
						modifiers |= ModifierKeys.Alt;
					keybind = new Keybind(k, modifiers);
					if (keybind != previous) {
						UpdateKeybind();
						RaiseEvent(new KeybindChangedEventArgs(KeybindChangedEvent, previous, keybind));
					}
				}
				e.Handled = true;
			}
		}

		#endregion
	}
}
