using System;
using System.Collections.Generic;
using System.Linq;
using Sanford.Multimedia.Midi;
using System.Windows.Input;

namespace TerrariaMidiPlayer {

	public class Midi {
		public struct TrackData {
			public int HighestNote;
			public int LowestNote;
			public int Notes;
			public List<int> Channels;
			public int Index;
		}

		public class TrackSettings {
			public bool Enabled { get; set; }
			public int OctaveOffset { get; set; }

			public TrackSettings() {
				Enabled = true;
				OctaveOffset = 4;
			}
		}

		private string name;
		private string path;
		private Sequence sequence;
		private Dictionary<int, TrackData> channels;
		private List<TrackData> tracks;
		private List<TrackSettings> trackSettings;
		private int noteOffset;
		private int speed;
		private Keybind keybind;

		public Midi() {
			name = "";
			path = "";
			sequence = new Sequence();
			channels = new Dictionary<int, TrackData>();
			tracks = new List<TrackData>();
			trackSettings = new List<TrackSettings>();
			noteOffset = 0;
			keybind = new Keybind();
		}

		public bool Load(string path) {
			name = "";
			channels.Clear();
			tracks.Clear();
			trackSettings.Clear();
			noteOffset = 0;
			speed = 100;
			try {
				sequence.Load(path);
				this.path = path;
				int index = 0;
				foreach (var track in sequence) {
					TrackData trackData = new TrackData();
					TrackSettings settings = new TrackSettings();
					trackData.Channels = new List<int>();
					trackData.Index = index;
					for (int i = 0; i < track.Count; i++) {
						var midiEvent = track.GetMidiEvent(i);
						if (midiEvent.MidiMessage.MessageType == MessageType.Channel) {
							var message = midiEvent.MidiMessage as ChannelMessage;
							if (message.Data2 > 0 && message.Command == ChannelCommand.NoteOn) {

								if (!channels.ContainsKey(message.MidiChannel)) {
									channels.Add(message.MidiChannel, trackData);
									trackData.Channels.Add(message.MidiChannel);
								}
								trackData.Notes++;
								if (message.Data1 < trackData.LowestNote || trackData.LowestNote == 0)
									trackData.LowestNote = message.Data1;
								if (message.Data1 > trackData.HighestNote)
									trackData.HighestNote = message.Data1;
							}
						}
					}
					if (trackData.Channels.Count > 0) {
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

						foreach (int channel in trackData.Channels) {
							channels[channel] = trackData;
						}
						tracks.Add(trackData);
						trackSettings.Add(settings);
						index++;
					}
				}
				return true;
			}
			catch {
				this.path = "";
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
		public TrackData GetTrackByChannel(int channel) {
			return channels[channel];
		}
		public TrackSettings GetTrackSettingsByChannel(int channel) {
			return trackSettings[channels[channel].Index];
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

		public bool IsMessagePlayable(ChannelMessage message) {
			return (message.Data2 > 0 && message.Command == ChannelCommand.NoteOn &&
					GetTrackSettingsByChannel(message.MidiChannel).Enabled);
		}
	}
}
