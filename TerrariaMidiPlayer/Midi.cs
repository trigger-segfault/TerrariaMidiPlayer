using System;
using System.Collections.Generic;
using System.Linq;
using Sanford.Multimedia.Midi;

namespace TerrariaMidiPlayer {

	public struct TrackData {
		public int HighestNote;
		public int LowestNote;
		public int Notes;
		public int Channel;
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

	public class Midi {
		private string path;
		private Sequence sequence;
		private Dictionary<int, TrackData> channels;
		private List<TrackData> tracks;
		private List<TrackSettings> trackSettings;
		private int noteOffset;

		public Midi() {
			path = "";
			sequence = new Sequence();
			channels = new Dictionary<int, TrackData>();
			tracks = new List<TrackData>();
			trackSettings = new List<TrackSettings>();
			noteOffset = 0;
		}

		public bool Load(string path) {
			tracks.Clear();
			trackSettings.Clear();
			noteOffset = 0;
			try {
				sequence.Load(path);
				this.path = path;
				int index = 0;
				foreach (var track in sequence) {
					TrackData trackData = new TrackData();
					TrackSettings settings = new TrackSettings();
					trackData.Channel = -1;
					trackData.Index = index;
					for (int i = 0; i < track.Count; i++) {
						var midiEvent = track.GetMidiEvent(i);
						if (midiEvent.MidiMessage.MessageType == MessageType.Channel) {
							var message = midiEvent.MidiMessage as ChannelMessage;
							trackData.Channel = message.MidiChannel;
							if (message.Data2 > 0 && message.Command == ChannelCommand.NoteOn) {
								trackData.Notes++;
								if (message.Data1 < trackData.LowestNote || trackData.LowestNote == 0)
									trackData.LowestNote = message.Data1;
								if (message.Data1 > trackData.HighestNote)
									trackData.HighestNote = message.Data1;
							}
						}
					}
					if (trackData.Channel != -1) {
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

						channels.Add(trackData.Channel, trackData);
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
	}
}
