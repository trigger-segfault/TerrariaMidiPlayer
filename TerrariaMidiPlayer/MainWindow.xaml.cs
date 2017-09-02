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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Timers;
using Gma.System.MouseKeyHook;
using System.Threading;
using Timer = System.Timers.Timer;
using Keys = System.Windows.Forms.Keys;
using Sanford.Multimedia.Midi;
using Microsoft.Win32;
using System.Xml;
using System.Reflection;
using System.Globalization;
using TerrariaMidiPlayer.Syncing;
using System.Net;
using System.Text.RegularExpressions;
using System.Net.NetworkInformation;
using TerrariaMidiPlayer.Util;
using TerrariaMidiPlayer.Windows;
using TerrariaMidiPlayer.Controls;
using System.Runtime.InteropServices;

namespace TerrariaMidiPlayer {
	/**<summary>The main window running Terraria Midi Player.</summary>*/
	public partial class MainWindow : Window {
		//=========== MEMBERS ============
		#region Members

		Stopwatch watch = new Stopwatch();
		IKeyboardMouseEvents globalHook = Hook.GlobalEvents();
		Random rand = new Random();
		Timer playbackUITimer = new Timer(100);

		Sequencer sequencer = new Sequencer();

		Stopwatch noteWatch = new Stopwatch();

		Rect clientArea = new Rect(0, 0, 0, 0);
		
		bool mounted = false;

		int checkCount = 0;

		bool firstNote = true;
		
		bool loaded = false;
		private DateTime syncTime;
		private long syncTickCount;
		int trackIndex = -1;

		#endregion
		//========= CONSTRUCTORS =========
		#region Constructors

		/**<summary>Constructs the main window.</summary>*/
		public MainWindow() {
			InitializeComponent();
			
			globalHook.KeyDown += OnGlobalKeyDown;
			
			sequencer.ChannelMessagePlayed += OnChannelMessagePlayed;
			sequencer.PlayingCompleted += OnPlayingCompleted;

			playbackUITimer.Elapsed += OnPlaybackUIUpdate;

			// Init mount combo box
			foreach (Mount mount in Mount.Mounts) {
				comboBoxMount.Items.Add(mount.Name);
			}
			comboBoxMount.SelectedIndex = 0;

			// Init Syncing
			InitializeHost();
			InitializeClient();

			// Load config
			Config.Initialize(this);
			if (Config.ConfigExists())
				LoadConfig();
			else
				SaveConfig(false);

			UpdateConfig();

			/* TODO:
			 * Code Refactor
			 * Split MainWindow into multiple classes
			 * Pressing Enter to talk mutes Mount key (Setting)
			 * Touhou Midis/Initial D Midis
			 */


			// Make numeric up down tab focus properly
			FocusableProperty.OverrideMetadata(typeof(IntSpinner), new FrameworkPropertyMetadata(false));
			KeyboardNavigation.TabNavigationProperty.OverrideMetadata(typeof(IntSpinner), new FrameworkPropertyMetadata(KeyboardNavigationMode.Local));
		}

		#endregion
		//============ CONFIG ============
		#region Config

