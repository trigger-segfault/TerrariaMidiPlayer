using System;
using System.Collections.Generic;
using System.Linq;
using Sanford.Multimedia.Midi;
using System.Windows.Input;
using System.IO;
using System.Security.Cryptography;
using System.Xml;
using IOPath = System.IO.Path;
using TextPlayer.ABC;

namespace TerrariaMidiPlayer.Util {
	/**<summary>The list of notes available in Terraria.</summary>*/
	public enum Notes {
		C0 = 0,
		Cs0 = 1, Db0 = 1,
		D0 = 2,
		Ds0 = 3, Eb0 = 3,
		E0 = 4,
		F0 = 5,
		Fs0 = 6, Gb0 = 6,
		G0 = 7,
		Gs0 = 8, Ab0 = 8,
		A0 = 9,
		As0 = 10, Bb0 = 10,
		B0 = 11,

		C1 = 12,
		Cs1 = 13, Db1 = 13,
		D1 = 14,
		Ds1 = 15, Eb1 = 15,
		E1 = 16,
		F1 = 17,
		Fs1 = 18, Gb1 = 18,
		G1 = 19,
		Gs1 = 20, Ab1 = 20,
		A1 = 21,
		As1 = 22, Bb1 = 22,
		B1 = 23,

		C2 = 24
	}

	/**<summary>Keeps track of settings and data about the loaded midi.</summary>*/
	public class Midi {
		//=========== CLASSES ============
		#region Classes

		/**<summary>Data about a given track.</summary>*/
		public struct TrackData {
			/**<summary>The highest note in the track.</summary>*/
			public int HighestNote;
			/**<summary>The lowest note in the track.</summary>*/
			public int LowestNote;
			/**<summary>The number of chords played in the track.</summary>*/
			public int Chords;
			/**<summary>The number of notes in the track.</summary>*/
			public int Notes;
			/**<summary>The track object used for referencing.</summary>*/
			public Track TrackObj;
		}

		/**<summary>Settings about a given track.</summary>*/
		public class TrackSettings {
			/**<summary>True if the track should play.</summary>*/
			public bool Enabled;
			/**<summary>The octave offset of the track.</summary>*/
			public int OctaveOffset;
			/**<summary>The index of the track.</summary>*/
			public int Index;
			/**<summary>The custom name of the track.</summary>*/
			public string Name;
			/**<summary>The proper name of the track.</summary>*/
			public string ProperName {
				get {
					if (string.IsNullOrWhiteSpace(Name)) {
						if (!Config.UseTrackNames || string.IsNullOrWhiteSpace(TrackName))
							return "Track " + (Index + 1).ToString();
						else
							return TrackName;
					}
					else {
						return Name;
					}
				}
			}
			/**<summary>The real name of the track.</summary>*/
			public string TrackName;

			/**<summary>Constructs the default track settings.</summary>*/
			public TrackSettings() {
				Enabled = true;
				OctaveOffset = 4;
				Index = 0;
				Name = "";
				TrackName = "";
			}
		}

		#endregion
		//=========== MEMBERS ============
		#region Members

		/**<summary>The file path of the track.</summary>*/
		private string path = "";
		/**<summary>The custom name of the track.</summary>*/
		private string name = "";
		/**<summary>The sequence object of the track.</summary>*/
		private Sequence sequence = new Sequence();
		/**<summary>The list of track datas.</summary>*/
		private List<TrackData> tracks = new List<TrackData>();
		/**<summary>The list of track settings.</summary>*/
		private List<TrackSettings> trackSettings = new List<TrackSettings>();
		/**<summary>The note offset in semitones when playing.</summary>*/
		private int noteOffset = 0;
		/**<summary>The speed of the song as a percentage.</summary>*/
		private int speed = 100;
		/**<summary>The keybind for the midi.</summary>*/
		private Keybind keybind = Keybind.None;
		/**<summary>The last exception when loading.</summary>*/
		private Exception exception = null;

		#endregion
		//========= CONSTRUCTORS =========
		#region Constructors

		/**<summary>Constructs the default midi.</summary>*/
		public Midi() {
			
		}

		#endregion
		//=========== LOADING ============
		#region Loading

