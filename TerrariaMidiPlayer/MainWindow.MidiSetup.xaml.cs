using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TerrariaMidiPlayer.Controls;
using TerrariaMidiPlayer.Util;
using TerrariaMidiPlayer.Windows;

namespace TerrariaMidiPlayer {
	/**<summary>The main window running Terraria Midi Player.</summary>*/
	public partial class MainWindow : Window {
	
		//============ EVENTS ============
		#region Events
		//--------------------------------
		#region Tracks

		private void OnTrackChanged(object sender, SelectionChangedEventArgs e) {
			if (!loaded)
				return;
			trackIndex = listTracks.SelectedIndex;
			UpdateTrack();
		}
		private void OnEditTrackName(object sender, RoutedEventArgs e) {
			if (!Config.PianoMode) {
				StopOrPause();
				loaded = false;
			}

			string newName = EditNameDialog.ShowDialog(this, Config.Midi.GetTrackSettingsAt(trackIndex).ProperName);
			if (newName != null) {
				Config.Midi.GetTrackSettingsAt(trackIndex).Name = newName;
				loaded = false;
				((ListBoxItem)listTracks.Items[trackIndex]).Content = Config.Midi.GetTrackSettingsAt(trackIndex).ProperName;
				//listTracks.SelectedIndex = trackIndex;
				loaded = true;
			}
			loaded = true;
		}
		private void OnTrackEnabledClicked(object sender, RoutedEventArgs e) {
			if (!loaded)
				return;
			loaded = false;

			Config.Midi.GetTrackSettingsAt(trackIndex).Enabled = checkBoxTrackEnabled.IsChecked.Value;

			listTracks.Items.RemoveAt(trackIndex);
			ListBoxItem item = new ListBoxItem();
			item.Content = Config.Midi.GetTrackSettingsAt(trackIndex).ProperName;
			if (!Config.Midi.GetTrackSettingsAt(trackIndex).Enabled)
				item.Foreground = Brushes.Gray;
			listTracks.Items.Insert(trackIndex, item);
			listTracks.SelectedIndex = trackIndex;

			loaded = true;
		}
		private void OnOctaveOffsetChanged(object sender, ValueChangedEventArgs<int> e) {
			if (!loaded)
				return;
			Config.Midi.GetTrackSettingsAt(trackIndex).OctaveOffset = numericOctaveOffset.Value;
			UpdateTrackNotes();
		}
		private void OnTrackGraph(object sender, RoutedEventArgs e) {
			if (!Config.PianoMode) {
				StopOrPause();
			}
			//loaded = false;
			TrackGraphWindow.Show(this, Config.Midi, trackIndex);
			//loaded = true;
			UpdateTrack();
			numericNoteOffset.Value = Config.Midi.NoteOffset;
			numericSpeed.Value = Config.Midi.Speed;
			OnPlaybackUIUpdate(null, null);
			menuItemWrapPianoMode.IsChecked = Config.WrapPianoMode;
			menuItemSkipPianoMode.IsChecked = Config.SkipPianoMode;
		}

		#endregion
		//--------------------------------
		#region Midi

		private void OnNoteOffsetChanged(object sender, ValueChangedEventArgs<int> e) {
			if (!loaded)
				return;
			Config.Midi.NoteOffset = numericNoteOffset.Value;
			UpdateTrackNotes();
		}
		private void OnSpeedChanged(object sender, ValueChangedEventArgs<int> e) {
			if (!loaded)
				return;
			Config.Midi.Speed = numericSpeed.Value;
			Config.Sequencer.Speed = Config.Midi.SpeedRatio;
			labelDuration.Content = "Duration: " + MillisecondsToString(Config.Sequencer.Duration);
			UpdatePlayTime();
		}
		public void OnMidiKeybindChanged(object sender, KeybindChangedEventArgs e) {
			if (!loaded)
				return;
			
			string name = "";
			if (e.New != Keybind.None) {
				if (e.New == Config.Keybinds.Play)
					name = "Play Midi";
				else if (e.New == Config.Keybinds.Pause)
					name = "Pause Midi";
				else if (e.New == Config.Keybinds.Stop)
					name = "Stop Midi";
				else if (e.New == Config.Keybinds.Close)
					name = "Close Window";
				else if (e.New == Config.Keybinds.Mount)
					name = "Toggle Mount";
				else {
					for (int i = 0; i < Config.MidiCount; i++) {
						if (i != Config.MidiIndex && e.New == Config.Midis[i].Keybind) {
							name = Config.Midis[i].ProperName;
							break;
						}
					}
				}
			}
			if (name == "") {
				// The name can safely be changed
				Config.Midi.Keybind = e.New;
			}
			else {
				// Nag the user about poor life choices
				TriggerMessageBox.Show(this, MessageIcon.Error, "Keybind is already in use by the '" + name + "' keybind!", "Keybind in Use");
				keybindReaderMidi.Keybind = e.Previous;
			}
		}

		#endregion
		//--------------------------------
		#endregion
	}
}
