using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using TerrariaMidiPlayer.Controls;
using TerrariaMidiPlayer.Util;

namespace TerrariaMidiPlayer.Windows {
	/**<summary>Used to configure the main keybinds and keybind settings.</summary>*/
	public partial class ChangeKeybindsDialog : Window {
		//========= CONSTRUCTORS =========
		#region Constructors

		/**<summary>Constructs the keybinds dialog.</summary>*/
		public ChangeKeybindsDialog() {
			InitializeComponent();
			keybindReaderPlay.Keybind = Config.Keybinds.Play;
			keybindReaderPause.Keybind = Config.Keybinds.Pause;
			keybindReaderStop.Keybind = Config.Keybinds.Stop;
			keybindReaderClose.Keybind = Config.Keybinds.Close;
			keybindReaderMount.Keybind = Config.Keybinds.Mount;
			checkBoxClose.IsChecked = Config.CloseNoFocus;
			checkBoxPlayback.IsChecked = Config.PlaybackNoFocus;
			checkBoxDisableMount.IsChecked = Config.DisableMountWhenTalking;
		}

		#endregion
		//============ EVENTS ============
		#region Events

		private void OnKeybindChanged(object sender, KeybindChangedEventArgs e) {
			// Stop cannot be unbound for safety reasons
			if (sender == keybindReaderStop) {
				if (keybindReaderStop.Keybind == Keybind.None) {
					TriggerMessageBox.Show(this, MessageIcon.Warning, "Cannot unassign the stop keybind!", "Can't Unbind");
					keybindReaderStop.Keybind = e.Previous;
					return;
				}
			}
			
			// Make sure the keybind isn't already in use
			if (e.New != Keybind.None) {
				string name = "";
				if (e.New == keybindReaderPlay.Keybind && sender != keybindReaderPlay)
					name = "Play Midi";
				else if (e.New == keybindReaderPause.Keybind && sender != keybindReaderPause)
					name = "Pause Midi";
				else if (e.New == keybindReaderStop.Keybind && sender != keybindReaderStop)
					name = "Stop Midi";
				else if (e.New == keybindReaderClose.Keybind && sender != keybindReaderClose)
					name = "Close Window";
				else if (e.New == keybindReaderMount.Keybind && sender != keybindReaderMount)
					name = "Toggle Mount";
				else {
					for (int i = 0; i < Config.Midis.Count; i++) {
						if (e.New == Config.Midis[i].Keybind) {
							name = Config.Midis[i].ProperName;
							break;
						}
					}
				}
				if (name != "") {
					// Nag the user about making poor life choices
					TriggerMessageBox.Show(this, MessageIcon.Warning, "Keybind is already in use by the '" + name + "' keybind!", "Keybind in Use");
					((KeybindReader)sender).Keybind = e.Previous;
				}
			}
		}
		private void OnOKClicked(object sender, RoutedEventArgs e) {
			DialogResult = true;
		}

		#endregion
		//=========== SHOWING ============
		#region Showing

		/**<summary>Shows the keybinds dialog.</summary>*/
		public static bool ShowDialog(Window owner) {
			ChangeKeybindsDialog window = new ChangeKeybindsDialog();
			window.Owner = owner;
			var result = window.ShowDialog();
			if (result != null && result.Value) {
				Config.Keybinds.Play = window.keybindReaderPlay.Keybind;
				Config.Keybinds.Pause = window.keybindReaderPause.Keybind;
				Config.Keybinds.Stop = window.keybindReaderStop.Keybind;
				Config.Keybinds.Close = window.keybindReaderClose.Keybind;
				Config.Keybinds.Mount = window.keybindReaderMount.Keybind;
				Config.CloseNoFocus = window.checkBoxClose.IsChecked.Value;
				Config.PlaybackNoFocus = window.checkBoxPlayback.IsChecked.Value;
				Config.DisableMountWhenTalking = window.checkBoxDisableMount.IsChecked.Value;
				return true;
			}
			return false;
		}

		#endregion

	}
}
