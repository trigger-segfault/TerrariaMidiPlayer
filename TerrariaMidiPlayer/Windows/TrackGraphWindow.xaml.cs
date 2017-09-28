using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
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
	/// <summary>
	/// Interaction logic for TrackGraphWindow.xaml
	/// </summary>
	public partial class TrackGraphWindow : Window {
		//=========== MEMBERS ============
		#region Members

		/**<summary>The saved width of the window.</summary>*/
		private static double InitialWidth = 0;
		/**<summary>The saved height of the window.</summary>*/
		private static double InitialHeight = 0;

		/**<summary>The current track index.</summary>*/
		int trackIndex;
		/**<summary>True if the window is loaded.</summary>*/
		bool loaded = false;
		/**<summary>The playback UI update timer.</summary>*/
		Timer playbackUITimer = new Timer(100);
		/**<summary>The old piano mode before opening the window.</summary>*/
		bool oldPianoMode;
		/**<summary>True if changing the song position.</summary>*/
		bool dragging = false;

		#endregion
		//========= CONSTRUCTORS =========
		#region Constructors

		/**<summary>Constructs the track graph window.</summary>*/
		public TrackGraphWindow(Midi midi, int trackIndex) {
			InitializeComponent();

			playbackUITimer.Elapsed += OnPlaybackUIUpdate;
			playbackUITimer.AutoReset = true;
			if (!DesignerProperties.GetIsInDesignMode(this)) {
				playbackUITimer.Start();
				oldPianoMode = Config.PianoMode;
				Config.PianoMode = true;
				toggleButtonPiano.IsChecked = true;
				toggleButtonWrapPianoMode.IsChecked = Config.WrapPianoMode;
				toggleButtonSkipPianoMode.IsChecked = Config.SkipPianoMode;
			}

			ListBoxItem item = new ListBoxItem();
			item.Content = "All Tracks";
			item.FontWeight = FontWeights.Bold;
			listTracks.Items.Add(item);
			for (int i = 0; i < Config.Midi.TrackCount; i++) {
				item = new ListBoxItem();
				item.Content = Config.Midi.GetTrackSettingsAt(i).ProperName;
				if (!Config.Midi.GetTrackSettingsAt(i).Enabled)
					item.Foreground = Brushes.Gray;
				listTracks.Items.Add(item);
			}
			this.trackIndex = trackIndex;

			this.listTracks.SelectedIndex = trackIndex + 1;

			loaded = true;

			Width = InitialWidth;
			Height = InitialHeight;
			Title += " - " + midi.ProperName;
		}

		#endregion
		//============ EVENTS ============
		#region Events
		//--------------------------------
		#region Window

		private void OnWindowClosing(object sender, CancelEventArgs e) {
			InitialWidth = Width;
			InitialHeight = Height;
			Config.PianoMode = oldPianoMode;
			if (!Config.PianoMode) {
				Config.MainWindow.StopOrPause();
			}
		}
		private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e) {
			if (!dragging)
				OnPlaybackUIUpdate(null, null);
		}
		private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e) {
			// Make text boxes lose focus on click away
			FocusManager.SetFocusedElement(this, this);
		}

		#endregion
		//--------------------------------
		#region View

		private void OnValidChecked(object sender, RoutedEventArgs e) {
			trackGraph.ShowValidNotes = checkBoxValid.IsChecked.Value;
			trackGraph.Update();
		}
		private void OnWrappedChecked(object sender, RoutedEventArgs e) {
			trackGraph.ShowWrappedNotes = checkBoxWrapped.IsChecked.Value;
			trackGraph.Update();
		}
		private void OnSkippedChecked(object sender, RoutedEventArgs e) {
			trackGraph.ShowSkippedNotes = checkBoxSkipped.IsChecked.Value;
			trackGraph.Update();
		}

		#endregion
		//--------------------------------
		#region Playback Position

		private void OnLeftMouseDown(object sender, MouseButtonEventArgs e) {
			dragging = true;
			OnMouseMove(sender, e);
			playArea.CaptureMouse();
		}
		private void OnLeftMouseUp(object sender, MouseButtonEventArgs e) {
			dragging = false;
			playArea.ReleaseMouseCapture();
		}
		private void OnMouseMove(object sender, MouseEventArgs e) {
			if (dragging) {
				double width = playArea.ActualWidth - 2;
				double x = Math.Max(0, Math.Min(width, e.GetPosition(playArea).X));
				double progress = x / width;
				playMarker.Margin = new Thickness(x, 0, 0, 0);
				Config.Sequencer.Position = Config.Sequencer.ProgressToTicks(progress);
				UpdatePlayButtons();
			}
		}

		#endregion
		//--------------------------------
		#region Midi/Track Settings

		private void OnTrackChanged(object sender, SelectionChangedEventArgs e) {
			if (listTracks.SelectedIndex != -1) {
				trackIndex = listTracks.SelectedIndex - 1;
				trackGraph.LoadTrack(trackIndex);
				UpdateTrack();
			}
		}
		private void OnTrackEnabledClicked(object sender, RoutedEventArgs e) {
			if (!loaded)
				return;
			loaded = false;

			Config.Midi.GetTrackSettingsAt(trackIndex).Enabled = checkBoxTrackEnabled.IsChecked.Value;

			listTracks.Items.RemoveAt(trackIndex + 1);
			ListBoxItem item = new ListBoxItem();
			item.Content = Config.Midi.GetTrackSettingsAt(trackIndex).ProperName;
			if (!Config.Midi.GetTrackSettingsAt(trackIndex).Enabled)
				item.Foreground = Brushes.Gray;
			listTracks.Items.Insert(trackIndex + 1, item);
			listTracks.SelectedIndex = trackIndex + 1;

			loaded = true;
			trackGraph.Update();
		}
		private void OnOctaveOffsetChanged(object sender, ValueChangedEventArgs<int> e) {
			if (!loaded)
				return;
			Config.Midi.GetTrackSettingsAt(trackIndex).OctaveOffset = numericOctaveOffset.Value;
			UpdateTrackNotes();
		}
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
			trackGraph.ReloadTrack();
			trackGraph.Update();
			UpdatePlayTime();
		}

		#endregion
		//--------------------------------
		#region Playback

		private void OnPlaybackUIUpdate(object sender, ElapsedEventArgs e) {
			Dispatcher.Invoke(() => {
				if (!dragging) {
					double progress = (double)Config.Sequencer.TicksToMilliseconds(Config.Sequencer.Position) / Config.Sequencer.Duration;
					double x = progress * (playArea.ActualWidth - 2);
					playMarker.Margin = new Thickness(x, 0, 0, 0);
				}
				UpdatePlayButtons();
				UpdatePlayTime();
			});
		}
		private void OnWrapPianoMode(object sender, RoutedEventArgs e) {
			if (!loaded)
				return;
			Config.WrapPianoMode = toggleButtonWrapPianoMode.IsChecked.Value;
		}
		private void OnSkipPianoMode(object sender, RoutedEventArgs e) {
			if (!loaded)
				return;
			Config.SkipPianoMode = toggleButtonSkipPianoMode.IsChecked.Value;
		}
		private void OnPianoToggled(object sender, RoutedEventArgs e) {
			if (!loaded)
				return;
			Config.MainWindow.StopOrPause();
			Config.PianoMode = toggleButtonPiano.IsChecked.Value;
			UpdatePlayButtons();
		}
		private void OnStopToggled(object sender, RoutedEventArgs e) {
			Config.MainWindow.Stop();
			UpdatePlayButtons();
		}
		private void OnPlayToggled(object sender, RoutedEventArgs e) {
			Config.MainWindow.Play();
			UpdatePlayButtons();
		}
		private void OnPauseToggled(object sender, RoutedEventArgs e) {
			Config.MainWindow.Pause();
			UpdatePlayButtons();
		}

		#endregion
		//--------------------------------
		#endregion
		//=========== UPDATING ===========
		#region Updating

		/**<summary>Updates changes to the track.</summary>*/
		private void UpdateTrack() {
			trackGraph.Update();
			checkBoxTrackEnabled.IsEnabled = trackIndex != -1;
			numericOctaveOffset.IsEnabled = trackIndex != -1;
			numericSpeed.Value = Config.Midi.Speed;
			numericNoteOffset.Value = Config.Midi.NoteOffset;
			labelChords.Content = "Chords: " + trackGraph.Chords;
			labelDuration.Content = "Duration: " + MillisecondsToString(Config.Sequencer.Duration);
			labelNotes.Content = "Notes: " + trackGraph.TotalNotes;
			if (trackIndex != -1) {
				numericOctaveOffset.Value = Config.Midi.GetTrackSettingsAt(trackIndex).OctaveOffset;
				checkBoxTrackEnabled.IsChecked = Config.Midi.GetTrackSettingsAt(trackIndex).Enabled;
			}
			checkBoxValid.ToolTip = trackGraph.ValidNotes + " Notes";
			checkBoxWrapped.ToolTip = trackGraph.WrappedNotes + " Notes";
			checkBoxSkipped.ToolTip = trackGraph.SkippedNotes + " Notes";
			UpdateTrackNotes();
		}
		/**<summary>Updates changes to the midi track's note range.</summary>*/
		private void UpdateTrackNotes() {
			if (trackIndex == -1) {
				int highestNote = 0;
				int lowestNote = 132;
				for (int index = 0; index < Config.Midi.TrackCount; index++) {
					if (!Config.Midi.GetTrackSettingsAt(index).Enabled)
						continue;
					highestNote = Math.Max(highestNote, Config.Midi.GetTrackAt(index).HighestNote);
					lowestNote = Math.Min(lowestNote, Config.Midi.GetTrackAt(index).LowestNote);
				}
				if (lowestNote > highestNote) {
					lowestNote = 12;
					highestNote = 12;
				}
				labelHighestNote.Content = "Highest Note: " + NoteToString(
					highestNote + Config.Midi.NoteOffset
				);
				labelLowestNote.Content = "Lowest Note: " + NoteToString(
					lowestNote + Config.Midi.NoteOffset
				);
			}
			else {
				labelHighestNote.Content = "Highest Note: " + NoteToString(
					Config.Midi.GetTrackAt(trackIndex).HighestNote +
					Config.Midi.NoteOffset
				);
				labelLowestNote.Content = "Lowest Note: " + NoteToString(
					Config.Midi.GetTrackAt(trackIndex).LowestNote +
					Config.Midi.NoteOffset
				);
			}
		}
		/**<summary>Updates the play time in the playback tab.</summary>*/
		private void UpdatePlayTime() {
			labelMidiPosition.Content = MillisecondsToString(Config.Sequencer.CurrentTime) + "/" + MillisecondsToString(Config.Sequencer.Duration);
		}
		/**<summary>Updates enabled state of midi buttons.</summary>*/
		private void UpdatePlayButtons() {
			toggleButtonStop.IsChecked = Config.Sequencer.Position <= 1 && !Config.Sequencer.IsPlaying;
			toggleButtonPlay.IsChecked = Config.Sequencer.IsPlaying;
			toggleButtonPause.IsChecked = Config.Sequencer.Position > 1 && !Config.Sequencer.IsPlaying;
		}

		#endregion
		//=========== HELPERS ============
		#region Helpers

		/**<summary>Gets the name of the specified note.</summary>*/
		private string NoteToString(int note) {
			string[] notes = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
			string[] flatNotes = { "", "D\u266D", "", "E\u266D", "", "", "G\u266D", "", "A\u266D", "", "B\u266D", "" };
			int semitone = note % 12;
			note -= 12;
			string noteStr = notes[semitone] + (note / 12);
			if (flatNotes[semitone].Length > 0)
				noteStr += " (" + flatNotes[semitone] + (note / 12) + ")";
			return noteStr;
		}
		/**<summary>Converts milliseconds to a string.</summary>*/
		private string MillisecondsToString(int milliseconds, bool showHours = false, bool showMilliseconds = false) {
			int ms = milliseconds % 1000;
			int seconds = (milliseconds / 1000) % 60;
			int minutes = (milliseconds / 1000 / 60);
			int hours = (milliseconds / 1000 / 60 / 60);
			if (showHours)
				minutes %= 60;

			string timeStr = "";
			if (showHours) {
				timeStr += hours.ToString() + ":";
				if (minutes < 10)
					timeStr += "0";
			}
			timeStr += minutes.ToString() + ":";
			if (seconds < 10)
				timeStr += "0";
			timeStr += seconds.ToString();
			if (showMilliseconds) {
				timeStr += "." + ms.ToString();
			}
			return timeStr;
		}

		#endregion
		//=========== SHOWING ============
		#region Showing

		/**<summary>Shows the track graph window.</summary>*/
		public static void Show(Window owner, Midi midi, int trackIndex) {
			TrackGraphWindow window = new TrackGraphWindow(midi, trackIndex);
			window.Owner = owner;
			window.ShowDialog();
		}

		#endregion
	}
}
