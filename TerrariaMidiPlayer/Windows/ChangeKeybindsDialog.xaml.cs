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

namespace TerrariaMidiPlayer.Windows {
	/// <summary>
	/// Interaction logic for EditKeybindsDialog.xaml
	/// </summary>
	public partial class ChangeKeybindsDialog : Window {

		List<Midi> midis;
		Keybind play;
		Keybind pause;
		Keybind stop;
		Keybind close;
		Keybind mount;

		public ChangeKeybindsDialog(Keybind play, Keybind pause, Keybind stop, Keybind close, Keybind mount, bool closeNoFocus, bool playbackNoFocus, List<Midi> midis) {
			InitializeComponent();
			this.play = play;
			this.pause = pause;
			this.stop = stop;
			this.close = close;
			this.mount = mount;
			this.midis = midis;
			keybindReaderPlay.Keybind = play;
			keybindReaderPause.Keybind = pause;
			keybindReaderStop.Keybind = stop;
			keybindReaderClose.Keybind = close;
			keybindReaderMount.Keybind = mount;
			checkBoxClose.IsChecked = closeNoFocus;
			checkBoxPlayback.IsChecked = playbackNoFocus;
		}

		private void OnOKClicked(object sender, RoutedEventArgs e) {
			DialogResult = true;
		}
		
		public static bool ShowDialog(Window owner, ref Keybind play, ref Keybind pause, ref Keybind stop, ref Keybind close, ref Keybind mount, ref bool closeNoFocus, ref bool playbackNoFocus, List<Midi> midis) {
			ChangeKeybindsDialog window = new ChangeKeybindsDialog(play, pause, stop, close, mount, closeNoFocus, playbackNoFocus, midis);
			window.Owner = owner;
			var result = window.ShowDialog();
			if (result != null && result.Value) {
				play = window.play;
				pause = window.pause;
				stop = window.stop;
				close = window.close;
				mount = window.mount;
				closeNoFocus = window.checkBoxClose.IsChecked.Value;
				playbackNoFocus = window.checkBoxPlayback.IsChecked.Value;
				return true;
			}
			return false;
		}

		private void OnKeybindChanged(object sender, RoutedEventArgs e) {
			Keybind previous = Keybind.None;
			if (sender == keybindReaderStop) {
				if (keybindReaderStop.Keybind == Keybind.None) {
					TriggerMessageBox.Show(this, MessageIcon.Warning, "Cannot unassign the stop keybind!", "Can't Unbind");
					keybindReaderStop.Keybind = stop;
					return;
				}
				previous = stop;
			}
			else if (sender == keybindReaderPlay) {
				previous = play;
			}
			else if (sender == keybindReaderPause) {
				previous = pause;
			}
			else if (sender == keybindReaderClose) {
				previous = close;
			}
			else if (sender == keybindReaderMount) {
				previous = mount;
			}

			Keybind newBind = ((KeybindReader)sender).Keybind;
			string name = "";
			if (newBind != Keybind.None) {
				if (newBind == play && sender != keybindReaderPlay)
					name = "Play Midi";
				else if (newBind == pause && sender != keybindReaderPause)
					name = "Pause Midi";
				else if (newBind == stop && sender != keybindReaderStop)
					name = "Stop Midi";
				else if (newBind == close && sender != keybindReaderClose)
					name = "Close Window";
				else if (newBind == mount && sender != keybindReaderMount)
					name = "Toggle Mount";
				else {
					for (int i = 0; i < midis.Count; i++) {
						if (newBind == midis[i].Keybind) {
							name = midis[i].ProperName;
							break;
						}
					}
				}
			}
			if (name == "") {
				if (sender == keybindReaderStop)
					stop = newBind;
				else if (sender == keybindReaderPlay)
					play = newBind;
				else if (sender == keybindReaderPause)
					pause = newBind;
				else if (sender == keybindReaderClose)
					close = newBind;
				else if (sender == keybindReaderMount)
					mount = newBind;
			}
			else {
				TriggerMessageBox.Show(this, MessageIcon.Error, "Keybind is already in use by the '" + name + "' keybind!", "Keybind in Use");
				((KeybindReader)sender).Keybind = previous;
			}
		}
	}
}
