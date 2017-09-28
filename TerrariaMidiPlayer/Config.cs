using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml;
using Sanford.Multimedia.Midi;
using TerrariaMidiPlayer.Util;
using TerrariaMidiPlayer.Windows;

namespace TerrariaMidiPlayer {
	/**<summary>Data about mounts.</summary>*/
	public struct Mount {
		//========== CONSTANTS ===========
		#region Constants

		/**<summary>The list of mounts in the game. Drill mount is excluded.</summary>*/
		public static readonly Mount[] Mounts = {
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

		#endregion
		//=========== MEMBERS ============
		#region Members

		/**<summary>The name of the mount.</summary>*/
		public string Name;
		/**<summary>The height offset of the mount in 1x1 pixels.</summary>*/
		public int Offset;

		#endregion
		//========= CONSTRUCTORS =========
		#region Constructors

		/**<summary>Constructs a mount with a name and offset.</summary>*/
		public Mount(string name, int offset) {
			Name = name;
			Offset = offset;
		}

		#endregion
	}

	/**<summary>The type of page the sync tab is showing.</summary>*/
	public enum SyncTypes { Client, Host }

	/**<summary>The list of config settings for Terraria Midi Player.</summary>*/
	public static class Config {
		//=========== CLASSES ============
		#region Classes

		/** <summary>The subclass of available keybinds and keybind settings.</summary> */
		public static class Keybinds {
			//=========== MEMBERS ============
			#region Members

			/**<summary>Starts playing the midi.</summary>*/
			public static Keybind Play { get; set; } = new Keybind(Key.NumPad0);
			/**<summary>Pauses the midi.</summary>*/
			public static Keybind Pause { get; set; } = new Keybind(Key.NumPad1);
			/**<summary>Stops the midi.</summary>*/
			public static Keybind Stop { get; set; } = new Keybind(Key.NumPad2);
			/**<summary>Closes the window.</summary>*/
			public static Keybind Close { get; set; } = new Keybind(Key.Add);
			/**<summary>Toggles the mounted state.</summary>*/
			public static Keybind Mount { get; set; } = new Keybind(Key.R);

			#endregion
		}

		/**<summary>The subclass of available syncing settings.</summary>*/
		public static class Syncing {
			//=========== MEMBERS ============
			#region Members

			/**<summary>The currently open page for syncing.</summary>*/
			public static SyncTypes SyncType { get; set; } = SyncTypes.Client;

			//--------------------------------
			#region Client

			/**<summary>The input IP address for the client.</summary>*/
			public static string ClientIPAddress { get; set; } = "";
			/**<summary>The input port for the client.</summary> */
			public static int ClientPort { get; set; } = 0;
			/**<summary>The input username for the client.</summary>*/
			public static string ClientUsername { get; set; } = "";
			/**<summary>The input password for the client.</summary>*/
			public static string ClientPassword { get; set; } = "";
			/**<summary>The input time offset before playing a song in sync.</summary>*/
			public static int ClientTimeOffset { get; set; } = 0;

			#endregion
			//--------------------------------
			#region Host

			/**<summary>The input port for the host.</summary>*/
			public static int HostPort { get; set; } = 0;
			/**<summary>The input password for the host.</summary>*/
			public static string HostPassword { get; set; } = "";
			/**<summary>The time to wait after sending the signal before everyone begins playing.</summary>*/
			public static int HostWait { get; set; } = 5000;

			#endregion
			//--------------------------------
			#endregion
		}

		#endregion
		//========== CONSTANTS ===========
		#region Constants

		/**<summary>The version of the config file.</summary>*/
		public const int ConfigVersion = 1;
		/**<summary>The path to the config file.</summary>*/
		public static readonly string OldConfigPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Config.xml");
		/**<summary>The path to the config file.</summary>*/
		public static readonly string ConfigPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "TerrariaMidiPlayer.xml");

		#endregion
		//=========== MEMBERS ============
		#region Members
		//--------------------------------
		#region General

		/**<summary>An accessor for the main window.</summary>*/
		public static MainWindow MainWindow { get; private set; } = null;

