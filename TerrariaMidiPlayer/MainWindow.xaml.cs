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

		Stopwatch watch = new Stopwatch();
		IKeyboardMouseEvents globalHook;
		Random rand = new Random();
		int useTime = 3;
		Song song;

		Midi midi = null;
		Sequencer sequencer;

		Rect clientArea = new Rect(0, 0, 0, 0);

		double projectileAngle = 0;
		double projectileRange = 360;

		Keybind keybindPlay = new Keybind(Key.NumPad0);
		Keybind keybindPause = new Keybind(Key.NumPad1);
		Keybind keybindStop = new Keybind(Key.NumPad2);
		Keybind keybindClose = new Keybind(Key.Escape);

		int mount = 0;

		bool checksEnabled = true;
		int checkFrequency = 0;
		int checkCount = 0;
		
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

		int clickTime = 40;

		public MainWindow() {
			
			InitializeComponent();
			loaded = false;
			rand = new Random();
			globalHook = Hook.GlobalEvents();
			watch = new Stopwatch();
			useTime = 3;
			globalHook.KeyDown += OnGlobalKeyDown;

			midis = new List<Midi>();

			sequencer = new Sequencer();
			sequencer.ChannelMessagePlayed += OnChannelMessagePlayed;
			sequencer.PlayingCompleted += OnPlayingCompleted;
			clientArea = new Rect();

			projectileAngle = 0;
			projectileRange = 360;

			checksEnabled = true;
			checkFrequency = 0;
			checkCount = 0;
			clickTime = 40;

			mount = 0;

			for (int i = 0; i < Mounts.Length; i++) {
				comboBoxMount.Items.Add(Mounts[i].Name);
			}
			comboBoxMount.SelectedIndex = 0;

			LoadConfig();

			UpdateMidi();

			#region Songs
			song = new Song();
			Song kombat = new Song();
			Song shipping = new Song();

			#region Kombat
			KombatMain();
			KombatMain();
			KombatMain2();
			KombatMain2();
			KombatMain2_5();
			KombatMain3();
			KombatMain();
			KombatMain();
			KombatMain2();
			KombatMain2();
			KombatMain4();
			KombatMain4();
			KombatMain();
			KombatMain();
			#endregion

			kombat = song;
			song = new Song();

			#region Shipping
			ShippingIntro();
			ShippingMain();
			ShippingMain();
			ShippingHigh();
			ShippingHigh();
			ShippingMain();
			ShippingMain();
			ShippingHigh();
			ShippingHigh();
			ShippingAlt();
			ShippingMain();
			ShippingMain();
			ShippingHigh();
			ShippingHigh();
			ShippingAlt();
			ShippingEnd();
			#endregion

			shipping = song;
			song = kombat;
			#endregion
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
					node = doc.SelectSingleNode("/TerrariaMidiPlayer/Settings/UseTime");
					if (node != null) int.TryParse(node.InnerText, out useTime);
					numericUseTime.Value = useTime;

					node = doc.SelectSingleNode("/TerrariaMidiPlayer/Settings/ClickTime");
					if (node != null) int.TryParse(node.InnerText, out clickTime);
					numericClickTime.Value = clickTime;

					node = doc.SelectSingleNode("/TerrariaMidiPlayer/Settings/ChecksEnabled");
					if (node != null) bool.TryParse(node.InnerText, out checksEnabled);
					checkBoxChecks.IsChecked = checksEnabled;

					node = doc.SelectSingleNode("/TerrariaMidiPlayer/Settings/CheckFrequency");
					if (node != null) int.TryParse(node.InnerText, out checkFrequency);
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
					if (node != null) double.TryParse(node.InnerText, out projectileAngle);
					projectileControl.Angle = (int)projectileAngle;

					node = doc.SelectSingleNode("/TerrariaMidiPlayer/Settings/ProjectileRange");
					if (node != null) double.TryParse(node.InnerText, out projectileRange);
					projectileControl.Range = (int)projectileRange;

					node = doc.SelectSingleNode("/TerrariaMidiPlayer/Settings/KeybindPlay");
					if (node != null) Keybind.TryParse(node.InnerText, out keybindPlay);

					node = doc.SelectSingleNode("/TerrariaMidiPlayer/Settings/KeybindPause");
					if (node != null) Keybind.TryParse(node.InnerText, out keybindPause);

					node = doc.SelectSingleNode("/TerrariaMidiPlayer/Settings/KeybindStop");
					if (node != null) Keybind.TryParse(node.InnerText, out keybindStop);

					node = doc.SelectSingleNode("/TerrariaMidiPlayer/Settings/KeybindClose");
					if (node != null) Keybind.TryParse(node.InnerText, out keybindClose);
					#endregion

					#region Midis
					XmlNodeList midiList = doc.SelectNodes("/TerrariaMidiPlayer/Midis/Midi");
					for (int i = 0; i < midiList.Count; i++) {
						Midi midi = new Midi();
						if (midi.LoadConfig(midiList[i])) {
							midis.Add(midi);
							listMidis.Items.Add(midi.ProperName);
						}
					}

					if (midis.Count > 0) {
						listMidis.SelectedIndex = 0;
						this.midi = midis[0];
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
				SaveConfig();
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

				element = doc.CreateElement("KeybindPlay");
				element.AppendChild(doc.CreateTextNode(keybindPlay.ToString()));
				setting.AppendChild(element);

				element = doc.CreateElement("KeybindPause");
				element.AppendChild(doc.CreateTextNode(keybindPause.ToString()));
				setting.AppendChild(element);

				element = doc.CreateElement("KeybindStop");
				element.AppendChild(doc.CreateTextNode(keybindStop.ToString()));
				setting.AppendChild(element);

				element = doc.CreateElement("KeybindClose");
				element.AppendChild(doc.CreateTextNode(keybindClose.ToString()));
				setting.AppendChild(element);
				#endregion

				#region Midis
				XmlElement midis = doc.CreateElement("Midis");
				midiPlayer.AppendChild(midis);

				foreach (Midi midi in this.midis) {
					XmlElement midiElement = doc.CreateElement("Midi");
					midis.AppendChild(midiElement);

					element = doc.CreateElement("FilePath");
					element.AppendChild(doc.CreateTextNode(midi.Path));
					midiElement.AppendChild(element);

					element = doc.CreateElement("LastModified");
					element.AppendChild(doc.CreateTextNode(midi.LastModified.ToString()));
					midiElement.AppendChild(element);

					element = doc.CreateElement("Name");
					element.AppendChild(doc.CreateTextNode(midi.Name));
					midiElement.AppendChild(element);

					element = doc.CreateElement("NoteOffset");
					element.AppendChild(doc.CreateTextNode(midi.NoteOffset.ToString()));
					midiElement.AppendChild(element);

					element = doc.CreateElement("Speed");
					element.AppendChild(doc.CreateTextNode(midi.Speed.ToString()));
					midiElement.AppendChild(element);

					element = doc.CreateElement("Keybind");
					element.AppendChild(doc.CreateTextNode(midi.Keybind.ToString()));
					midiElement.AppendChild(element);

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
			globalHook.KeyDown -= OnGlobalKeyDown;
			globalHook.Dispose();
			globalHook = null;
			watch.Stop();
			sequencer.Stop();
			SaveConfig(true);
		}

		private void OnGlobalKeyDown(object sender, System.Windows.Forms.KeyEventArgs e) {
			if (!loaded || keybindReaderMidi.IsReading)
				return;

			if (midi != null) {
				if (keybindPlay.IsDown(e)) {
					Play();
				}
				else if (keybindPause.IsDown(e)) {
					Pause();
				}
				else if (keybindStop.IsDown(e)) {
					Stop();
				}
			}
			for (int i = 0; i < midis.Count; i++) {
				if (midis[i].Keybind.IsDown(e)) {
					Stop();

					loaded = false;
					listMidis.SelectedIndex = i;
					loaded = true;
					midi = midis[listMidis.SelectedIndex];
					sequencer.Sequence = midi.Sequence;

					UpdateMidi();
				}
			}
			if (keybindClose.IsDown(e)) {
				Close();
			}
		}
		#endregion

		#region Midi Playing
		private void OnPlayingCompleted(object sender, EventArgs e) {
			Stop();
		}

		private void OnChannelMessagePlayed(object sender, ChannelMessageEventArgs e) {
			if (midi.IsMessagePlayable(e.Message) && watch.ElapsedMilliseconds >= useTime * 1000 / 60 + 2) {
				if (checksEnabled) {
					checkCount++;
					if (checkCount > checkFrequency) {
						checkCount = 0;
						TerrariaWindowLocator.Update();
						if (!TerrariaWindowLocator.HasFocus) {
							TerrariaWindowLocator.Focus();
							Thread.Sleep(100);
						}
						if (!TerrariaWindowLocator.IsOpen) {
							Pause();
							return;
						}
						clientArea = TerrariaWindowLocator.ClientArea;
					}
				}
				int note = e.Message.Data1 - 12 * (midi.GetTrackSettingsByChannel(e.Message.MidiChannel).OctaveOffset + 1) + midi.NoteOffset;
				watch.Restart();
				PlaySemitone(note, (projectileAngle - projectileRange / 2 + rand.NextDouble() * projectileRange + 270) / 360.0 * Math.PI * 2.0);
			}
		}

		private void PlaySemitone(int semitone, double direction) {
			double heightRatio = clientArea.Height / 48.0;
			while (semitone < 0)
				semitone += 12;
			while (semitone > 24)
				semitone -= 12;
			double centerx = clientArea.Width / 2;
			double centery = clientArea.Height / 2 - Mounts[mount].Offset;
			int x = (int)(centerx + Math.Cos(direction) * (heightRatio * semitone + 2));
			int y = (int)(centery + Math.Sin(direction) * (heightRatio * semitone + 2));
			if (x < 0) x = 0;
			if (x >= (int)clientArea.Width) x = (int)clientArea.Width - 1;
			if (y < 0) y = 0;
			if (y >= (int)clientArea.Height) y = (int)clientArea.Height - 1;
			x += (int)clientArea.X;
			y += (int)clientArea.Y;
			MouseControl.SimulateClick(x, y, clickTime);
		}
		#endregion

		#region Songs
		#region ShippingIntro
		private void ShippingIntro() {
			//--------------------
			song.Add(Notes.E0, 2);
			song.Add(Notes.E0, 10);
			//--------------------
			song.Add(Notes.E0, 2);
			song.Add(Notes.E0, 4);
			song.Add(Notes.E1, 1);
			song.Add(Notes.E1, 1);
			song.Add(Notes.E1, 1);
			song.Add(Notes.E1, 1);
			song.Add(Notes.E1, 1);
			song.Add(Notes.E1, 1);
			//--------------------
			song.Add(Notes.E0, 2);
			song.Add(Notes.D0, 1);
			song.Add(Notes.E0, 2);
			song.Add(Notes.D0, 1);

			song.Add(Notes.E0, 1);
			song.Add(Notes.Fs0, 1);
			song.Add(Notes.G0, 1);

			song.Add(Notes.Fs0, 1);
			song.Add(Notes.E0, 1);
			song.Add(Notes.D0, 1);
			//--------------------
			song.Add(Notes.B0, 1);
			song.Add(Notes.A0, 1);
			song.Add(Notes.G0, 1);

			song.Add(Notes.A0, 1);
			song.Add(Notes.G0, 1);
			song.Add(Notes.Fs0, 1);

			song.Add(Notes.G0, 1);
			song.Add(Notes.Fs0, 1);
			song.Add(Notes.D0, 1);

			song.Add(Notes.E0, 2);
			song.Add(Notes.D0, 1);
			//--------------------
		}
		#endregion
		#region ShippingMain
		private void ShippingMain() {
			//--------------------
			song.Add(Notes.E0, 2);
			song.Add(Notes.D0, 1);
			song.Add(Notes.E0, 2);
			song.Add(Notes.D0, 1);

			song.Add(Notes.E0, 1);
			song.Add(Notes.Fs0, 1);
			song.Add(Notes.G0, 1);

			song.Add(Notes.Fs0, 1);
			song.Add(Notes.E0, 1);
			song.Add(Notes.D0, 1);
			//--------------------
			song.Add(Notes.E0, 2);
			song.Add(Notes.D0, 1);
			song.Add(Notes.E0, 2);
			song.Add(Notes.D0, 1);

			song.Add(Notes.E0, 1);
			song.Add(Notes.Fs0, 1);
			song.Add(Notes.G0, 1);

			song.Add(Notes.B0, 3);
			//--------------------
			song.Add(Notes.E0, 2);
			song.Add(Notes.D0, 1);
			song.Add(Notes.E0, 2);
			song.Add(Notes.D0, 1);

			song.Add(Notes.E0, 1);
			song.Add(Notes.Fs0, 1);
			song.Add(Notes.G0, 1);

			song.Add(Notes.Fs0, 1);
			song.Add(Notes.E0, 1);
			song.Add(Notes.D0, 1);
			//--------------------
			song.Add(Notes.B0, 1);
			song.Add(Notes.A0, 1);
			song.Add(Notes.G0, 1);

			song.Add(Notes.A0, 1);
			song.Add(Notes.G0, 1);
			song.Add(Notes.Fs0, 1);

			song.Add(Notes.G0, 1);
			song.Add(Notes.Fs0, 1);
			song.Add(Notes.D0, 1);

			song.Add(Notes.E0, 2);
			song.Add(Notes.D0, 1);
			//--------------------
		}
		#endregion
		#region ShippingHigh
		private void ShippingHigh() {
			//--------------------
			song.Add(Notes.B0, 2);
			song.Add(Notes.A0, 1);
			song.Add(Notes.B0, 2);
			song.Add(Notes.A0, 1);

			song.Add(Notes.B0, 1);
			song.Add(Notes.Cs1, 1);
			song.Add(Notes.D1, 1);

			song.Add(Notes.Cs1, 1);
			song.Add(Notes.B0, 1);
			song.Add(Notes.A0, 1);
			//--------------------
			song.Add(Notes.B0, 2);
			song.Add(Notes.A0, 1);
			song.Add(Notes.B0, 2);
			song.Add(Notes.A0, 1);

			song.Add(Notes.B0, 1);
			song.Add(Notes.Cs1, 1);
			song.Add(Notes.D1, 1);

			song.Add(Notes.Fs1, 3);
			//--------------------
			song.Add(Notes.B0, 2);
			song.Add(Notes.A0, 1);
			song.Add(Notes.B0, 2);
			song.Add(Notes.A0, 1);

			song.Add(Notes.B0, 1);
			song.Add(Notes.Cs1, 1);
			song.Add(Notes.D1, 1);

			song.Add(Notes.Cs1, 1);
			song.Add(Notes.B0, 1);
			song.Add(Notes.A0, 1);
			//--------------------
			song.Add(Notes.Fs1, 1);
			song.Add(Notes.E1, 1);
			song.Add(Notes.D1, 1);

			song.Add(Notes.E1, 1);
			song.Add(Notes.D1, 1);
			song.Add(Notes.Cs1, 1);

			song.Add(Notes.D1, 1);
			song.Add(Notes.Cs1, 1);
			song.Add(Notes.A0, 1);

			song.Add(Notes.B0, 2);
			song.Add(Notes.A0, 1);
			//--------------------
		}
		#endregion
		#region ShippingEnd
		private void ShippingEnd() {
			//--------------------
			song.Add(Notes.E0, 2);
			song.Add(Notes.D0, 1);
			song.Add(Notes.E0, 2);
			song.Add(Notes.D0, 1);

			song.Add(Notes.E0, 1);
			song.Add(Notes.Fs0, 1);
			song.Add(Notes.G0, 1);

			song.Add(Notes.Fs0, 1);
			song.Add(Notes.E0, 1);
			song.Add(Notes.D0, 1);
			//--------------------
			song.Add(Notes.E0, 2);
			song.Add(Notes.D0, 1);
			song.Add(Notes.E0, 2);
			song.Add(Notes.D0, 1);

			song.Add(Notes.E0, 1);
			song.Add(Notes.Fs0, 1);
			song.Add(Notes.G0, 1);

			song.Add(Notes.B0, 3);
			//--------------------
			song.Add(Notes.E0, 2);
			song.Add(Notes.D0, 1);
			song.Add(Notes.E0, 2);
			song.Add(Notes.D0, 1);

			song.Add(Notes.E0, 1);
			song.Add(Notes.Fs0, 1);
			song.Add(Notes.G0, 1);

			song.Add(Notes.Fs0, 1);
			song.Add(Notes.E0, 1);
			song.Add(Notes.D0, 1);
			//--------------------
			song.Add(Notes.B0, 1);
			song.Add(Notes.A0, 1);
			song.Add(Notes.G0, 1);

			song.Add(Notes.A0, 1);
			song.Add(Notes.G0, 1);
			song.Add(Notes.D0, 1);

			song.Add(Notes.E0, 1);
			song.Add(Notes.E0, 1);
			song.Add(Notes.E0, 1);
			song.Add(Notes.E0, 1);
			//--------------------
		}
		#endregion
		#region ShippingAlt
		private void ShippingAlt() {
			//--------------------
			song.Add(Notes.Fs1, 2);
			song.Add(Notes.Fs1, 1);
			song.Add(Notes.Fs1, 2);
			song.Add(Notes.Fs1, 1);
			song.Add(Notes.Fs1, 2);
			song.Add(Notes.Fs1, 1);
			song.Add(Notes.D1, 3);

			song.Add(Notes.G1, 3);
			song.Add(Notes.Fs1, 3);
			song.Add(Notes.D1, 5);
			song.Add(Notes.E1, 1);
			//--------------------
			song.Add(Notes.Fs1, 2);
			song.Add(Notes.Fs1, 1);
			song.Add(Notes.Fs1, 2);
			song.Add(Notes.Fs1, 1);
			song.Add(Notes.Fs1, 2);
			song.Add(Notes.Fs1, 1);
			song.Add(Notes.D1, 3);

			song.Add(Notes.G1, 3);
			song.Add(Notes.G1, 3);
			song.Add(Notes.G1, 3);
			song.Add(Notes.A1, 3);
			//--------------------
			song.Add(Notes.Fs1, 2);
			song.Add(Notes.Fs1, 1);
			song.Add(Notes.Fs1, 2);
			song.Add(Notes.Fs1, 1);
			song.Add(Notes.Fs1, 2);
			song.Add(Notes.Fs1, 1);
			song.Add(Notes.D1, 3);

			song.Add(Notes.G1, 3);
			song.Add(Notes.Fs1, 3);
			song.Add(Notes.D1, 5);
			song.Add(Notes.E1, 1);
			//--------------------
			song.Add(Notes.Fs1, 2);
			song.Add(Notes.Fs1, 1);
			song.Add(Notes.Fs1, 2);
			song.Add(Notes.Fs1, 1);
			song.Add(Notes.Fs1, 2);
			song.Add(Notes.Fs1, 1);
			song.Add(Notes.Fs1, 2);
			song.Add(Notes.Fs1, 1);

			song.Add(Notes.E1, 3);
			song.Add(Notes.Fs1, 3);
			song.Add(Notes.D1, 5);
			song.Add(Notes.D0, 1);
			//--------------------
		}
		#endregion

		#region KombatMain
		private void KombatMain() {
			//--------------------
			song.Add(Notes.A0, 2);
			song.Add(Notes.A0, 2);
			song.Add(Notes.C1, 2);
			song.Add(Notes.A0, 2);
			song.Add(Notes.D1, 2);
			song.Add(Notes.A0, 2);
			song.Add(Notes.E1, 2);
			song.Add(Notes.D1, 2);
			//--------------------
			song.Add(Notes.C1, 2);
			song.Add(Notes.C1, 2);
			song.Add(Notes.E1, 2);
			song.Add(Notes.C1, 2);
			song.Add(Notes.G1, 2);
			song.Add(Notes.C1, 2);
			song.Add(Notes.E1, 2);
			song.Add(Notes.C1, 2);
			//--------------------
			song.Add(Notes.G0, 2);
			song.Add(Notes.G0, 2);
			song.Add(Notes.B0, 2);
			song.Add(Notes.G0, 2);
			song.Add(Notes.C1, 2);
			song.Add(Notes.G0, 2);
			song.Add(Notes.D1, 2);
			song.Add(Notes.C1, 2);
			//--------------------
			song.Add(Notes.F0, 2);
			song.Add(Notes.F0, 2);
			song.Add(Notes.A0, 2);
			song.Add(Notes.F0, 2);
			song.Add(Notes.C1, 2);
			song.Add(Notes.F0, 2);
			song.Add(Notes.C1, 2);
			song.Add(Notes.B0, 2);
			//--------------------
		}
		#endregion
		#region KombatMain2
		private void KombatMain2() {
			//--------------------
			song.Add(Notes.A0, 3);
			song.Add(Notes.A0, 3);
			song.Add(Notes.A0, 3);
			song.Add(Notes.A0, 3);
			song.Add(Notes.G0, 2);
			song.Add(Notes.C1, 2);
			//--------------------
			song.Add(Notes.A0, 3);
			song.Add(Notes.A0, 3);
			song.Add(Notes.A0, 3);
			song.Add(Notes.A0, 3);
			song.Add(Notes.G0, 2);
			song.Add(Notes.E0, 2);
			//--------------------
			song.Add(Notes.A0, 3);
			song.Add(Notes.A0, 3);
			song.Add(Notes.A0, 3);
			song.Add(Notes.A0, 3);
			song.Add(Notes.G0, 2);
			song.Add(Notes.C1, 2);
			//--------------------
			song.Add(Notes.A0, 3);
			song.Add(Notes.A0, 3);
			song.Add(Notes.A0, 2);
			song.Add(Notes.A0, 1);
			song.Add(Notes.A0, 2);
			song.Add(Notes.A0, 1);
			song.Add(Notes.A0, 4);
			//--------------------
		}
		#endregion
		#region KombatMain2_5
		private void KombatMain2_5() {
			//--------------------
			song.Add(Notes.A0, 1);
			song.Add(Notes.E1, 2);
			song.Add(Notes.A0, 1);
			song.Add(Notes.C1, 2);
			song.Add(Notes.A0, 1);
			song.Add(Notes.As0, 2);
			song.Add(Notes.A0, 1);
			song.Add(Notes.C1, 2);
			song.Add(Notes.As0, 2);
			song.Add(Notes.G0, 2);
			//--------------------
		}
		#endregion
		#region KombatMain3
		private void KombatMain3() {
			//--------------------
			song.Add(Notes.A1, 3);
			song.Add(Notes.E1, 3);
			song.Add(Notes.D1, 4);
			song.Add(Notes.Bf1, 2);
			song.Add(Notes.A1, 4);
			//--------------------
			song.Add(Notes.A1, 3);
			song.Add(Notes.E1, 3);
			song.Add(Notes.D1, 4);
			song.Add(Notes.Bf1, 2);
			song.Add(Notes.A1, 4);
			//--------------------
			song.Add(Notes.A1, 3);
			song.Add(Notes.E1, 3);
			song.Add(Notes.D1, 4);
			song.Add(Notes.Bf1, 2);
			song.Add(Notes.A1, 4);
			//--------------------
			song.Add(Notes.A1, 3);
			song.Add(Notes.E1, 3);
			song.Add(Notes.D1, 4);
			song.Add(Notes.Bf1, 2);
			song.Add(Notes.A1, 4);
			//--------------------
		}
		#region KombatMain4
		#endregion
		private void KombatMain4() {
			//--------------------
			song.Add(Notes.A0, 1);
			song.Add(Notes.E1, 2);
			song.Add(Notes.A0, 1);
			song.Add(Notes.C1, 2);
			song.Add(Notes.A0, 1);
			song.Add(Notes.As0, 2);
			song.Add(Notes.A0, 1);
			song.Add(Notes.C1, 2);
			song.Add(Notes.As0, 2);
			song.Add(Notes.G0, 2);
			//--------------------
			song.Add(Notes.A0, 1);
			song.Add(Notes.E1, 2);
			song.Add(Notes.A0, 1);
			song.Add(Notes.C1, 2);
			song.Add(Notes.A0, 1);
			song.Add(Notes.As0, 2);
			song.Add(Notes.A0, 1);
			song.Add(Notes.C1, 2);
			song.Add(Notes.As0, 2);
			song.Add(Notes.G0, 2);
			//--------------------
		}
		#endregion

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

		private void OnMountChanged(object sender, SelectionChangedEventArgs e) {
			if (!loaded)
				return;

			mount = comboBoxMount.SelectedIndex;
		}

		private void OnProjectileChanged(object sender, RoutedEventArgs e) {
			projectileAngle = projectileControl.Angle;
			projectileRange = projectileControl.Range;
		}
		#endregion

		#region Midi Playing
		private void Play() {
			if (midi != null) {
				TerrariaWindowLocator.Update();
				if (!TerrariaWindowLocator.HasFocus) {
					TerrariaWindowLocator.Focus();
					Thread.Sleep(400);
				}
				if (TerrariaWindowLocator.IsOpen) {
					clientArea = TerrariaWindowLocator.ClientArea;
					watch.Start();
					sequencer.Continue();
					checkCount = 0;
				}
				else {
					TriggerMessageBox.Show(this, MessageIcon.Error, "You cannot play a song when Terraria is not running!", "Terraria not Running");
				}
			}
		}

		private void Pause() {
			sequencer.Stop();
		}

		private void Stop() {
			watch.Stop();
			sequencer.Stop();
			sequencer.Position = 0;
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
				midi.Load(fileName);

				midis.Add(midi);
				listMidis.Items.Add("Loading...");
				listMidis.SelectedIndex = listMidis.Items.Count - 1;
				listMidis.ScrollIntoView(listMidis.Items[listMidis.SelectedIndex]);

				sequencer.Sequence = midi.Sequence;
				listMidis.Items[listMidis.SelectedIndex] = midi.ProperName;
				listMidis.SelectedIndex = listMidis.Items.Count - 1;
				UpdateMidi();
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
				loaded = true;
				if (newName != null) {
					midi.Name = newName;
					listMidis.Items[listMidis.SelectedIndex] = newName;
				}
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
			sequencer.AltTempo = 100.0 / (double)midi.Speed;
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
				//labelDuration.Content = "Duration: " + MillisecondsToString(sequencer.Duration);
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
		private void OnChangeKeybinds(object sender, RoutedEventArgs e) {
			ChangeKeybindsDialog.ShowDialog(this, ref keybindPlay, ref keybindPause, ref keybindStop, ref keybindClose);
		}

		private void OnExit(object sender, RoutedEventArgs e) {
			Close();
		}
		#endregion

		private void OnOpenOnGitHub(object sender, RoutedEventArgs e) {
			Process.Start("https://github.com/trigger-death/TerrariaMidiPlayer");
		}
	}
}