		/**<summary>Loads the config file.</summary>*/
		private bool LoadConfig() {
			if (!Config.Load()) {
				MessageBoxResult result = TriggerMessageBox.Show(this, MessageIcon.Error, "Error while trying to load config. Would you like to see the error?", "Load Error", MessageBoxButton.YesNo);
				if (result == MessageBoxResult.Yes)
					ErrorMessageBox.Show(Config.LastException);
				return false;
			}
			return true;
		}
		/**<summary>Saves the config file.</summary>*/
		private bool SaveConfig(bool silent) {
			if (!Config.Save()) {
				if (!silent) {
					MessageBoxResult result = TriggerMessageBox.Show(this, MessageIcon.Error, "Error while trying to save config. Would you like to see the error?", "Save Error", MessageBoxButton.YesNo);
					if (result == MessageBoxResult.Yes)
						ErrorMessageBox.Show(Config.LastException);
				}
				return false;
			}
			return true;
		}
		/**<summary>Updates controls after a config load.</summary>*/
		private void UpdateConfig() {
			loaded = false;

			#region Settings

			numericUseTime.Value = Config.UseTime;
			numericClickTime.Value = Config.ClickTime;
			numericChecks.Value = Config.CheckFrequency;
			checkBoxChecks.IsChecked = Config.ChecksEnabled;

			comboBoxMount.SelectedIndex = Config.MountIndex;

			projectileControl.Angle = (int)Config.ProjectileAngle;
			projectileControl.Range = (int)Config.ProjectileRange;

			#endregion
			//--------------------------------
			#region Keybinds
			
			UpdateKeybindTooltips();

			#endregion
			//--------------------------------
			#region Syncing

			switch (Config.Syncing.SyncType) {
			case SyncTypes.Client:
				comboBoxSyncType.SelectedIndex = 0;
				gridSyncClient.Visibility = Visibility.Visible;
				gridSyncHost.Visibility = Visibility.Hidden;
				break;
			case SyncTypes.Host:
				comboBoxSyncType.SelectedIndex = 1;
				gridSyncClient.Visibility = Visibility.Hidden;
				gridSyncHost.Visibility = Visibility.Visible;
				break;
			}

			textBoxClientIP.Text = Config.Syncing.ClientIPAddress;
			textBoxClientUsername.Text = Config.Syncing.ClientUsername;
			textBoxClientPassword.Text = Config.Syncing.ClientPassword;
			numericClientPort.Value = Config.Syncing.ClientPort;
			numericClientTimeOffset.Value = Config.Syncing.ClientTimeOffset;

			textBoxHostPassword.Text = Config.Syncing.HostPassword;
			numericHostPort.Value = Config.Syncing.HostPort;
			numericHostWait.Value = Config.Syncing.HostWait;

			#endregion
			//--------------------------------
			#region Midis

			listMidis.Items.Clear();
			for (int i = 0; i < Config.MidiCount; i++) {
				listMidis.Items.Add(Config.Midis[i].ProperName);
				if (i == Config.MidiIndex)
					listMidis.SelectedIndex = i;
			}
			UpdateMidi();

			#endregion

			loaded = true;
		}

		#endregion
		//============ EVENTS ============
		#region Events

		private void OnWindowLoaded(object sender, RoutedEventArgs e) {
			loaded = true;
		}
		private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e) {
			globalHook.KeyDown -= OnGlobalKeyDown;
			globalHook.Dispose();
			globalHook = null;

			watch.Stop();
			sequencer.Stop();
			playbackUITimer.Stop();

			SaveConfig(true);

			// Disconnect the host or client
			if (server != null)
				server.Stop();
			if (client != null)
				client.Disconnect();
		}
		private void OnGlobalKeyDown(object sender, System.Windows.Forms.KeyEventArgs e) {
			if (!loaded || keybindReaderMidi.IsReading)
				return;

			if (Config.HasMidi) {
				if (Config.Keybinds.Play.IsDown(e) && (Config.PlaybackNoFocus || IsActive || TerrariaWindowLocator.CheckIfFocused())) {
					if (server != null)
						HostStartPlay();
					else
						Play();
				}
				else if (Config.Keybinds.Pause.IsDown(e) && (Config.PlaybackNoFocus || IsActive || TerrariaWindowLocator.CheckIfFocused())) {
					if (server != null)
						HostStop();
					else
						Pause();
				}
				else if (Config.Keybinds.Stop.IsDown(e)) {
					if (server != null)
						HostStop();
					else
						Stop();
				}
			}
			if (Config.Keybinds.Mount.IsDown(e) && TerrariaWindowLocator.CheckIfFocused()) {
				mounted = !mounted;
				checkBoxMounted.IsChecked = mounted;
			}
			for (int i = 0; i < Config.Midis.Count; i++) {
				if (Config.Midis[i].Keybind.IsDown(e) && (Config.PlaybackNoFocus || IsActive || TerrariaWindowLocator.CheckIfFocused())) {
					Stop();

					loaded = false;
					Config.MidiIndex = i;
					listMidis.SelectedIndex = i;
					loaded = true;
					UpdateMidi();
				}
			}
			if (Config.Keybinds.Close.IsDown(e) && (Config.CloseNoFocus || IsActive || TerrariaWindowLocator.CheckIfFocused())) {
				Close();
			}
		}
		private void OnPlaybackUIUpdate(object sender, ElapsedEventArgs e) {
			UpdatePlayTime();
		}
		private void OnSyncTypeChanged(object sender, SelectionChangedEventArgs e) {
			if (!loaded)
				return;

			switch (comboBoxSyncType.SelectedIndex) {
			case 0:
				Config.Syncing.SyncType = SyncTypes.Client;
				gridSyncClient.Visibility = Visibility.Visible;
				gridSyncHost.Visibility = Visibility.Hidden;
				break;
			case 1:
				Config.Syncing.SyncType = SyncTypes.Host;
				gridSyncClient.Visibility = Visibility.Hidden;
				gridSyncHost.Visibility = Visibility.Visible;
				break;
			}
		}