		/**<summary>The list of loaded midis.</summary>*/
		public static List<Midi> Midis { get; } = new List<Midi>();
		/**<summary>The index of the currently selected midi.</summary>*/
		public static int MidiIndex { get; set; } = -1;
		/**<summary>The currently selected midi.</summary>*/
		public static Midi Midi {
			get { return Midis.ElementAtOrDefault(MidiIndex); }
		}
		/**<summary>True if a midi is selected.</summary>*/
		public static bool HasMidi {
			get { return MidiIndex != -1; }
		}
		/**<summary>The number of midis in the list.</summary>*/
		public static int MidiCount {
			get { return Midis.Count; }
		}
		/**<summary>True if playback is done within the program instead of Terraria.</summary>*/
		public static bool PianoMode { get; set; } = false;
		/**<summary>True if playback is done within the program instead of Terraria.</summary>*/
		public static Sequencer Sequencer { get; } = new Sequencer();
		/**<summary>The output device for previewing midis.</summary>*/
		public static OutputDevice OutputDevice { get; } = new OutputDevice(0);

		/**<summary>The names of the Terraria executable. Used to obtain the process. Separated by newlines.</summary>*/
		public static string ExecutableNames { get; set; } = "Terraria";

		/**<summary>True if the program does not require focus on itself or in Terraria for the close keybind.</summary>*/
		public static bool CloseNoFocus { get; set; } = false;
		/**<summary>True if the program does not require focus on itself or in Terraria for the playback keybinds.</summary>*/
		public static bool PlaybackNoFocus { get; set; } = false;
		/**<summary>True if the mount keybind is disabled while the enter key is toggled.</summary>*/
		public static bool DisableMountWhenTalking { get; set; } = false;

		/**<summary>True if midi track names are used by default.</summary>*/
		public static bool UseTrackNames { get; set; } = false;
		/**<summary>True if notes are wrapped when in piano mode.</summary>*/
		public static bool WrapPianoMode { get; set; } = true;
		/**<summary>True if notes are skipped when in piano mode.</summary>*/
		public static bool SkipPianoMode { get; set; } = true;

		/**<summary>Gets the last exception from Load or Save.</summary>*/
		public static Exception LastException { get; private set; } = null;

		#endregion
		//--------------------------------
		#region Playback

		/**<summary>The determined use time of the instrument in frames.</summary>*/
		public static int UseTime { get; set; } = 11;
		/**<summary>The time in milliseconds to hold down the mouse when playing a note.</summary>*/
		public static int ClickTime { get; set; } = 40;
		/**<summary>True if the program regularly checks if Terraria is still open when playing a note.</summary>*/
		public static bool ChecksEnabled { get; set; } = true;
		/**<summary>The number of notes to skip before performing another check.</summary>*/
		public static int CheckFrequency { get; set; } = 20;
		/**<summary>The index of the currently selected mount.</summary>*/
		public static int MountIndex { get; set; } = 0;
		/**<summary>The currently selected mount.</summary>*/
		public static Mount Mount {
			get { return Mount.Mounts[MountIndex]; }
		}

		/**<summary>The center angle of the projectile aiming.</summary>*/
		public static double ProjectileAngle { get; set; } = 0;
		/**<summary>The range of the projectile aiming.</summary>*/
		public static double ProjectileRange { get; set; } = 360;

		#endregion
		//--------------------------------
		#endregion
		//=========== LOADING ============
		#region Loading

