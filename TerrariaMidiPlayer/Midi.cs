using System;
using System.Collections.Generic;
using System.Linq;
using Sanford.Multimedia.Midi;
using System.Windows.Input;
using System.IO;
using System.Security.Cryptography;
using System.Xml;

namespace TerrariaMidiPlayer {
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

	public class Midi {
		public struct TrackData {
			public int HighestNote;
			public int LowestNote;
			public int Notes;
			public int Index;
			public Track Track;
		}

		public class TrackSettings {
			public bool Enabled;
			public int OctaveOffset;

			public TrackSettings() {
				Enabled = true;
				OctaveOffset = 4;
			}
		}

		private string name;
		private string path;
		private Sequence sequence;
		private List<TrackData> tracks;
		private List<TrackSettings> trackSettings;
		private int noteOffset;
		private int speed;
		private Keybind keybind;
		private Exception exception;

		public Midi() {
			name = "";
			path = "";
			sequence = new Sequence();
			tracks = new List<TrackData>();
			trackSettings = new List<TrackSettings>();
			noteOffset = 0;
			keybind = new Keybind();
			exception = null;
		}

		public bool Load(string path) {
			name = "";
			tracks.Clear();
			trackSettings.Clear();
			noteOffset = 0;
			speed = 100;
			try {
				sequence.Load(path);
				this.path = path;
				int index = 0;
				foreach (Track track in sequence) {
					TrackData trackData = new TrackData();
					trackData.Track = track;
					TrackSettings settings = new TrackSettings();
					trackData.Index = index;
					for (int i = 0; i < track.Count; i++) {
						var midiEvent = track.GetMidiEvent(i);
						if (midiEvent.MidiMessage.MessageType == MessageType.Channel) {
							var message = midiEvent.MidiMessage as ChannelMessage;
							if (message.Data2 > 0 && message.Command == ChannelCommand.NoteOn) {
								trackData.Notes++;
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

		public bool LoadConfig(XmlNode midiNode) {
			try {
				XmlElement element;
				string path = "";
				element = midiNode["FilePath"];
				if (element != null)
					path = element.InnerText;
				if (!System.IO.File.Exists(path))
					return false;

				if (!Load(path))
					return false;
				
				element = midiNode["Name"];
				if (element != null) name = element.InnerText;

				element = midiNode["NoteOffset"];
				if (element != null) int.TryParse(element.InnerText, out noteOffset);

				element = midiNode["Speed"];
				if (element != null && !int.TryParse(element.InnerText, out speed))
					speed = 100;

				element = midiNode["Keybind"];
				if (element != null) Keybind.TryParse(element.InnerText, out keybind);


				element = midiNode["Tracks"];
				if (element != null) {
					XmlNodeList trackList = element.SelectNodes("Track");
					for (int j = 0; j < trackList.Count && tracks.Count == trackList.Count; j++) {
						if (trackList[j].Attributes["Enabled"] != null)
							bool.TryParse(trackList[j].Attributes["Enabled"].Value, out trackSettings[j].Enabled);
						if (trackList[j].Attributes["OctaveOffset"] != null)
							int.TryParse(trackList[j].Attributes["OctaveOffset"].Value, out trackSettings[j].OctaveOffset);
					}
				}
				return true;
			}
			catch {
				return false;
			}
		}

		public string Name {
			get { return name; }
			set { name = value; }
		}
		public string ProperName {
			get { return (name.Length > 0 ? name : System.IO.Path.GetFileName(path)); }
		}
		public string Path {
			get { return path; }
		}
		public Sequence Sequence {
			get { return sequence; }
		}
		public int TrackCount {
			get { return tracks.Count; }
		}
		public TrackData GetTrack(int index) {
			return tracks[index];
		}
		public TrackSettings GetTrackSettings(int index) {
			return trackSettings[index];
		}
		public TrackData GetTrackByTrackObj(Track track) {
			return tracks.Find(t => t.Track == track);
		}
		public TrackSettings GetTrackSettingsByTrackObj(Track track) {
			int index = tracks.FindIndex(t => t.Track == track);
			if (index != -1)
				return trackSettings[index];
			return null;
		}
		public bool ContainsTrackObj(Track track) {
			return tracks.FindIndex(t => t.Track == track) != -1;
		}
		public int NoteOffset {
			get { return noteOffset; }
			set { noteOffset = value; }
		}
		public int Speed {
			get { return speed; }
			set {
				value = Math.Min(10000, Math.Max(1, value));
				speed = value;
			}
		}
		public double SpeedRatio {
			get { return (double)speed / 100; }
		}
		public int TotalNotes {
			get {
				int notes = 0;
				for (int i = 0; i < tracks.Count; i++)
					notes += tracks[i].Notes;
				return notes;
			}
		}
		public Keybind Keybind {
			get { return keybind; }
			set { keybind = value; }
		}

		public bool IsMessagePlayable(ChannelMessageEventArgs e) {
			return (e.Message.Data2 > 0 && e.Message.Command == ChannelCommand.NoteOn &&
					ContainsTrackObj(e.Track) && GetTrackSettingsByTrackObj(e.Track).Enabled);
		}
		public Exception LoadException {
			get { return exception; }
		}
	}
}