		/**<summary>Loads the midi file located at the specified file path.</summary>*/
		public bool Load(string path) {
			name = "";
			tracks.Clear();
			trackSettings.Clear();
			noteOffset = 0;
			speed = 100;
			try {
				string ext = IOPath.GetExtension(path).ToLower();
				if (ext == ".abc") {
					sequence = ABCConverter.CreateSequenceFromABCFile(path);
				}
				else {
					sequence.Load(path);
				}
				this.path = path;
				int index = 0;
				foreach (Track track in sequence) {
					TrackSettings settings = new TrackSettings();
					TrackData trackData = new TrackData();
					trackData.TrackObj = track;
					settings.Index = index;
					if (!string.IsNullOrWhiteSpace(track.Name))
						settings.TrackName = track.Name;

					int lastTick = -1;
					bool chord = false;

					// Scan all of the notes
					for (int i = 0; i < track.Count; i++) {
						var midiEvent = track.GetMidiEvent(i);
						if (midiEvent.MidiMessage.MessageType == MessageType.Channel) {
							var message = midiEvent.MidiMessage as ChannelMessage;
							if (message.Data2 > 0 && message.Command == ChannelCommand.NoteOn) {
								// Check for chords
								int newTick = midiEvent.AbsoluteTicks;
								if (newTick == lastTick) {
									if (!chord) {
										trackData.Chords++;
										chord = true;
									}
								}
								else {
									lastTick = newTick;
									chord = false;
								}

								// Increment note count
								trackData.Notes++;

								// Check for highest and lowest note
								if (message.Data1 < trackData.LowestNote || trackData.LowestNote == 0)
									trackData.LowestNote = message.Data1;
								if (message.Data1 > trackData.HighestNote)
									trackData.HighestNote = message.Data1;
							}
						}
					}

					if (trackData.Notes > 0) {
						// Guess a good octave offset
						int lowestOffset = trackData.LowestNote % 12;
						int highestOffset = (trackData.HighestNote - 1) % 12;
						if (lowestOffset + (trackData.HighestNote - trackData.LowestNote) <= 25) {
							settings.OctaveOffset = trackData.LowestNote / 12 - 1;
						}
						else if (lowestOffset > highestOffset) {
							settings.OctaveOffset = (trackData.LowestNote + 11) / 12 - 1;
						}
						else {
							settings.OctaveOffset = (trackData.HighestNote + 11 - 1) / 12 - 2 - 1;
						}
						
						// Add the track data and settings
						tracks.Add(trackData);
						trackSettings.Add(settings);
						index++;
					}
				}
				exception = null;
				return true;
			}
			catch (Exception e) {
				exception = e;
				this.path = "";
				return false;
			}
		}

		#endregion
		//========== PROPERTIES ==========
		#region Properties

		/**<summary>The file path of the track.</summary>*/
		public string Path {
			get { return path; }
		}
		/**<summary>The custom name of the track.</summary>*/
		public string Name {
			get { return name; }
			set { name = value; }
		}
		/**<summary>The custom name of the track whether it be the name or filename.</summary>*/
		public string ProperName {
			get { return (name.Length > 0 ? name : IOPath.GetFileName(path)); }
		}
		/**<summary>The sequence object of the track.</summary>*/
		public Sequence Sequence {
			get { return sequence; }
		}
		/**<summary>The number of tracks in the midi.</summary>*/
		public int TrackCount {
			get { return tracks.Count; }
		}
		/**<summary>The note offset in semitones when playing.</summary>*/
		public int NoteOffset {
			get { return noteOffset; }
			set { noteOffset = value; }
		}
		/**<summary>The speed of the song as a percentage.</summary>*/
		public int Speed {
			get { return speed; }
			set {
				value = Math.Min(10000, Math.Max(1, value));
				speed = value;
			}
		}
		/**<summary>The speed of the song as a double.</summary>*/
		public double SpeedRatio {
			get { return 100.0 / (double)speed; }
		}
		/**<summary>The total number of notes in the song.</summary>*/
		public int TotalNotes {
			get {
				int notes = 0;
				for (int i = 0; i < tracks.Count; i++)
					notes += tracks[i].Notes;
				return notes;
			}
		}
		/**<summary>The keybind for the midi.</summary>*/
		public Keybind Keybind {
			get { return keybind; }
			set { keybind = value; }
		}
		/**<summary>The last exception when loading.</summary>*/
		public Exception LoadException {
			get { return exception; }
		}
		/**<summary>True if the midi is an ABC notation file.</summary>*/
		public bool IsABC {
			get { return IOPath.GetExtension(path).ToLower() == ".abc"; }
		}

		#endregion
		//============ OTHERS ============
		#region Others

		/**<summary>The track at the specified index.</summary>*/
		public TrackData GetTrackAt(int index) {
			return tracks[index];
		}
		/**<summary>The track settings at the specified index.</summary>*/
		public TrackSettings GetTrackSettingsAt(int index) {
			return trackSettings[index];
		}
		/**<summary>The track with the specified object.</summary>*/
		public TrackData GetTrackByTrackObj(Track track) {
			return tracks.Find(t => t.TrackObj == track);
		}
		/**<summary>The track settings with the specified object.</summary>*/
		public TrackSettings GetTrackSettingsByTrackObj(Track track) {
			int index = tracks.FindIndex(t => t.TrackObj == track);
			if (index != -1)
				return trackSettings[index];
			return null;
		}
		/**<summary>Returns true if the midi contains a playable track object.</summary>*/
		public bool ContainsTrackObj(Track track) {
			return tracks.FindIndex(t => t.TrackObj == track) != -1;
		}

		/**<summary>Tests if the message in the midi should be played.</summary>*/
		public bool IsMessagePlayable(ChannelMessageEventArgs e) {
			return (e.Message.Data2 > 0 && e.Message.Command == ChannelCommand.NoteOn &&
					ContainsTrackObj(e.Track) && GetTrackSettingsByTrackObj(e.Track).Enabled);
		}

		#endregion
	}
}
