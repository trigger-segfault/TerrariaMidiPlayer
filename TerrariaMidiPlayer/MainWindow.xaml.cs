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

	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {
		public struct Mount {
			public string Name;
			public int Offset;

			public Mount(string name, int offset) {
				Name = name;
				Offset = offset;
			}
		}

		//https://stackoverflow.com/questions/31020626/how-to-detect-if-keyboard-has-numeric-block
		[DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true, CallingConvention = CallingConvention.Winapi)]
		private static extern short GetKeyState(int keyCode);

		private bool KeyboardHasNumpad {
			get { return (((ushort)GetKeyState(0x90)) & 0xffff) != 0; }
		}

		Stopwatch watch = new Stopwatch();
		IKeyboardMouseEvents globalHook;
		Random rand = new Random();
		Timer uiUpdateTimer = new Timer(100);

		Midi midi = null;
		Sequencer sequencer;

		Rect clientArea = new Rect(0, 0, 0, 0);
		
		Keybind keybindPlay = new Keybind(Key.NumPad0);
		Keybind keybindPause = new Keybind(Key.NumPad1);
		Keybind keybindStop = new Keybind(Key.NumPad2);
		Keybind keybindClose = new Keybind(Key.Add);
		Keybind keybindMount = new Keybind(Key.R);
		bool closeNoFocus = false;
		bool playbackNoFocus = false;
		
		int useTime = 11;
		int clickTime = 40;
		bool checksEnabled = true;
		int checkFrequency = 20;
		bool mounted = false;
		int mount = 0;

		double projectileAngle = 0;
		double projectileRange = 360;

		int checkCount = 0;
		bool closing = false;

		bool firstNote = true;
		
		static readonly Mount[] Mounts = {
			new Mount("No Mount", 0),
			new Mount("Bunny", 17),
			new Mount("Slime", 15),
			new Mount("Bee", 12),
			new Mount("Turtle", 20),
			new Mount("Basilisk", 6),
			new Mount("Unicorn", 27),
			new Mount("Reindeer", 13),
			new Mount("Pigron", 15),
			new Mount("Fishron", 11),
			new Mount("Scutlix", 12),
			new Mount("UFO", 12)
		};

		List<Midi> midis = new List<Midi>();
		bool loaded = false;

		public MainWindow() {
			
			InitializeComponent();
			loaded = false;
			rand = new Random();
			globalHook = Hook.GlobalEvents();
			watch = new Stopwatch();
			globalHook.KeyDown += OnGlobalKeyDown;

			midis = new List<Midi>();

			sequencer = new Sequencer();
			sequencer.ChannelMessagePlayed += OnChannelMessagePlayed;
			sequencer.PlayingCompleted += OnPlayingCompleted;
			clientArea = new Rect();

			projectileAngle = 0;
			projectileRange = 360;

			uiUpdateTimer.Elapsed += OnPlaybackUIUpdate;

			useTime = 11;
			checksEnabled = true;
			checkFrequency = 20;
			checkCount = 0;
			clickTime = 40;

			mount = 0;

			for (int i = 0; i < Mounts.Length; i++) {
				comboBoxMount.Items.Add(Mounts[i].Name);
			}
			comboBoxMount.SelectedIndex = 0;

			InitHost();
			InitClient();

			if (!KeyboardHasNumpad) {
				keybindPlay = new Keybind(Key.Delete);
				keybindPause = new Keybind(Key.End);
				keybindStop = new Keybind(Key.PageDown);
				keybindClose = new Keybind(Key.PageUp);
			}

			LoadConfig();

			UpdateMidi();
			UpdateKeybindTooltips();

			/* TODO:
			 * Code Refactor
			 * Split MainWindow into multiple classes
			 * Pressing Enter to talk mutes Mount key (Setting)
			 * Touhou Midis/Initial D Midis
			 */


			// Make numeric up down tab focus properly
			FocusableProperty.OverrideMetadata(typeof(NumericUpDown), new FrameworkPropertyMetadata(false));
			KeyboardNavigation.TabNavigationProperty.OverrideMetadata(typeof(NumericUpDown), new FrameworkPropertyMetadata(KeyboardNavigationMode.Local));
		}

		#region Quick n' Easy Propertiesies
		public static string ApplicationDirectory {
			get { return System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location); }
		}
		public static string ConfigPath {
			get { return System.IO.Path.Combine(ApplicationDirectory, "Config.xml"); }
		}
		#endregion

		#region Config
		private bool LoadConfig() {
			if (System.IO.File.Exists(ConfigPath)) {
				try {
					XmlNode node;
					XmlElement element;
					XmlAttribute attribute;
					XmlDocument doc = new XmlDocument();
					doc.Load(ConfigPath);

					int version = 0;
					node = doc.SelectSingleNode("/TerrariaMidiPlayer/Version");
					if (node != null && !int.TryParse(node.InnerText, out version))
						return false;

					#region Settings
					node = doc.SelectSingleNode("/TerrariaMidiPlayer/Settings/ExecutableName");
					if (node != null)
						TerrariaWindowLocator.ExeName = node.InnerText;

					node = doc.SelectSingleNode("/TerrariaMidiPlayer/Settings/UseTime");
					if (node != null && !int.TryParse(node.InnerText, out useTime))
						useTime = 11;
					numericUseTime.Value = useTime;

					node = doc.SelectSingleNode("/TerrariaMidiPlayer/Settings/ClickTime");
					if (node != null && !int.TryParse(node.InnerText, out clickTime))
						clickTime = 40;
					numericClickTime.Value = clickTime;

					node = doc.SelectSingleNode("/TerrariaMidiPlayer/Settings/ChecksEnabled");
					if (node != null && !bool.TryParse(node.InnerText, out checksEnabled))
						checksEnabled = true;
					checkBoxChecks.IsChecked = checksEnabled;
					numericChecks.IsEnabled = checksEnabled;

					node = doc.SelectSingleNode("/TerrariaMidiPlayer/Settings/CheckFrequency");
					if (node != null && !int.TryParse(node.InnerText, out checkFrequency))
						checkFrequency = 20;
					numericChecks.Value = checkFrequency;

					node = doc.SelectSingleNode("/TerrariaMidiPlayer/Settings/Mount");
					if (node != null) {
						for (int i = 0; i < Mounts.Length; i++) {
							if (string.Compare(node.InnerText, Mounts[i].Name, true) == 0) {
								mount = i;
								comboBoxMount.SelectedIndex = i;
								break;
							}
						}
					}

					node = doc.SelectSingleNode("/TerrariaMidiPlayer/Settings/ProjectileAngle");
					if (node != null && !double.TryParse(node.InnerText, out projectileAngle))
						projectileAngle = 0;
					projectileControl.Angle = (int)projectileAngle;

					node = doc.SelectSingleNode("/TerrariaMidiPlayer/Settings/ProjectileRange");
					if (node != null && !double.TryParse(node.InnerText, out projectileRange))
						projectileRange = 360;
					projectileControl.Range = (int)projectileRange;

					node = doc.SelectSingleNode("/TerrariaMidiPlayer/Settings/CloseNoFocus");
					if (node != null && !bool.TryParse(node.InnerText, out closeNoFocus))
						closeNoFocus = false;

					node = doc.SelectSingleNode("/TerrariaMidiPlayer/Settings/PlaybackNoFocus");
					if (node != null && !bool.TryParse(node.InnerText, out playbackNoFocus))
						playbackNoFocus = false;

					#region Keybinds
					Keybind keybind;
					node = doc.SelectSingleNode("/TerrariaMidiPlayer/Settings/Keybinds/Play");
					if (node != null && Keybind.TryParse(node.InnerText, out keybind))
						keybindPlay = keybind;

					node = doc.SelectSingleNode("/TerrariaMidiPlayer/Settings/Keybinds/Pause");
					if (node != null && Keybind.TryParse(node.InnerText, out keybind))
						keybindPause = keybind;

					node = doc.SelectSingleNode("/TerrariaMidiPlayer/Settings/Keybinds/Stop");
					if (node != null && Keybind.TryParse(node.InnerText, out keybind))
						keybindStop = keybind;

					node = doc.SelectSingleNode("/TerrariaMidiPlayer/Settings/Keybinds/Close");
					if (node != null && Keybind.TryParse(node.InnerText, out keybind)) {
						keybindClose = keybind;
						menuItemExit.InputGestureText = keybindClose.ToCharString();
					}

					node = doc.SelectSingleNode("/TerrariaMidiPlayer/Settings/Keybinds/Mount");
					if (node != null && Keybind.TryParse(node.InnerText, out keybind))
						keybindMount = keybind;
					#endregion

					#region Syncing
					node = doc.SelectSingleNode("/TerrariaMidiPlayer/Settings/Syncing/SyncType");
					if (node != null && string.Compare(node.InnerText, "Host", true) == 0) {
						comboBoxSyncType.SelectedIndex = 1;
						gridSyncClient.Visibility = Visibility.Hidden;
						gridSyncHost.Visibility = Visibility.Visible;
					}


					node = doc.SelectSingleNode("/TerrariaMidiPlayer/Settings/Syncing/ClientIPAddress");
					if (node != null) textBoxClientIP.Text = node.InnerText;

					ushort port = 0;
					node = doc.SelectSingleNode("/TerrariaMidiPlayer/Settings/Syncing/ClientPort");
					if (node != null) ushort.TryParse(node.InnerText, out port);
					numericClientPort.Value = port;

					node = doc.SelectSingleNode("/TerrariaMidiPlayer/Settings/Syncing/ClientUsername");
					if (node != null) textBoxClientUsername.Text = node.InnerText;

					node = doc.SelectSingleNode("/TerrariaMidiPlayer/Settings/Syncing/ClientPassword");
					if (node != null) textBoxClientPassword.Text = node.InnerText;

					int timeOffset = 0;
					node = doc.SelectSingleNode("/TerrariaMidiPlayer/Settings/Syncing/ClientTimeOffset");
					if (node != null) int.TryParse(node.InnerText, out timeOffset);
					numericClientPlayOffset.Value = timeOffset;

					port = 0;
					node = doc.SelectSingleNode("/TerrariaMidiPlayer/Settings/Syncing/HostPort");
					if (node != null) ushort.TryParse(node.InnerText, out port);
					numericHostPort.Value = port;

					node = doc.SelectSingleNode("/TerrariaMidiPlayer/Settings/Syncing/HostPassword");
					if (node != null) textBoxHostPassword.Text = node.InnerText;

					timeOffset = 0;
					node = doc.SelectSingleNode("/TerrariaMidiPlayer/Settings/Syncing/HostWait");
					if (node != null && int.TryParse(node.InnerText, out timeOffset))
						numericHostWait.Value = timeOffset;
					#endregion
					#endregion

					#region Midis
					int selectedIndex = -1;
					bool selected;
					XmlNodeList midiList = doc.SelectNodes("/TerrariaMidiPlayer/Midis/Midi");
					for (int i = 0; i < midiList.Count; i++) {
						Midi midi = new Midi();

						if (midi.LoadConfig(midiList[i])) {
							if (midiList[i].Attributes["Selected"] != null && bool.TryParse(midiList[i].Attributes["Selected"].Value, out selected) && selected)
								selectedIndex = i;
							midis.Add(midi);
							listMidis.Items.Add(midi.ProperName);
						}
					}

					if (midis.Count > 0) {
						listMidis.SelectedIndex = Math.Max(0, selectedIndex);
						this.midi = midis[listMidis.SelectedIndex];
						this.sequencer.Sequence = midi.Sequence;
						sequencer.Speed = 100.0 / (double)midi.Speed;
					}
					#endregion
					return true;
				}
				catch (Exception ex) {
					MessageBoxResult result = TriggerMessageBox.Show(this, MessageIcon.Error, "Error while trying to load config. Would you like to see the error?", "Load Error", MessageBoxButton.YesNo);
					if (result == MessageBoxResult.Yes)
						ErrorMessageBox.Show(ex);
					return false;
				}
			}
			else {
				SaveConfig(false);
				return true;
			}
		}
		
		private bool SaveConfig(bool silent = false) {
			try {
				XmlElement element;
				XmlAttribute attribute;
				XmlDocument doc = new XmlDocument();
				doc.AppendChild(doc.CreateXmlDeclaration("1.0", "UTF-8", null));

				XmlElement midiPlayer = doc.CreateElement("TerrariaMidiPlayer");
				doc.AppendChild(midiPlayer);

				XmlElement version = doc.CreateElement("Version");
				version.AppendChild(doc.CreateTextNode("1"));
				midiPlayer.AppendChild(version);

				#region Settings
				XmlElement setting = doc.CreateElement("Settings");
				midiPlayer.AppendChild(setting);

				element = doc.CreateElement("ExecutableName");
				element.AppendChild(doc.CreateTextNode(TerrariaWindowLocator.ExeName));
				setting.AppendChild(element);

				element = doc.CreateElement("UseTime");
				element.AppendChild(doc.CreateTextNode(useTime.ToString()));
				setting.AppendChild(element);

				element = doc.CreateElement("ClickTime");
				element.AppendChild(doc.CreateTextNode(clickTime.ToString()));
				setting.AppendChild(element);

				element = doc.CreateElement("ChecksEnabled");
				element.AppendChild(doc.CreateTextNode(checksEnabled.ToString()));
				setting.AppendChild(element);

				element = doc.CreateElement("CheckFrequency");
				element.AppendChild(doc.CreateTextNode(checkFrequency.ToString()));
				setting.AppendChild(element);

				element = doc.CreateElement("Mount");
				element.AppendChild(doc.CreateTextNode(Mounts[mount].Name));
				setting.AppendChild(element);

				element = doc.CreateElement("ProjectileAngle");
				element.AppendChild(doc.CreateTextNode(projectileAngle.ToString()));
				setting.AppendChild(element);

				element = doc.CreateElement("ProjectileRange");
				element.AppendChild(doc.CreateTextNode(projectileRange.ToString()));
				setting.AppendChild(element);

				element = doc.CreateElement("CloseNoFocus");
				element.AppendChild(doc.CreateTextNode(closeNoFocus.ToString()));
				setting.AppendChild(element);

				element = doc.CreateElement("PlaybackNoFocus");
				element.AppendChild(doc.CreateTextNode(playbackNoFocus.ToString()));
				setting.AppendChild(element);

				#region Keybinds
				XmlElement keybinds = doc.CreateElement("Keybinds");
				setting.AppendChild(keybinds);

				element = doc.CreateElement("Play");
				element.AppendChild(doc.CreateTextNode(keybindPlay.ToString()));
				keybinds.AppendChild(element);

				element = doc.CreateElement("Pause");
				element.AppendChild(doc.CreateTextNode(keybindPause.ToString()));
				keybinds.AppendChild(element);

				element = doc.CreateElement("Stop");
				element.AppendChild(doc.CreateTextNode(keybindStop.ToString()));
				keybinds.AppendChild(element);

				element = doc.CreateElement("Close");
				element.AppendChild(doc.CreateTextNode(keybindClose.ToString()));
				keybinds.AppendChild(element);

				element = doc.CreateElement("Mount");
				element.AppendChild(doc.CreateTextNode(keybindMount.ToString()));
				keybinds.AppendChild(element);
				#endregion

				#region Syncing
				XmlElement syncing = doc.CreateElement("Syncing");
				setting.AppendChild(syncing);

				element = doc.CreateElement("SyncType");
				element.AppendChild(doc.CreateTextNode(comboBoxSyncType.SelectedIndex == 0 ? "Client" : "Host"));
				syncing.AppendChild(element);

				element = doc.CreateElement("ClientIPAddress");
				element.AppendChild(doc.CreateTextNode(textBoxClientIP.Text));
				syncing.AppendChild(element);

				element = doc.CreateElement("ClientPort");
				element.AppendChild(doc.CreateTextNode(numericClientPort.Value.ToString()));
				syncing.AppendChild(element);

				element = doc.CreateElement("ClientUsername");
				element.AppendChild(doc.CreateTextNode(textBoxClientUsername.Text));
				syncing.AppendChild(element);

				element = doc.CreateElement("ClientPassword");
				element.AppendChild(doc.CreateTextNode(textBoxClientPassword.Text));
				syncing.AppendChild(element);

				element = doc.CreateElement("ClientTimeOffset");
				element.AppendChild(doc.CreateTextNode(numericClientPlayOffset.Value.ToString()));
				syncing.AppendChild(element);

				element = doc.CreateElement("HostPort");
				element.AppendChild(doc.CreateTextNode(numericHostPort.Value.ToString()));
				syncing.AppendChild(element);

				element = doc.CreateElement("HostPassword");
				element.AppendChild(doc.CreateTextNode(textBoxHostPassword.Text));
				syncing.AppendChild(element);

				element = doc.CreateElement("HostWait");
				element.AppendChild(doc.CreateTextNode(numericHostWait.Value.ToString()));
				syncing.AppendChild(element);
				#endregion
				#endregion

				#region Midis
				XmlElement midis = doc.CreateElement("Midis");
				midiPlayer.AppendChild(midis);

				foreach (Midi midi in this.midis) {
					XmlElement midiElement = doc.CreateElement("Midi");
					midis.AppendChild(midiElement);

					if (midi == this.midi)
						midiElement.SetAttribute("Selected", true.ToString());

					element = doc.CreateElement("FilePath");
					element.AppendChild(doc.CreateTextNode(midi.Path));
					midiElement.AppendChild(element);

					if (midi.Name != "") {
						element = doc.CreateElement("Name");
						element.AppendChild(doc.CreateTextNode(midi.Name));
						midiElement.AppendChild(element);
					}
					if (midi.NoteOffset != 0) {
						element = doc.CreateElement("NoteOffset");
						element.AppendChild(doc.CreateTextNode(midi.NoteOffset.ToString()));
						midiElement.AppendChild(element);
					}
					if (midi.Speed != 100) {
						element = doc.CreateElement("Speed");
						element.AppendChild(doc.CreateTextNode(midi.Speed.ToString()));
						midiElement.AppendChild(element);
					}
					if (midi.Keybind != Keybind.None) {
						element = doc.CreateElement("Keybind");
						element.AppendChild(doc.CreateTextNode(midi.Keybind.ToString()));
						midiElement.AppendChild(element);
					}
					XmlElement tracks = doc.CreateElement("Tracks");
					midiElement.AppendChild(tracks);

					for (int i = 0; i < midi.TrackCount; i++) {
						Midi.TrackSettings trackSettings = midi.GetTrackSettings(i);
						XmlElement track = doc.CreateElement("Track");
						tracks.AppendChild(track);
						
						track.SetAttribute("Enabled", trackSettings.Enabled.ToString());
						track.SetAttribute("OctaveOffset", trackSettings.OctaveOffset.ToString());
					}
				}

				#endregion

				doc.Save(ConfigPath);
				return true;
			}
			catch (Exception ex) {
				if (!silent) {
					MessageBoxResult result = TriggerMessageBox.Show(this, MessageIcon.Error, "Error while trying to save config. Would you like to see the error?", "Save Error", MessageBoxButton.YesNo);
					if (result == MessageBoxResult.Yes)
						ErrorMessageBox.Show(ex);
				}
				return false;
			}
		}
		#endregion

		#region Window Events
		private void OnLoaded(object sender, RoutedEventArgs e) {
			loaded = true;
		}

		private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e) {
			closing = true;
			globalHook.KeyDown -= OnGlobalKeyDown;
			globalHook.Dispose();
			globalHook = null;
			watch.Stop();
			sequencer.Stop();
			SaveConfig(true);
			uiUpdateTimer.Stop();

			if (server != null)
				server.Stop();
			if (client != null)
				client.Disconnect();
		}

		private void OnGlobalKeyDown(object sender, System.Windows.Forms.KeyEventArgs e) {
			if (!loaded || keybindReaderMidi.IsReading)
				return;

			if (midi != null) {
				if (keybindPlay.IsDown(e) && (playbackNoFocus || IsActive || TerrariaWindowLocator.CheckIfFocused())) {
					if (server != null)
						HostStartPlay();
					else
						Play();
				}
				else if (keybindPause.IsDown(e) && (playbackNoFocus || IsActive || TerrariaWindowLocator.CheckIfFocused())) {
					if (server != null)
						HostStop();
					else
						Pause();
				}
				else if (keybindStop.IsDown(e)) {
					if (server != null)
						HostStop();
					else
						Stop();
				}
			}
			if (keybindMount.IsDown(e) && TerrariaWindowLocator.CheckIfFocused()) {
				mounted = !mounted;
				checkBoxMounted.IsChecked = mounted;
			}
			for (int i = 0; i < midis.Count; i++) {
				if (midis[i].Keybind.IsDown(e) && (playbackNoFocus || IsActive || TerrariaWindowLocator.CheckIfFocused())) {
					Stop();

					loaded = false;
					listMidis.SelectedIndex = i;
					loaded = true;
					midi = midis[listMidis.SelectedIndex];
					sequencer.Sequence = midi.Sequence;
					sequencer.Speed = 100.0 / (double)midi.Speed;

					UpdateMidi();
				}
			}
			if (keybindClose.IsDown(e) && (closeNoFocus || IsActive || TerrariaWindowLocator.CheckIfFocused())) {
				Close();
			}
		}
		#endregion

		#region Midi Playing
		private void OnPlayingCompleted(object sender, EventArgs e) {
			Stop();
		}

		private void OnChannelMessagePlayed(object sender, ChannelMessageEventArgs e) {
			if (midi.IsMessagePlayable(e) && (watch.ElapsedMilliseconds >= useTime * 1000 / 60 + 2 || firstNote)) {
				if (checksEnabled) {
					checkCount++;
					if (!TerrariaWindowLocator.Update(checkCount > checkFrequency)) {
						Stop();
						Dispatcher.Invoke(() => {
							TriggerMessageBox.Show(this, MessageIcon.Error, "Failed to keep track of the Terraria Window!", "Tracking Error");
						});
						return;
					}
					if (checkCount > checkFrequency)
						checkCount = 0;
					if (!TerrariaWindowLocator.HasFocus) {
						TerrariaWindowLocator.Focus();
						Thread.Sleep(100);
						return;
					}
					if (!TerrariaWindowLocator.IsOpen) {
						Pause();
						return;
					}
					clientArea = TerrariaWindowLocator.ClientArea;
				}
				firstNote = false;
				int note = e.Message.Data1 - 12 * (midi.GetTrackSettingsByTrackObj(e.Track).OctaveOffset + 1) + midi.NoteOffset;
				watch.Restart();
				PlaySemitone(note);
			}
		}

		private void PlaySemitone(int semitone) {
			double direction;
			double heightRatio = clientArea.Height / 48.0;
			while (semitone < 0)
				semitone += 12;
			while (semitone > 24)
				semitone -= 12;
			double centerx = clientArea.Width / 2;
			double centery = clientArea.Height / 2 - (mounted ? Mounts[mount].Offset : 0);
			int testY = (int)(centery - heightRatio * semitone);
			// The right & left boundary before losing notes go bad
			double maxAngle = (Math.Acos(centery / (heightRatio * semitone)) / Math.PI * 180) % 360;
			double minAngle = 360 - maxAngle;
			double rangeStart = ((projectileAngle - projectileRange / 2 + 360)) % 360;
			double rangeEnd = (projectileAngle + projectileRange / 2) % 360;
			// Fix mount offsets reducing vertical note range
			if (testY < 0 && ((rangeStart > minAngle || rangeStart < maxAngle) || 
				(rangeEnd > minAngle || rangeEnd < maxAngle) || (rangeStart > rangeEnd))) {
				double start1 = 0, start2 = 0;
				double stop1 = 0, stop2 = 0;
				if (rangeStart <= minAngle && rangeStart >= maxAngle) {
					start1 = rangeStart;
					stop1 = minAngle;
				}
				if (rangeEnd >= maxAngle) {
					start2 = maxAngle;
					stop2 = rangeEnd;
				}
				if (start1 == 0 && start2 == 0) {
					if (projectileAngle == 0)
						direction = (rand.Next() % 2 == 0 ? minAngle : maxAngle);
					else if (projectileAngle < 180)
						direction = maxAngle;
					else
						direction = minAngle;
				}
				else if (start2 == 0) {
					direction = start1 + rand.NextDouble() * (stop1 - start1);
				}
				else if (start1 == 0) {
					direction = start2 + rand.NextDouble() * (stop2 - start2);
				}
				else {
					double angle = rand.NextDouble() * ((stop1 - start1) + (stop2 - start2));
					if (angle >= (stop1 - start1))
						direction = start2 + (angle - (stop1 - start1));
					else
						direction = start1 + angle;
				}
				direction = (direction + 270) / 180 * Math.PI;
			}
			else {
				direction = (projectileAngle - projectileRange / 2 + rand.NextDouble() * projectileRange + 270) / 180 * Math.PI;
			}
			int x = (int)(centerx + Math.Cos(direction) * (heightRatio * semitone));
			int y = (int)(centery + Math.Sin(direction) * (heightRatio * semitone));
			if (x < 0) x = 0;
			if (x >= (int)clientArea.Width) x = (int)clientArea.Width - 1;
			if (y < 0) y = 0;
			if (y >= (int)clientArea.Height) y = (int)clientArea.Height - 1;
			x += (int)clientArea.X;
			y += (int)clientArea.Y;
			MouseControl.SimulateClick(x, y, clickTime);
		}
		#endregion

		#region Playback Tab Events
		private void OnUseTimeChanged(object sender, RoutedEventArgs e) {
			if (!loaded)
				return;

			useTime = numericUseTime.Value;
		}

		private void OnChecksChanged(object sender, RoutedEventArgs e) {
			checkFrequency = numericChecks.Value;
		}

		private void OnChecksEnabledClicked(object sender, RoutedEventArgs e) {
			if (!loaded)
				return;

			checksEnabled = checkBoxChecks.IsChecked.Value;
			numericChecks.IsEnabled = checksEnabled;
		}

		private void OnClickTimeChanged(object sender, RoutedEventArgs e) {
			if (!loaded)
				return;

			clickTime = numericClickTime.Value;
		}

		private void OnMountedChanged(object sender, RoutedEventArgs e) {
			if (!loaded)
				return;

			mounted = checkBoxMounted.IsChecked.Value;
		}

		private void OnMountChanged(object sender, SelectionChangedEventArgs e) {
			if (!loaded)
				return;

			mount = comboBoxMount.SelectedIndex;
		}

		private void OnProjectileChanged(object sender, RoutedEventArgs e) {
			projectileAngle = projectileControl.Angle;
			projectileRange = projectileControl.Range;
		}

		private void OnStopToggled(object sender, RoutedEventArgs e) {
			if (server != null)
				HostStop();
			else
				Stop();
		}

		private void OnPlayToggled(object sender, RoutedEventArgs e) {
			if (server != null)
				HostStartPlay();
			else
				Play();
		}

		private void OnPauseToggled(object sender, RoutedEventArgs e) {
			if (server != null)
				HostStop();
			else
				Pause(); 
		}

		private void OnMidiPositionChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
			if (!loaded)
				return;

			sequencer.Position = sequencer.ProgressToTicks(e.NewValue);
			labelMidiPosition.Content = MillisecondsToString(sequencer.CurrentTime) + "/" + MillisecondsToString(sequencer.Duration);
			toggleButtonStop.IsChecked = (e.NewValue == 0);
			toggleButtonPlay.IsChecked = false;
			toggleButtonPause.IsChecked = (e.NewValue != 0);
		}
		#endregion

		#region Midi Playing
		private void Play() {
			if (midi != null) {
				firstNote = true;
				TerrariaWindowLocator.Update(true);
				if (!TerrariaWindowLocator.HasFocus) {
					TerrariaWindowLocator.Focus();
					Thread.Sleep(400);
				}
				if (TerrariaWindowLocator.IsOpen) {
					clientArea = TerrariaWindowLocator.ClientArea;
					watch.Restart();
					if (sequencer.Position <= 1)
						sequencer.Start();
					else
						sequencer.Continue();
					checkCount = 0;
					Dispatcher.Invoke(() => {
						toggleButtonStop.IsChecked = false;
						toggleButtonPlay.IsChecked = true;
						toggleButtonPause.IsChecked = false;
						uiUpdateTimer.Start();
					});
				}
				else {
					Dispatcher.Invoke(() => {
						toggleButtonPlay.IsChecked = false;
						TriggerMessageBox.Show(this, MessageIcon.Warning, "You cannot play a midi when Terraria isn't running! Have you specified the correct executable name in Options?", "Terraria not Running");
					});
				}
			}
		}

		private void Pause() {
			sequencer.Stop();
			if (midi != null) {
				Dispatcher.Invoke(() => {
					toggleButtonStop.IsChecked = false;
					toggleButtonPlay.IsChecked = false;
					toggleButtonPause.IsChecked = true;
					OnPlaybackUIUpdate(null, null);
					uiUpdateTimer.Stop();
				});
			}
		}

		private void Stop() {
			watch.Stop();
			sequencer.Stop();
			sequencer.Position = 0;
			if (midi != null) {
				Dispatcher.Invoke(() => {
					toggleButtonStop.IsChecked = true;
					toggleButtonPlay.IsChecked = false;
					toggleButtonPause.IsChecked = false;
					OnPlaybackUIUpdate(null, null);
					uiUpdateTimer.Stop();
					labelClientPlaying.Content = "Stopped";
				});
			}
			if (server != null)
				HostSongFinished();
			if (client != null)
				ClientSongFinished();
		}
		#endregion

		#region Midi List Events
		private void OnMidiChanged(object sender, SelectionChangedEventArgs e) {
			if (!loaded)
				return;

			Stop();
			if (listMidis.SelectedIndex != -1) {
				midi = midis[listMidis.SelectedIndex];
				sequencer.Sequence = midi.Sequence;
				sequencer.Speed = 100.0 / (double)midi.Speed;
			}
			else {
				midi = null;
			}
			UpdateMidi();
		}

		private void OnAddMidi(object sender, RoutedEventArgs e) {
			Stop();

			loaded = false;
			OpenFileDialog dialog = new OpenFileDialog();
			dialog.Filter = "Midi Files|*.mid;*.midi|All Files|*.*";
			dialog.FilterIndex = 0;
			var result = dialog.ShowDialog(this);
			loaded = true;
			if (result.HasValue && result.Value) {
				string fileName = dialog.FileName;
				midi = new Midi();
				if (midi.Load(fileName)) {

					midis.Add(midi);
					listMidis.Items.Add("Loading...");
					listMidis.SelectedIndex = listMidis.Items.Count - 1;
					listMidis.ScrollIntoView(listMidis.Items[listMidis.SelectedIndex]);

					sequencer.Sequence = midi.Sequence;
					sequencer.Speed = 100.0 / (double)midi.Speed;
					listMidis.Items[listMidis.SelectedIndex] = midi.ProperName;
					listMidis.SelectedIndex = listMidis.Items.Count - 1;
					UpdateMidi();
				}
				else {
					var result2 = TriggerMessageBox.Show(this, MessageIcon.Error, "Error when loading the selected midi! Would you like to see the error?", "Load Error", MessageBoxButton.YesNo);
					if (result2 == MessageBoxResult.Yes)
						ErrorMessageBox.Show(midi.LoadException, true);
				}
			}
		}

		private void OnRemoveMidi(object sender, RoutedEventArgs e) {
			Stop();
			loaded = false;
			var result = TriggerMessageBox.Show(this, MessageIcon.Warning, "Are you sure you want to remove this midi?", "Remove Midi", MessageBoxButton.YesNo);
			loaded = true;
			if (result == MessageBoxResult.Yes) {
				int index = listMidis.SelectedIndex;

				loaded = false;
				listMidis.Items.RemoveAt(index);
				midis.RemoveAt(index);
				if (index > 0)
					index--;
				else if (index >= listMidis.Items.Count)
					index = -1;
				listMidis.SelectedIndex = index;
				if (index != -1)
					listMidis.ScrollIntoView(listMidis.Items[index]);
				loaded = true;

				if (index != -1) {
					midi = midis[index];
					sequencer.Sequence = midi.Sequence;
					sequencer.Speed = 100.0 / (double)midi.Speed;
				}
				else {
					midi = null;
				}
				UpdateMidi();
			}
		}

		private void OnEditMidiName(object sender, RoutedEventArgs e) {
			Stop();
			if (midi != null) {
				loaded = false;
				string newName = EditNameDialog.ShowDialog(this, midi.ProperName);
				if (newName != null) {
					int index = listMidis.SelectedIndex;
					midi.Name = newName;
					listMidis.Items[listMidis.SelectedIndex] = newName;
					listMidis.SelectedIndex = index;
				}
				loaded = true;
			}
		}

		private void OnMoveMidiUp(object sender, RoutedEventArgs e) {
			Stop();
			int index = listMidis.SelectedIndex;
			if (midi != null && index > 0) {
				loaded = false;
				listMidis.Items.RemoveAt(index);
				listMidis.Items.Insert(index - 1, midi.ProperName);
				listMidis.SelectedIndex = index - 1;
				midis.RemoveAt(index);
				midis.Insert(index - 1, midi);
				loaded = true;
				UpdateMidiButtons();
			}
		}

		private void OnMoveMidiDown(object sender, RoutedEventArgs e) {
			Stop();
			int index = listMidis.SelectedIndex;
			if (midi != null && index + 1 < listMidis.Items.Count) {
				loaded = false;
				listMidis.Items.RemoveAt(index);
				listMidis.Items.Insert(index + 1, midi.ProperName);
				listMidis.SelectedIndex = index + 1;
				midis.RemoveAt(index);
				midis.Insert(index + 1, midi);
				loaded = true;
				UpdateMidiButtons();
			}
		}
		#endregion

		#region Midi Setup Tab Events
		private void OnTrackChanged(object sender, SelectionChangedEventArgs e) {
			if (!loaded)
				return;

			UpdateTrack();
		}

		private void OnTrackEnabledClicked(object sender, RoutedEventArgs e) {
			if (!loaded)
				return;

			int index = listTracks.SelectedIndex;
			midi.GetTrackSettings(index).Enabled = checkBoxTrackEnabled.IsChecked.Value;

			loaded = false;
			listTracks.Items.RemoveAt(index);
			ListBoxItem item = new ListBoxItem();
			item.Content = "Track " + (index + 1);
			if (!midi.GetTrackSettings(index).Enabled)
				item.Foreground = Brushes.Gray;
			listTracks.Items.Insert(index, item);
			listTracks.SelectedIndex = index;
			loaded = true;
		}
		
		private void OnOctaveOffsetChanged(object sender, RoutedEventArgs e) {
			if (!loaded)
				return;

			midi.GetTrackSettings(listTracks.SelectedIndex).OctaveOffset = numericOctaveOffset.Value;
		}

		private void OnNoteOffsetChanged(object sender, RoutedEventArgs e) {
			if (!loaded)
				return;

			midi.NoteOffset = numericNoteOffset.Value;
			labelHighestNote.Content = "Highest Note: " + NoteToString(midi.GetTrack(listTracks.SelectedIndex).HighestNote + midi.NoteOffset);
			labelLowestNote.Content = "Lowest Note: " + NoteToString(midi.GetTrack(listTracks.SelectedIndex).LowestNote + midi.NoteOffset);
		}

		private void OnSpeedChanged(object sender, RoutedEventArgs e) {
			if (!loaded)
				return;

			midi.Speed = numericSpeed.Value;
			sequencer.Speed = 100.0 / (double)midi.Speed;
			labelDuration.Content = "Duration: " + MillisecondsToString(sequencer.Duration);
			OnPlaybackUIUpdate(null, null);
		}

		public void OnMidiKeybindChanged(object sender, RoutedEventArgs e) {
			if (!loaded)
				return;

			Keybind previous = midi.Keybind;
			Keybind newBind = keybindReaderMidi.Keybind;
			string name = "";
			if (newBind != Keybind.None) {
				if (newBind == keybindPlay)
					name = "Play Midi";
				else if (newBind == keybindPause)
					name = "Pause Midi";
				else if (newBind == keybindStop)
					name = "Stop Midi";
				else if (newBind == keybindClose)
					name = "Close Window";
				else if (newBind == keybindMount)
					name = "Toggle Mount";
				else {
					for (int i = 0; i < midis.Count; i++) {
						if (midis[i] != midi && newBind == midis[i].Keybind) {
							name = midis[i].ProperName;
							break;
						}
					}
				}
			}
			if (name == "") {
				midi.Keybind = newBind;
			}
			else {
				TriggerMessageBox.Show(this, MessageIcon.Error, "Keybind is already in use by the '" + name + "' keybind!", "Keybind in Use");
				keybindReaderMidi.Keybind = previous;
			}
		}
		#endregion

		#region Updating
		public void UpdateMidi() {
			loaded = false;
			listTracks.Items.Clear();
			loaded = true;
			if (midi != null) {
				loaded = false;
				labelTotalNotes.Content = "Total Notes: " + midi.TotalNotes;
				labelDuration.Content = "Duration: " + MillisecondsToString(sequencer.Duration);
				keybindReaderMidi.Keybind = midi.Keybind;
				numericNoteOffset.IsEnabled = true;
				numericSpeed.IsEnabled = true;
				numericNoteOffset.Value = midi.NoteOffset;
				numericSpeed.Value = midi.Speed;
				keybindReaderMidi.IsEnabled = true;
				if (midi.TrackCount > 0) {
					for (int i = 0; i < midi.TrackCount; i++) {
						ListBoxItem item = new ListBoxItem();
						item.Content = "Track " + (i + 1);
						if (!midi.GetTrackSettings(i).Enabled)
							item.Foreground = Brushes.Gray;
						listTracks.Items.Add(item);
					}
					listTracks.SelectedIndex = 0;
				}
				listTracks.IsEnabled = (midi.TrackCount > 0);

				sequencer.Speed = 100.0 / (double)midi.Speed;
				labelDuration.Content = "Duration: " + MillisecondsToString(sequencer.Duration);
				OnPlaybackUIUpdate(null, null);
				loaded = true;
			}
			else {
				labelTotalNotes.Content = "Total Notes: ";
				labelDuration.Content = "Duration: ";
				numericNoteOffset.IsEnabled = false;
				numericSpeed.IsEnabled = false;
				listTracks.IsEnabled = false;
				keybindReaderMidi.IsEnabled = false;
			}
			UpdateTrack();
			UpdateMidiButtons();
		}
		private void UpdateTrack() {
			if (midi != null && midi.TrackCount > 0) {
				loaded = false;
				labelHighestNote.Content = "Highest Note: " + NoteToString(midi.GetTrack(listTracks.SelectedIndex).HighestNote + midi.NoteOffset);
				labelLowestNote.Content = "Lowest Note: " + NoteToString(midi.GetTrack(listTracks.SelectedIndex).LowestNote + midi.NoteOffset);
				labelNotes.Content = "Notes: " + midi.GetTrack(listTracks.SelectedIndex).Notes;
				checkBoxTrackEnabled.IsChecked = midi.GetTrackSettings(listTracks.SelectedIndex).Enabled;
				numericOctaveOffset.Value = midi.GetTrackSettings(listTracks.SelectedIndex).OctaveOffset;
				numericOctaveOffset.IsEnabled = true;
				checkBoxTrackEnabled.IsEnabled = true;
				loaded = true;
			}
			else {
				labelHighestNote.Content = "Highest Note: ";
				labelLowestNote.Content = "Lowest Note: ";
				labelNotes.Content = "Notes: ";
				numericOctaveOffset.IsEnabled = false;
				checkBoxTrackEnabled.IsEnabled = false;
			}
		}
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
			if (listMidis.SelectedIndex != -1)
				labelMidiPosition.Content = MillisecondsToString(sequencer.CurrentTime) + "/" + MillisecondsToString(sequencer.Duration);
			else
				labelMidiPosition.Content = "-:--/-:--";
		}

		private void OnPlaybackUIUpdate(object sender, ElapsedEventArgs e) {
			Dispatcher.Invoke(() => {
				loaded = false;
				double currentProgress = sequencer.CurrentProgress;
				sliderMidiPosition.Value = (double.IsNaN(currentProgress) ? 0 : currentProgress);
				loaded = true;
				labelMidiPosition.Content = MillisecondsToString(sequencer.CurrentTime) + "/" + MillisecondsToString(sequencer.Duration);
			});
		}

		private void UpdateKeybindTooltips() {
			toggleButtonStop.ToolTip = "Stop midi playback. <";
			toggleButtonPlay.ToolTip = "Start midi playback. <";
			toggleButtonPause.ToolTip = "Pause midi playback. <";
			checkBoxMounted.ToolTip = "Needed to calculate the center of the player. <";
			// Yes I know stop can't "officially" be unassigned.
			toggleButtonStop.ToolTip += (keybindStop == Keybind.None ? "No Keybind" : keybindStop.ToCharString()) + ">";
			toggleButtonPlay.ToolTip += (keybindPlay == Keybind.None ? "No Keybind" : keybindPlay.ToCharString()) + ">";
			toggleButtonPause.ToolTip += (keybindPause == Keybind.None ? "No Keybind" : keybindPause.ToCharString()) + ">";
			checkBoxMounted.ToolTip += (keybindPause == Keybind.None ? "No Keybind" : keybindMount.ToCharString()) + ">";

		}
		#endregion

		#region Helpers
		private string NoteToString(int note) {
			string[] notes = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
			string[] altNotes = { "", "D\u266D", "", "E\u266D", "", "", "G\u266D", "", "A\u266D", "", "B\u266D", "" };
			int semitone = note % 12;
			note -= 12;
			string noteStr = notes[semitone] + (note / 12);
			if (altNotes[semitone].Length > 0)
				noteStr += " (" + altNotes[semitone] + (note / 12) + ")";
			return noteStr;
		}

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

		#region Menu Item Events
		private void OnExit(object sender, RoutedEventArgs e) {
			Close();
		}

		private void OnChangeKeybinds(object sender, RoutedEventArgs e) {
			Stop();
			loaded = false;
			ChangeKeybindsDialog.ShowDialog(this, ref keybindPlay, ref keybindPause, ref keybindStop, ref keybindClose, ref keybindMount,
				ref closeNoFocus, ref playbackNoFocus, midis);
			loaded = true;
			menuItemExit.InputGestureText = keybindClose.ToCharString();
			UpdateKeybindTooltips();
		}

		private void OnTerrariaExeName(object sender, RoutedEventArgs e) {
			loaded = false;
			ExecutableNameDialog.ShowDialog(this);
			loaded = true;
		}

		private void OnSaveConfig(object sender, RoutedEventArgs e) {
			SaveConfig(false);
		}

		private void OnOpenOnGitHub(object sender, RoutedEventArgs e) {
			Process.Start("https://github.com/trigger-death/TerrariaMidiPlayer");
		}

		private void OnAbout(object sender, RoutedEventArgs e) {
			loaded = false;
			AboutWindow.Show(this);
			loaded = true;
		}

		private void OnHelp(object sender, RoutedEventArgs e) {
			Process.Start("https://github.com/trigger-death/TerrariaMidiPlayer/wiki");
		}

		private void OnCredits(object sender, RoutedEventArgs e) {
			loaded = false;
			CreditsWindow.Show(this);
			loaded = true;
		}

		private void OnAboutInstruments(object sender, RoutedEventArgs e) {
			Process.Start("https://terraria.gamepedia.com/Harp");
		}
		#endregion

		#region Syncing
		private DateTime syncTime;
		private long syncTickCount;


		private void OnSyncTypeChanged(object sender, SelectionChangedEventArgs e) {
			if (!loaded)
				return;

			switch (comboBoxSyncType.SelectedIndex) {
				case 0:
					gridSyncClient.Visibility = Visibility.Visible;
					gridSyncHost.Visibility = Visibility.Hidden;
					break;
				case 1:
					gridSyncClient.Visibility = Visibility.Hidden;
					gridSyncHost.Visibility = Visibility.Visible;
					break;
			}
		}

		private DateTime CalculateSyncDateTime() {
			long ticks = unchecked((uint)Environment.TickCount) - syncTickCount;
			return syncTime.AddMilliseconds(ticks);
		}

		#region Host
		private Server server;
		private Dictionary<string, User> userMap;
		private List<User> userList;
		private string password = "";
		private Thread hostPlayThread;

		private void InitHost() {
			server = null;
			userMap = new Dictionary<string, User>();
			userList = new List<User>();

			gridSyncHost.Visibility = Visibility.Hidden;
			//buttonHostChecks.IsEnabled = false;
			textBoxHostNextSong.IsEnabled = false;
			buttonHostAssignSong.IsEnabled = false;
			listViewClients.IsEnabled = false;
			numericHostWait.IsEnabled = false;
			syncTime = DateTime.UtcNow;
			syncTickCount = unchecked((uint)Environment.TickCount);
			hostPlayThread = new Thread(HostPlay);
			gridHostPlaying.Visibility = Visibility.Hidden;
		}

		private void OnHostStartup(object sender, RoutedEventArgs e) {
			if (server == null) {
				int port = numericHostPort.Value;
				server = new Server();
				server.MessageReceived += OnHostMessageReceived;
				server.ClientConnected += OnHostClientConnected;
				server.ClientConnectionLost += OnHostClientConnectionLost;
				server.Error += OnHostError;
				if (!server.Start(port)) {
					TriggerMessageBox.Show(this, MessageIcon.Warning, "Failed to start host server!", "Host Error");
					server = null;
				}
				else {
					comboBoxSyncType.IsEnabled = false;
					textBoxHostPassword.IsEnabled = false;
					numericHostPort.IsEnabled = false;
					numericHostWait.IsEnabled = true;
					textBoxHostNextSong.IsEnabled = true;
					buttonHostAssignSong.IsEnabled = true;
					listViewClients.IsEnabled = true;
					buttonHostStartup.Content = "Shutdown";
					password = textBoxHostPassword.Text;
				}
			}
			else {
				Stop();
				server.Stop();
				server = null;
				userList.Clear();
				userMap.Clear();

				Dispatcher.Invoke(() => {
					listViewClients.Items.Clear();

					gridHostPlaying.Visibility = Visibility.Hidden;
					labelHostPlaying.Content = "Stopped";

					comboBoxSyncType.IsEnabled = true;
					textBoxHostPassword.IsEnabled = true;
					numericHostPort.IsEnabled = true;
					numericHostWait.IsEnabled = false;
					textBoxHostNextSong.IsEnabled = false;
					buttonHostAssignSong.IsEnabled = false;
					listViewClients.IsEnabled = false;
					buttonHostStartup.Content = "Startup";
					textBoxHostNextSong.Text = "";
				});
			}
		}

		private void OnHostAssignSong(object sender, RoutedEventArgs e) {
			server.Send(new StringCommand(Commands.AssingSong, Server.ServerName, textBoxHostNextSong.Text));
			for (int i = 0; i < userList.Count; i++) {
				userList[i].Ready = ReadyStates.NotReady;
				((HostClientListViewItem)listViewClients.Items[i]).Ready = userList[i].Ready;
			}
		}
		
		private void OnHostMessageReceived(Server server, ServerConnection connection, byte[] data, int size) {
			switch (Command.GetCommandType(data, data.Length)) {
				case Commands.Login: {
						var cmd = new StringCommand(data, data.Length);
						connection.User.IPAddress = connection.IPAddress;
						connection.User.Port = connection.Port;
						if (cmd.Text != password) {
							server.SendToConnection(new Command(Commands.InvalidPassword, Server.ServerName), connection);
						}
						else if (userMap.ContainsKey(cmd.Name)) {
							server.SendToConnection(new Command(Commands.NameTaken, Server.ServerName), connection);
						}
						else {
							connection.User.Name = cmd.Name;
							connection.IsLoggedIn = true;
							AddUser(connection.User);
							server.SendTo(new Command(Commands.AcceptedUser, Server.ServerName), cmd.Name);
						}
						break;
					}
				case Commands.Ready: {
						connection.User.Ready = ReadyStates.Ready;
						int index = userList.IndexOf(connection.User);
						if (index != -1) {
							Dispatcher.Invoke(() => {
								((HostClientListViewItem)listViewClients.Items[index]).Ready = ReadyStates.Ready;
							});
						}
						break;
					}
				case Commands.NotReady: {
						connection.User.Ready = ReadyStates.NotReady;
						int index = userList.IndexOf(connection.User);
						if (index != -1) {
							Dispatcher.Invoke(() => {
								((HostClientListViewItem)listViewClients.Items[index]).Ready = ReadyStates.NotReady;
							});
						}
						break;
					}
			}
		}
		private void OnHostClientConnected(Server server, ServerConnection connection) {
			
		}
		private void OnHostClientConnectionLost(Server server, ServerConnection connection) {
			RemoveUser(connection.Username);
		}
		private void OnHostError(Server server, Exception e) {
			var result = TriggerMessageBox.Show(this, MessageIcon.Error, "A host error occurred. Would you like to see the error?", "Host Error", MessageBoxButton.YesNo);
			if (result == MessageBoxResult.Yes)
				ErrorMessageBox.Show(e);
		}

		private void HostStartPlay() {
			if (hostPlayThread.ThreadState == System.Threading.ThreadState.Unstarted) {
				hostPlayThread.Start();
			}
			else if (hostPlayThread.ThreadState == System.Threading.ThreadState.Stopped) {
				hostPlayThread = new Thread(HostPlay);
				hostPlayThread.Start();
			}
		}
		private void HostPlay() {
			try {
				DateTime playTime = CalculateSyncDateTime();
				playTime = playTime.AddMilliseconds(numericHostWait.Value);
				server.IsPaused = true;

				foreach (User user in userList) {
					server.SendToNow(new TimeCommand(Commands.PlaySong, Server.ServerName, playTime), user.Name);
				}
				/*while (CalculateSyncDateTime() < playTime) {
					Thread.Sleep(1);
				}*/

				TimeSpan difference = playTime - CalculateSyncDateTime();
				Dispatcher.Invoke(() => {
					gridHostPlaying.Visibility = Visibility.Visible;
					labelHostPlaying.Content = "Playing in " + MillisecondsToString((int)difference.TotalMilliseconds, false, true);
				});
				while (difference.TotalMilliseconds > 0) {
					if (difference.TotalMilliseconds >= 500) {
						Dispatcher.Invoke(() => {
							labelHostPlaying.Content = "Playing in " + MillisecondsToString((int)difference.TotalMilliseconds, false, true);
						});
						if (server == null)
							return;
						lock (server) {
							if (!server.IsPaused) {
								Dispatcher.Invoke(() => {
									gridHostPlaying.Visibility = Visibility.Hidden;
									labelHostPlaying.Content = "Stopped";
								});
								return;
							}
						}
						Thread.Yield();
						Thread.Sleep(10);
					}
					else {
						Thread.Sleep(2);
					}
					difference = playTime - CalculateSyncDateTime();
				}

				Play();
				Dispatcher.Invoke(() => {
					labelHostPlaying.Content = "Playing now";
				});
			}
			catch (Exception ex) { }
		}

		private void HostStop() {
			server.IsPaused = false;
			server.Send(new Command(Commands.StopSong, Server.ServerName));
			Stop();
			Dispatcher.Invoke(() => {
				gridHostPlaying.Visibility = Visibility.Hidden;
				labelHostPlaying.Content = "Stopped";
			});
		}

		private void HostSongFinished() {
			server.IsPaused = false;
			Dispatcher.Invoke(() => {
				gridHostPlaying.Visibility = Visibility.Hidden;
				labelHostPlaying.Content = "Stopped";
			});
		}

		private void AddUser(User user) {
			user.Ready = ReadyStates.NotReady;
			Dispatcher.Invoke(() => {
				userMap.Add(user.Name, user);
				userList.Add(user);
				Dispatcher.Invoke(() => {
					listViewClients.Items.Add(new HostClientListViewItem(user.Name));
				});
			});
		}

		private void RemoveUser(string username) {
			if (userMap.ContainsKey(username)) {
				int index = userList.FindIndex(u => u.Name == username);
				if (index != -1) {
					userMap.Remove(username);
					userList.RemoveAt(index);
					Dispatcher.Invoke(() => {
						listViewClients.Items.RemoveAt(index);
					});
				}
			}
		}

		#endregion

		#region Client
		private ClientConnection client;
		private User clientUser;
		private bool clientReady;
		private Timer clientTimeout;
		private bool clientAccepted;
		private int clientTimeOffset;
		private Stopwatch reconnectWatch;

		private void InitClient() {
			client = null;
			clientUser = new User();
			clientReady = false;
			clientTimeout = new Timer(4000);
			clientTimeout.Elapsed += OnClientConnectingTimeout;
			clientTimeout.AutoReset = false;
			clientAccepted = false;
			clientTimeOffset = 0;
			reconnectWatch = new Stopwatch();

			gridSyncClient.Visibility = Visibility.Visible;
			buttonClientReady.IsEnabled = false;
			textBoxClientNextSong.IsEnabled = false;
			numericClientPlayOffset.IsEnabled = false;
		}

		private void OnClientConnect(object sender, RoutedEventArgs e) {
			if (client == null) {
				if (textBoxClientUsername.Text == "") {
					TriggerMessageBox.Show(this, MessageIcon.Warning, "Cannot connect with an empty username!", "Invalid Username");
					return;
				}
				IPAddress ip = (textBoxClientIP.Text == "" ? IPAddress.Loopback : IPAddress.Parse(textBoxClientIP.Text));
				int port = numericClientPort.Value;
				client = new ClientConnection();
				client.MessageReceived += OnClientMessageReceived;
				client.ConnectionLost += OnClientConnectionLost;
				clientUser.Name = textBoxClientUsername.Text;
				if (!client.Connect(ip, port)) {
					TriggerMessageBox.Show(this, MessageIcon.Warning, "Failed to connect to server!", "Connection Failed");
					client = null;
				}
				else {
					comboBoxSyncType.IsEnabled = false;
					textBoxClientIP.IsEnabled = false;
					numericClientPort.IsEnabled = false;
					textBoxClientUsername.IsEnabled = false;
					textBoxClientPassword.IsEnabled = false;
					textBoxClientNextSong.IsEnabled = false;
					buttonClientConnect.IsEnabled = false;
					buttonClientConnect.Content = "Connecting...";
					clientTimeout.Start();
					
					client.Send(new StringCommand(Commands.Login, clientUser.Name, textBoxClientPassword.Text));
				}
			}
			else {
				Stop();
				client.Disconnect();
				client = null;
				clientUser = new User();

				Dispatcher.Invoke(() => {
					comboBoxSyncType.IsEnabled = true;
					textBoxClientIP.IsEnabled = true;
					numericClientPort.IsEnabled = true;
					textBoxClientUsername.IsEnabled = true;
					textBoxClientPassword.IsEnabled = true;
					buttonClientReady.IsEnabled = false;
					textBoxClientNextSong.IsEnabled = false;
					buttonClientConnect.IsEnabled = true;
					numericClientPlayOffset.IsEnabled = false;
					buttonClientReady.Content = "Ready";
					buttonClientConnect.Content = "Connect";
					textBoxClientNextSong.Text = "";
					labelClientPlaying.Content = "Stopped";
				});
				clientAccepted = false;
				clientReady = false;
				reconnectWatch.Restart();
			}
		}

		private void OnClientReady(object sender, RoutedEventArgs e) {
			if (!clientReady) {
				clientReady = true;
				buttonClientReady.Content = "Not Ready";
				client.Send(new Command(Commands.Ready, clientUser.Name));
			}
			else {
				clientReady = false;
				buttonClientReady.Content = "Ready";
				client.Send(new Command(Commands.NotReady, clientUser.Name));
			}
		}

		private void OnClientConnectingTimeout(object sender, ElapsedEventArgs e) {
			if (!clientAccepted && client != null) {
				Dispatcher.Invoke(() => {
					OnClientConnect(null, new RoutedEventArgs());
					if (reconnectWatch.ElapsedMilliseconds < 1500 + (int)clientTimeout.Interval)
						TriggerMessageBox.Show(this, MessageIcon.Warning, "Failed to login! You are trying to reconnect to the server too quickly. Wait at least one second before reconnecting.", "Login Failed");
					else
						TriggerMessageBox.Show(this, MessageIcon.Warning, "Failed to login!", "Login Failed");
				});
			}
		}

		private void OnClientMessageReceived(ClientConnection client, byte[] data, int size) {
			switch (Command.GetCommandType(data, data.Length)) {
				case Commands.InvalidPassword: {
						if (!clientAccepted) {
							Dispatcher.Invoke(() => {
								OnClientConnect(null, new RoutedEventArgs());
								if (textBoxClientPassword.Text.Length == 0)
									TriggerMessageBox.Show(this, MessageIcon.Warning, "A password is required.", "Password Required");
								else
									TriggerMessageBox.Show(this, MessageIcon.Warning, "The chosen password is incorrect.", "Invalid Password");
							});
						}
						break;
					}
				case Commands.NameTaken: {
						if (!clientAccepted) {
							Dispatcher.Invoke(() => {
								OnClientConnect(null, new RoutedEventArgs());
								if (reconnectWatch.ElapsedMilliseconds < 1500)
									TriggerMessageBox.Show(this, MessageIcon.Warning, "You are trying to reconnect to the server too quickly.\nWait at least one second before reconnecting.", "Name Taken");
								else
									TriggerMessageBox.Show(this, MessageIcon.Warning, "The chosen username is already in use.", "Name Taken");
							});
						}
						break;
					}
				case Commands.AcceptedUser: {
						if (!clientAccepted) {
							clientAccepted = true;
							// Finish logging in
							clientTimeout.Stop();
							Dispatcher.Invoke(() => {
								textBoxClientNextSong.IsEnabled = true;
								numericClientPlayOffset.IsEnabled = true;
								buttonClientReady.IsEnabled = true;
								buttonClientConnect.IsEnabled = true;
								buttonClientConnect.Content = "Disconnect";
							});
						}
						break;
					}
				case Commands.AssingSong: {
						if (clientAccepted) {
							clientReady = false;
							var cmd = new StringCommand(data, data.Length);
							Dispatcher.Invoke(() => {
								textBoxClientNextSong.Text = cmd.Text;
								buttonClientReady.Content = "Ready";
							});
						}
						break;
					}
				case Commands.PlaySong: {
						if (clientAccepted && clientReady) {
							try {
								var cmd = new TimeCommand(data, size);
								DateTime playTime = cmd.DateTime.AddMilliseconds(clientTimeOffset);
								client.IsPlaying = true;
								TimeSpan difference = playTime - CalculateSyncDateTime();
								while (difference.TotalMilliseconds > 0) {
									if (difference.TotalMilliseconds >= 500) {
										Dispatcher.Invoke(() => {
											labelClientPlaying.Content = "Playing in " + MillisecondsToString((int)difference.TotalMilliseconds, false, true);
										});
										if (client == null)
											return;
										lock (client) {
											if (!client.IsPlaying) {
												Dispatcher.Invoke(() => {
													labelClientPlaying.Content = "Stopped";
												});
												return;
											}
										}
										Thread.Yield();
										Thread.Sleep(10);
									}
									else {
										Thread.Sleep(2);
									}
									difference = playTime - CalculateSyncDateTime();
								}

								Play();
								Dispatcher.Invoke(() => {
									if (difference.TotalMilliseconds < -400)
										labelClientPlaying.Content = "Played " + ((long)-difference.TotalMilliseconds).ToString() + "ms early";
									else
										labelClientPlaying.Content = "Playing now";
								});
							}
							catch (Exception ex) { }
						}
						break;
					}
				case Commands.StopSong: {
						if (clientAccepted) {
							if (client.IsPlaying) {
								Stop();
								client.IsPlaying = false;
								Dispatcher.Invoke(() => {
									labelClientPlaying.Content = "Stopped";
								});
							}
						}
						break;
					}
			}
		}
		private void OnClientConnectionLost(ClientConnection client) {
			OnClientConnect(null, new RoutedEventArgs());
		}

		private void ClientSongFinished() {
			client.IsPlaying = false;
			Dispatcher.Invoke(() => {
				labelClientPlaying.Content = "Stopped";
			});
		}

		private void OnClientTimeOffsetChanged(object sender, RoutedEventArgs e) {
			if (!loaded)
				return;

			clientTimeOffset = numericClientPlayOffset.Value;
		}

		#endregion

		#endregion
	}
}