		/**<summary>Initializes the config settings.</summary>*/
		public static void Initialize(MainWindow mainWindow) {
			MainWindow = mainWindow;
			if (!CppImports.KeyboardHasNumpad) {
				Keybinds.Play = new Keybind(Key.Delete);
				Keybinds.Pause = new Keybind(Key.End);
				Keybinds.Stop = new Keybind(Key.PageDown);
				Keybinds.Close = new Keybind(Key.PageUp);
			}
		}
		/**<summary>Checks if a config file exists.</summary>*/
		public static bool ConfigExists() {
			return File.Exists(ConfigPath) || File.Exists(OldConfigPath);
		}
		/**<summary>Loads the settings from the config file.</summary>*/
		public static bool Load() {
			try {
				XmlNode node;
				XmlElement element;
				XmlAttribute attribute;
				XmlDocument doc = new XmlDocument();
				bool deleteOldConfig = false;
				if (File.Exists(ConfigPath)) {
					doc.Load(ConfigPath);
				}
				else {
					doc.Load(OldConfigPath);
					if (doc.SelectSingleNode("TerrariaMidiPlayer") != null)
						deleteOldConfig = true;
				}

				int intValue;
				ushort ushortValue;
				bool boolValue;
				double doubleValue;
				Keybind keybindValue;
				
				node = doc.SelectSingleNode("TerrariaMidiPlayer/Version");
				if (node == null || !int.TryParse(node.InnerText, out intValue) || intValue > ConfigVersion || intValue <= 0)
					return false;
				
				#region Settings

				node = doc.SelectSingleNode("TerrariaMidiPlayer/Settings/ExecutableName");
				if (node != null)
					ExecutableNames = node.InnerText;

				node = doc.SelectSingleNode("TerrariaMidiPlayer/Settings/UseTime");
				if (node != null && int.TryParse(node.InnerText, out intValue))
					UseTime = intValue;

				node = doc.SelectSingleNode("TerrariaMidiPlayer/Settings/ClickTime");
				if (node != null && int.TryParse(node.InnerText, out intValue))
					ClickTime = intValue;

				node = doc.SelectSingleNode("TerrariaMidiPlayer/Settings/ChecksEnabled");
				if (node != null && bool.TryParse(node.InnerText, out boolValue))
					ChecksEnabled = boolValue;

				node = doc.SelectSingleNode("TerrariaMidiPlayer/Settings/CheckFrequency");
				if (node != null && int.TryParse(node.InnerText, out intValue))
					CheckFrequency = intValue;

				node = doc.SelectSingleNode("TerrariaMidiPlayer/Settings/Mount");
				if (node != null) {
					for (int i = 0; i < Mount.Mounts.Length; i++) {
						if (string.Compare(node.InnerText, Mount.Mounts[i].Name, true) == 0) {
							MountIndex = i;
							break;
						}
					}
				}

				node = doc.SelectSingleNode("TerrariaMidiPlayer/Settings/ProjectileAngle");
				if (node != null && double.TryParse(node.InnerText, out doubleValue))
					ProjectileAngle = doubleValue;

				node = doc.SelectSingleNode("TerrariaMidiPlayer/Settings/ProjectileRange");
				if (node != null && double.TryParse(node.InnerText, out doubleValue))
					ProjectileRange = doubleValue;

				node = doc.SelectSingleNode("TerrariaMidiPlayer/Settings/CloseNoFocus");
				if (node != null && bool.TryParse(node.InnerText, out boolValue))
					CloseNoFocus = boolValue;

				node = doc.SelectSingleNode("TerrariaMidiPlayer/Settings/PlaybackNoFocus");
				if (node != null && bool.TryParse(node.InnerText, out boolValue))
					PlaybackNoFocus = boolValue;

				node = doc.SelectSingleNode("TerrariaMidiPlayer/Settings/DisableMountWhenTalking");
				if (node != null && bool.TryParse(node.InnerText, out boolValue))
					DisableMountWhenTalking = boolValue;

				node = doc.SelectSingleNode("TerrariaMidiPlayer/Settings/UseTrackNames");
				if (node != null && bool.TryParse(node.InnerText, out boolValue))
					UseTrackNames = boolValue;

				node = doc.SelectSingleNode("TerrariaMidiPlayer/Settings/WrapPianoMode");
				if (node != null && bool.TryParse(node.InnerText, out boolValue))
					WrapPianoMode = boolValue;

				node = doc.SelectSingleNode("TerrariaMidiPlayer/Settings/SkipPianoMode");
				if (node != null && bool.TryParse(node.InnerText, out boolValue))
					SkipPianoMode = boolValue;

				#endregion
				//--------------------------------
				#region Keybinds

				node = doc.SelectSingleNode("TerrariaMidiPlayer/Settings/Keybinds/Play");
				if (node != null && Keybind.TryParse(node.InnerText, out keybindValue))
					Keybinds.Play = keybindValue;

				node = doc.SelectSingleNode("TerrariaMidiPlayer/Settings/Keybinds/Pause");
				if (node != null && Keybind.TryParse(node.InnerText, out keybindValue))
					Keybinds.Pause = keybindValue;

				node = doc.SelectSingleNode("TerrariaMidiPlayer/Settings/Keybinds/Stop");
				if (node != null && Keybind.TryParse(node.InnerText, out keybindValue))
					Keybinds.Stop = keybindValue;

				node = doc.SelectSingleNode("TerrariaMidiPlayer/Settings/Keybinds/Close");
				if (node != null && Keybind.TryParse(node.InnerText, out keybindValue)) {
					Keybinds.Close = keybindValue;
				}

				node = doc.SelectSingleNode("TerrariaMidiPlayer/Settings/Keybinds/Mount");
				if (node != null && Keybind.TryParse(node.InnerText, out keybindValue))
					Keybinds.Mount = keybindValue;

				#endregion
				//--------------------------------
				#region Syncing

				node = doc.SelectSingleNode("TerrariaMidiPlayer/Settings/Syncing/SyncType");
				if (node != null)
					Syncing.SyncType = ((string.Compare(node.InnerText, "Host", true) == 0) ? SyncTypes.Host : SyncTypes.Client);
				
				node = doc.SelectSingleNode("TerrariaMidiPlayer/Settings/Syncing/ClientIPAddress");
				if (node != null) Syncing.ClientIPAddress = node.InnerText;
				
				node = doc.SelectSingleNode("TerrariaMidiPlayer/Settings/Syncing/ClientPort");
				if (node != null && ushort.TryParse(node.InnerText, out ushortValue))
					Syncing.ClientPort = ushortValue;

				node = doc.SelectSingleNode("TerrariaMidiPlayer/Settings/Syncing/ClientUsername");
				if (node != null) Syncing.ClientUsername = node.InnerText;

				node = doc.SelectSingleNode("TerrariaMidiPlayer/Settings/Syncing/ClientPassword");
				if (node != null) Syncing.ClientPassword = node.InnerText;
				
				node = doc.SelectSingleNode("TerrariaMidiPlayer/Settings/Syncing/ClientTimeOffset");
				if (node != null && int.TryParse(node.InnerText, out intValue))
					Syncing.ClientTimeOffset = intValue;
				
				node = doc.SelectSingleNode("TerrariaMidiPlayer/Settings/Syncing/HostPort");
				if (node != null && ushort.TryParse(node.InnerText, out ushortValue))
					Syncing.HostPort = ushortValue;

				node = doc.SelectSingleNode("TerrariaMidiPlayer/Settings/Syncing/HostPassword");
				if (node != null) Syncing.HostPassword = node.InnerText;
				
				node = doc.SelectSingleNode("TerrariaMidiPlayer/Settings/Syncing/HostWait");
				if (node != null && int.TryParse(node.InnerText, out intValue))
					Syncing.HostWait = intValue;

				#endregion
				//--------------------------------
				#region Midis

				XmlNodeList midiList = doc.SelectNodes("TerrariaMidiPlayer/Midis/Midi");
				for (int i = 0; i < midiList.Count; i++) {
					node = midiList[i];
					Midi midi = new Midi();
					
					element = node["FilePath"];
					if (element != null) {
						if (midi.Load(element.InnerText)) {
							element = node["Name"];
							if (element != null) midi.Name = element.InnerText;

							element = node["NoteOffset"];
							if (element != null && int.TryParse(element.InnerText, out intValue))
								midi.NoteOffset = Math.Max(-11, Math.Min(11, intValue));

							element = node["Speed"];
							if (element != null && int.TryParse(element.InnerText, out intValue))
								midi.Speed = intValue;

							element = node["Keybind"];
							if (element != null && Keybind.TryParse(element.InnerText, out keybindValue))
								midi.Keybind = keybindValue;

							if (node.Attributes["Selected"] != null &&
								bool.TryParse(midiList[i].Attributes["Selected"].Value, out boolValue) && boolValue)
								MidiIndex = i;

							element = node["Tracks"];
							if (element != null) {
								XmlNodeList trackList = element.SelectNodes("Track");
								for (int j = 0; j < trackList.Count && trackList.Count == midi.TrackCount; j++) {
									node = trackList[j];

									attribute = node.Attributes["Name"];
									if (attribute != null)
										midi.GetTrackSettingsAt(j).Name = attribute.Value;

									attribute = node.Attributes["Enabled"];
									if (attribute != null && bool.TryParse(attribute.Value, out boolValue))
										midi.GetTrackSettingsAt(j).Enabled = boolValue;

									attribute = node.Attributes["OctaveOffset"];
									if (attribute != null && int.TryParse(attribute.Value, out intValue))
										midi.GetTrackSettingsAt(j).OctaveOffset = Math.Max(-1, Math.Min(8, intValue));
								}
							}

							Midis.Add(midi);
						}
						else {
							// Error
						}
					}
				}
				if (Midis.Count > 0 && MidiIndex == -1)
					MidiIndex = 0;

				#endregion

				if (deleteOldConfig && Save()) {
					try {
						File.Delete(OldConfigPath);
					}
					catch { }
				}
			}
			catch (Exception ex) {
				LastException = ex;
				return false;
			}
			return true;
		}
		/**<summary>Saves the settings to the config file.</summary>*/
		public static bool Save() {
			try {
				XmlElement element;
				XmlDocument doc = new XmlDocument();
				doc.AppendChild(doc.CreateXmlDeclaration("1.0", "UTF-8", null));

				XmlElement midiPlayer = doc.CreateElement("TerrariaMidiPlayer");
				doc.AppendChild(midiPlayer);

				XmlElement version = doc.CreateElement("Version");
				version.AppendChild(doc.CreateTextNode(ConfigVersion.ToString()));
				midiPlayer.AppendChild(version);

				#region Settings

				XmlElement setting = doc.CreateElement("Settings");
				midiPlayer.AppendChild(setting);

				element = doc.CreateElement("ExecutableName");
				element.AppendChild(doc.CreateTextNode(ExecutableNames));
				setting.AppendChild(element);

				element = doc.CreateElement("UseTime");
				element.AppendChild(doc.CreateTextNode(UseTime.ToString()));
				setting.AppendChild(element);

				element = doc.CreateElement("ClickTime");
				element.AppendChild(doc.CreateTextNode(ClickTime.ToString()));
				setting.AppendChild(element);

				element = doc.CreateElement("ChecksEnabled");
				element.AppendChild(doc.CreateTextNode(ChecksEnabled.ToString()));
				setting.AppendChild(element);

				element = doc.CreateElement("CheckFrequency");
				element.AppendChild(doc.CreateTextNode(CheckFrequency.ToString()));
				setting.AppendChild(element);

				element = doc.CreateElement("Mount");
				element.AppendChild(doc.CreateTextNode(Mount.Name));
				setting.AppendChild(element);

				element = doc.CreateElement("ProjectileAngle");
				element.AppendChild(doc.CreateTextNode(ProjectileAngle.ToString()));
				setting.AppendChild(element);

				element = doc.CreateElement("ProjectileRange");
				element.AppendChild(doc.CreateTextNode(ProjectileRange.ToString()));
				setting.AppendChild(element);

				element = doc.CreateElement("CloseNoFocus");
				element.AppendChild(doc.CreateTextNode(CloseNoFocus.ToString()));
				setting.AppendChild(element);

				element = doc.CreateElement("PlaybackNoFocus");
				element.AppendChild(doc.CreateTextNode(PlaybackNoFocus.ToString()));
				setting.AppendChild(element);

				element = doc.CreateElement("DisableMountWhenTalking");
				element.AppendChild(doc.CreateTextNode(DisableMountWhenTalking.ToString()));
				setting.AppendChild(element);

				element = doc.CreateElement("UseTrackNames");
				element.AppendChild(doc.CreateTextNode(UseTrackNames.ToString()));
				setting.AppendChild(element);

				element = doc.CreateElement("WrapPianoMode");
				element.AppendChild(doc.CreateTextNode(WrapPianoMode.ToString()));
				setting.AppendChild(element);

				element = doc.CreateElement("SkipPianoMode");
				element.AppendChild(doc.CreateTextNode(SkipPianoMode.ToString()));
				setting.AppendChild(element);

				#endregion
				//--------------------------------
				#region Keybinds

				XmlElement keybinds = doc.CreateElement("Keybinds");
				setting.AppendChild(keybinds);

				element = doc.CreateElement("Play");
				element.AppendChild(doc.CreateTextNode(Keybinds.Play.ToString()));
				keybinds.AppendChild(element);

				element = doc.CreateElement("Pause");
				element.AppendChild(doc.CreateTextNode(Keybinds.Pause.ToString()));
				keybinds.AppendChild(element);

				element = doc.CreateElement("Stop");
				element.AppendChild(doc.CreateTextNode(Keybinds.Stop.ToString()));
				keybinds.AppendChild(element);

				element = doc.CreateElement("Close");
				element.AppendChild(doc.CreateTextNode(Keybinds.Close.ToString()));
				keybinds.AppendChild(element);

				element = doc.CreateElement("Mount");
				element.AppendChild(doc.CreateTextNode(Keybinds.Mount.ToString()));
				keybinds.AppendChild(element);

				#endregion
				//--------------------------------
				#region Syncing

				XmlElement syncing = doc.CreateElement("Syncing");
				setting.AppendChild(syncing);

				element = doc.CreateElement("SyncType");
				element.AppendChild(doc.CreateTextNode(Syncing.SyncType.ToString()));
				syncing.AppendChild(element);

				element = doc.CreateElement("ClientIPAddress");
				element.AppendChild(doc.CreateTextNode(Syncing.ClientIPAddress));
				syncing.AppendChild(element);

				element = doc.CreateElement("ClientPort");
				element.AppendChild(doc.CreateTextNode(Syncing.ClientPort.ToString()));
				syncing.AppendChild(element);

				element = doc.CreateElement("ClientUsername");
				element.AppendChild(doc.CreateTextNode(Syncing.ClientUsername));
				syncing.AppendChild(element);

				element = doc.CreateElement("ClientPassword");
				element.AppendChild(doc.CreateTextNode(Syncing.ClientPassword));
				syncing.AppendChild(element);

				element = doc.CreateElement("ClientTimeOffset");
				element.AppendChild(doc.CreateTextNode(Syncing.ClientTimeOffset.ToString()));
				syncing.AppendChild(element);

				element = doc.CreateElement("HostPort");
				element.AppendChild(doc.CreateTextNode(Syncing.HostPort.ToString()));
				syncing.AppendChild(element);

				element = doc.CreateElement("HostPassword");
				element.AppendChild(doc.CreateTextNode(Syncing.HostPassword));
				syncing.AppendChild(element);

				element = doc.CreateElement("HostWait");
				element.AppendChild(doc.CreateTextNode(Syncing.HostWait.ToString()));
				syncing.AppendChild(element);

				#endregion
				//--------------------------------
				#region Midis

				XmlElement midiList = doc.CreateElement("Midis");
				midiPlayer.AppendChild(midiList);
				
				for (int i = 0; i < Midis.Count; i++) {
					Midi midi = Midis[i];
					XmlElement midiElement = doc.CreateElement("Midi");
					midiList.AppendChild(midiElement);

					if (midi == Midi)
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

					for (int j = 0; j < midi.TrackCount; j++) {
						Midi.TrackSettings trackSettings = midi.GetTrackSettingsAt(j);
						XmlElement track = doc.CreateElement("Track");
						tracks.AppendChild(track);

						if (trackSettings.Name != "")
							track.SetAttribute("Name", trackSettings.Name);
						track.SetAttribute("Enabled", trackSettings.Enabled.ToString());
						track.SetAttribute("OctaveOffset", trackSettings.OctaveOffset.ToString());
					}
				}

				#endregion

				doc.Save(ConfigPath);
			}
			catch (Exception ex) {
				LastException = ex;
				return false;
			}
			return true;
		}

		#endregion
	}
}