		#endregion
		//=========== UPDATING ===========
		#region Updating

		/**<summary>Updates changes to the selected midi.</summary>*/
		public void UpdateMidi() {
			loaded = false;
			listTracks.Items.Clear();
			if (Config.HasMidi) {
				sequencer.Sequence = Config.Midi.Sequence;
				sequencer.Speed = Config.Midi.SpeedRatio;

				labelTotalNotes.Content = "Total Notes: " + Config.Midi.TotalNotes;
				labelDuration.Content = "Duration: " + MillisecondsToString(sequencer.Duration);
				keybindReaderMidi.Keybind = Config.Midi.Keybind;
				numericNoteOffset.IsEnabled = true;
				numericSpeed.IsEnabled = true;
				numericNoteOffset.Value = Config.Midi.NoteOffset;
				numericSpeed.Value = Config.Midi.Speed;
				keybindReaderMidi.IsEnabled = true;
				if (Config.Midi.TrackCount > 0) {
					for (int i = 0; i < Config.Midi.TrackCount; i++) {
						ListBoxItem item = new ListBoxItem();
						item.Content = Config.Midi.GetTrackSettingsAt(i).ProperName;
						if (!Config.Midi.GetTrackSettingsAt(i).Enabled)
							item.Foreground = Brushes.Gray;
						listTracks.Items.Add(item);
					}
					trackIndex = 0;
				}
				else {
					trackIndex = -1;
				}
				buttonEditTrackName.IsEnabled = (trackIndex != -1);
				listTracks.SelectedIndex = trackIndex;
				listTracks.IsEnabled = (Config.Midi.TrackCount > 0);

				labelDuration.Content = "Duration: " + MillisecondsToString(sequencer.Duration);
			}
			else {
				trackIndex = -1;
				labelTotalNotes.Content = "Total Notes: ";
				labelDuration.Content = "Duration: ";
				numericNoteOffset.IsEnabled = false;
				numericSpeed.IsEnabled = false;
				listTracks.IsEnabled = false;
				keybindReaderMidi.IsEnabled = false;
				buttonEditTrackName.IsEnabled = false;
			}
			loaded = true;
			UpdateTrack();
			UpdateMidiButtons();
			UpdatePlayTime();
		}
		/**<summary>Updates changes to the selected midi track.</summary>*/
		private void UpdateTrack() {
			loaded = false;
			if (Config.HasMidi && Config.Midi.TrackCount > 0) {
				UpdateTrackNotes();
				labelNotes.Content = "Notes: " + Config.Midi.GetTrackAt(listTracks.SelectedIndex).Notes;
				checkBoxTrackEnabled.IsChecked = Config.Midi.GetTrackSettingsAt(listTracks.SelectedIndex).Enabled;
				numericOctaveOffset.Value = Config.Midi.GetTrackSettingsAt(listTracks.SelectedIndex).OctaveOffset;
				numericOctaveOffset.IsEnabled = true;
				checkBoxTrackEnabled.IsEnabled = true;
			}
			else {
				labelHighestNote.Content = "Highest Note: ";
				labelLowestNote.Content = "Lowest Note: ";
				labelNotes.Content = "Notes: ";
				numericOctaveOffset.IsEnabled = false;
				checkBoxTrackEnabled.IsEnabled = false;
			}
			loaded = true;
		}
		/**<summary>Updates changes to the midi track's note range.</summary>*/
		private void UpdateTrackNotes() {
			labelHighestNote.Content = "Highest Note: " + NoteToString(
				Config.Midi.GetTrackAt(trackIndex).HighestNote +
				Config.Midi.NoteOffset /*-
				Config.Midi.GetTrackSettingsAt(trackIndex).OctaveOffset * 12*/
			);
			labelLowestNote.Content = "Lowest Note: " + NoteToString(
				Config.Midi.GetTrackAt(trackIndex).LowestNote +
				Config.Midi.NoteOffset /*-
				Config.Midi.GetTrackSettingsAt(trackIndex).OctaveOffset * 12*/
			);
		}
		/**<summary>Updates enabled state of midi buttons.</summary>*/
		private void UpdateMidiButtons() {
			buttonRemoveMidi.IsEnabled = (listMidis.SelectedIndex != -1);
			buttonEditMidiName.IsEnabled = (listMidis.SelectedIndex != -1);
			buttonMoveMidiUp.IsEnabled = (listMidis.SelectedIndex > 0);
			buttonMoveMidiDown.IsEnabled = (listMidis.SelectedIndex != -1 && listMidis.SelectedIndex + 1 < listMidis.Items.Count);

			toggleButtonStop.IsEnabled = (listMidis.SelectedIndex != -1);
			toggleButtonPlay.IsEnabled = (listMidis.SelectedIndex != -1);
			toggleButtonPause.IsEnabled = (listMidis.SelectedIndex != -1);
			sliderMidiPosition.IsEnabled = (listMidis.SelectedIndex != -1);
			loaded = false;
			sliderMidiPosition.Value = 0;
			loaded = true;
		}
		/**<summary>Updates the play time in the playback tab.</summary>*/
		private void UpdatePlayTime() {
			Dispatcher.Invoke(() => {
				loaded = false;
				double currentProgress = sequencer.CurrentProgress;
				sliderMidiPosition.Value = (double.IsNaN(currentProgress) ? 0 : currentProgress);
				if (Config.MidiIndex != -1)
					labelMidiPosition.Content = MillisecondsToString(sequencer.CurrentTime) + "/" + MillisecondsToString(sequencer.Duration);
				else
					labelMidiPosition.Content = "-:--/-:--";
				loaded = true;
			});
		}
		/**<summary>Updates the keybind tooltips.</summary>*/
		private void UpdateKeybindTooltips() {
			toggleButtonStop.ToolTip = "Stop midi playback. <";
			toggleButtonPlay.ToolTip = "Start midi playback. <";
			toggleButtonPause.ToolTip = "Pause midi playback. <";
			checkBoxMounted.ToolTip = "Needed to calculate the center of the player. <";
			// Yes I know stop can't "officially" be unassigned.
			toggleButtonStop.ToolTip += (Config.Keybinds.Stop == Keybind.None ? "No Keybind" : Config.Keybinds.Stop.ToProperString()) + ">";
			toggleButtonPlay.ToolTip += (Config.Keybinds.Play == Keybind.None ? "No Keybind" : Config.Keybinds.Play.ToProperString()) + ">";
			toggleButtonPause.ToolTip += (Config.Keybinds.Pause == Keybind.None ? "No Keybind" : Config.Keybinds.Pause.ToProperString()) + ">";
			checkBoxMounted.ToolTip += (Config.Keybinds.Pause == Keybind.None ? "No Keybind" : Config.Keybinds.Mount.ToProperString()) + ">";
			menuItemExit.InputGestureText = Config.Keybinds.Close.ToProperString();
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
		/**<summary>Calculates the current time used for syncing.</summary>*/
		private DateTime CalculateSyncDateTime() {
			long ticks = unchecked((uint)Environment.TickCount) - syncTickCount;
			return syncTime.AddMilliseconds(ticks);
		}

		#endregion
	}
}
