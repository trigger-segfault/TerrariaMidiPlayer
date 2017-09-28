using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Sanford.Multimedia.Midi;
using TerrariaMidiPlayer.Util;

namespace TerrariaMidiPlayer.Controls {
	class TrackGraph : Control {
		//=========== CLASSES ============
		#region Classes
			
		/**<summary>A note to be played in the track.</summary>*/
		private struct TrackNote {
			/**<summary>The semitone of the note.</summary>*/
			public int Semitone;
			/**<summary>The progress of the note.</summary>*/
			public double Progress;
			/**<summary>The absolute milliseconds of the note.</summary>*/
			public int Milliseconds;
			/**<summary>The track index of the note.</summary>*/
			public int TrackIndex;
			/**<summary>Constructs a track note.</summary>*/
			public TrackNote(int semitone, double progress, int milliseconds, int trackIndex) {
				this.Semitone = semitone;
				this.Progress = progress;
				this.Milliseconds = milliseconds;
				this.TrackIndex = trackIndex;
			}
		}

		#endregion
		//========== CONSTANTS ===========
		#region Constants

		/**<summary>The vertical spacing before the start and end of the graph.</summary>*/
		private const double SpacingY = 12;
		/**<summary>The horizontal spacing before the start of the graph.</summary>*/
		private const double SpacingLeft = 36;
		/**<summary>The horizontal spacing before the end of the graph.</summary>*/
		private const double SpacingRight = 6;
		/**<summary>The horizontal spacing before the octave labels.</summary>*/
		private const double SpacingLabel = 4;

		/**<summary>The color for recently played notes.</summary>*/
		public static readonly SolidColorBrush RecentlyPlayedNoteBrush = Brushes.LimeGreen;
		/**<summary>The color for valid notes.</summary>*/
		public static readonly SolidColorBrush ValidNoteBrush = Brushes.Violet;
		/**<summary>The color for wrapped notes.</summary>*/
		public static readonly SolidColorBrush WrappedNoteBrush = Brushes.Red;
		/**<summary>The color for skipped notes.</summary>*/
		public static readonly SolidColorBrush SkippedNoteBrush = Brushes.Blue;

		/**<summary>The track index code for displaying all tracks.</summary>*/
		public const int AllTracksIndex = -1;

		#endregion
		//=========== MEMBERS ============
		#region Members

		/**<summary>The index of the track.</summary>*/
		int trackIndex = -2;
		/**<summary>The sequencer used to get the track timing.</summary>*/
		//Sequencer sequncer = new Sequencer();
		/**<summary>The list of notes.</summary>*/
		List<TrackNote> notes = new List<TrackNote>();
		/**<summary>The highest displayed octave.</summary>*/
		int highestOctave = 10;
		/**<summary>The lowest displayed octave.</summary>*/
		int lowestOctave = -1;
		/**<summary>The duration of the track in milliseconds.</summary>*/
		int duration = 0;
		/**<summary>True if drawing playback.</summary>*/
		bool drawingPlayback = false;
		/**<summary>The playback UI update timer.</summary>*/
		Timer playbackUITimer = new Timer(100);

		#endregion
		//========= CONSTRUCTORS =========
		#region Constructors

		/**<summary>Constructs the track graph control.</summary>*/
		public TrackGraph() {
			playbackUITimer.Elapsed += OnPlaybackUIUpdate;
			playbackUITimer.AutoReset = true;
			Loaded += OnLoaded;

			if (!DesignerProperties.GetIsInDesignMode(this)) {
				playbackUITimer.Start();
			}
		}
		
		#endregion
		//=========== LOADING ============
		#region Loading

		/**<summary>Loads the specified track.</summary>*/
		public void LoadTrack(int trackIndex) {
			this.trackIndex = trackIndex;
			notes.Clear();
			duration = Config.Sequencer.Duration;
			drawingPlayback = Config.Sequencer.Position > 1 && Config.Sequencer.IsPlaying;
			playbackUITimer.Start();
			int min = (AllTracksLoaded ? 0 : trackIndex);
			int max = (AllTracksLoaded ? Config.Midi.TrackCount : trackIndex + 1);
			for (int index = min; index < max; index++) {
				if (!Config.Midi.GetTrackSettingsAt(index).Enabled && AllTracksLoaded)
					continue;
				Track track = Config.Midi.GetTrackAt(index).TrackObj;
				for (int i = 0; i < track.Count; i++) {
					var midiEvent = track.GetMidiEvent(i);
					if (midiEvent.MidiMessage.MessageType == MessageType.Channel) {
						var message = midiEvent.MidiMessage as ChannelMessage;
						if (message.Data2 > 0 && message.Command == ChannelCommand.NoteOn) {
							int semitone = message.Data1;
							int milliseconds = Config.Sequencer.TicksToMilliseconds(midiEvent.AbsoluteTicks);
							double progress = (double)milliseconds / duration;
							notes.Add(new TrackNote(semitone, progress, milliseconds, index));
						}
					}
				}
			}

			// Remove duplicate notes. They shouldn't exist in the first place
			for (int i = 0; i < notes.Count; i++) {
				for (int j = i + 1; j < notes.Count; j++) {
					if (notes[i].Milliseconds == notes[j].Milliseconds) {
						if (notes[i].Semitone == notes[j].Semitone) {
							// Remove duplicate
							notes.RemoveAt(j);
							j--;
							break;
						}
					}
					else {
						// No more notes could be duplicates at this point since note playtime is ordered
						break;
					}
				}
			}

			// Count the number of chords
			Chords = 0;
			bool chord = false;
			for (int i = 1; i < notes.Count; i++) {
				if (notes[i - 1].Milliseconds == notes[i].Milliseconds) {
					if (!chord) {
						Chords++;
						chord = true;
					}
				}
				else {
					chord = false;
				}
			}

			// Order by milliseconds so that all tracks can check for skipped notes
			notes.Sort((a, b) => { return a.Milliseconds - b.Milliseconds; });
		}
		/**<summary>Reloads the current track.</summary>*/
		public void ReloadTrack() {
			LoadTrack(trackIndex);
		}
		/**<summary>Updates changes made to the track.</summary>*/
		public void Update() {
			lowestOctave = 10;
			highestOctave = -1;

			int min = (AllTracksLoaded ? 0 : trackIndex);
			int max = (AllTracksLoaded ? Config.Midi.TrackCount : trackIndex + 1);
			for (int index = min; index < max; index++) {
				if (!Config.Midi.GetTrackSettingsAt(index).Enabled && AllTracksLoaded)
					continue;
				lowestOctave = Math.Min(lowestOctave, Math.Max(-1, Math.Min(
							Config.Midi.GetTrackSettingsAt(index).OctaveOffset - 1,
							(Config.Midi.GetTrackAt(index).LowestNote + Config.Midi.NoteOffset) / 12 - 1
				)));
				highestOctave = Math.Max(highestOctave, Math.Min(10, Math.Max(
							Config.Midi.GetTrackSettingsAt(index).OctaveOffset + 3,
							(Config.Midi.GetTrackAt(index).HighestNote + Config.Midi.NoteOffset - 1) / 12
				)));
			}
			if (lowestOctave >= highestOctave) {
				lowestOctave = -1;
				highestOctave = 10;
			}

			// The milliseconds of the next playable note
			int nextPlayable = 0;
			// The use time in milliseconds (Checked if not in designer)
			int useTime = 0;

			ValidNotes = 0;
			WrappedNotes = 0;
			SkippedNotes = 0;

			// Count note categories
			for (int i = 0; i < notes.Count; i++) {
				TrackNote note = notes[i];
				note.Semitone += Config.Midi.NoteOffset;
				if (note.Semitone >= 0 && note.Semitone <= 132) {
					bool skipped = note.Milliseconds < nextPlayable;
					bool inRange = IsNoteInsideRange(note);
					if (skipped) {
						SkippedNotes++;
					}
					else {
						nextPlayable = note.Milliseconds + useTime + 2;
						if (!inRange)
							WrappedNotes++;
						else
							ValidNotes++;
					}
				}
				else {
					SkippedNotes++;
				}
			}

			InvalidateVisual();
		}

		#endregion
		//========== PROPERTIES ==========
		#region Properties

		/**<summary>True if a track is loaded.</summary>*/
		public bool IsTrackLoaded {
			get { return trackIndex != -2; }
		}
		/**<summary>True if a track is loaded.</summary>*/
		public bool AllTracksLoaded {
			get { return trackIndex == AllTracksIndex; }
		}
		/**<summary>The loaded track index.</summary>*/
		/*private int TrackIndex {
			get { return trackIndex; }
		}*/
		/**<summary>The loaded track.</summary>*/
		/*private Track Track {
			get { return Config.Midi.GetTrackAt(trackIndex).TrackObj; }
		}*/
		/**<summary>The loaded midi track data.</summary>*/
		/*private Midi.TrackData TrackData {
			get { return Config.Midi.GetTrackAt(trackIndex); }
		}*/
		/**<summary>The loaded midi track settings.</summary>*/
		/*private Midi.TrackSettings TrackSettings {
			get { return Config.Midi.GetTrackSettingsAt(trackIndex); }
		}*/

		/**<summary>The highest semitone displayed on the graph.</summary>*/
		private int HighestSemitone {
			get { return (highestOctave + 1) * 12; }
		}
		/**<summary>The lowest semitone displayed on the graph.</summary>*/
		private int LowestSemitone {
			get { return (lowestOctave + 1) * 12; }
		}
		/**<summary>The semitone range displayed on the graph.</summary>*/
		private int SemitoneRange {
			get { return HighestSemitone - LowestSemitone; }
		}
		/**<summary>The octave range displayed on the graph.</summary>*/
		private int OctaveRange {
			get { return highestOctave - lowestOctave; }
		}

		/**<summary>The actual width of the graph area.</summary>*/
		private double ActualWidthSpacing {
			get { return ActualWidth - SpacingLeft - SpacingRight; }
		}
		/**<summary>The actual height of the graph area.</summary>*/
		private double ActualHeightSpacing {
			get { return ActualHeight - SpacingY * 2; }
		}

		/**<summary>True if valid notes are shown.</summary>*/
		public bool ShowValidNotes { get; set; } = true;
		/**<summary>True if wrapped notes are shown.</summary>*/
		public bool ShowWrappedNotes { get; set; } = true;
		/**<summary>True if skipped notes are shown.</summary>*/
		public bool ShowSkippedNotes { get; set; } = true;
		/**<summary>Gets the number of chords.</summary>*/
		public int Chords { get; private set; }
		/**<summary>Gets the number of valid notes.</summary>*/
		public int ValidNotes { get; private set; }
		/**<summary>Gets the number of wrapped notes.</summary>*/
		public int WrappedNotes { get; private set; }
		/**<summary>Gets the number of skipped notes.</summary>*/
		public int SkippedNotes { get; private set; }
		/**<summary>The total number of notes.</summary>*/
		public int TotalNotes {
			get { return ValidNotes + WrappedNotes + SkippedNotes; }
		}

		/**<summary>The threshold for how long a note will state it was recently played.</summary>*/
		private int RecentlyPlayedThreshold {
			get { return duration / 50; }
		}

		#endregion
		//============ EVENTS ============
		#region Events

		/**<summary>Called when the control is loaded to hook the window closed event.</summary>*/
		private void OnLoaded(object sender, RoutedEventArgs e) {
			if (!DesignerProperties.GetIsInDesignMode(this)) {
				Window.GetWindow(this).Closing += OnWindowClosing;
			}
		}
		/**<summary>Stops the playback timer.</summary>*/
		private void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e) {
			playbackUITimer.Stop();
		}
		/**<summary>Invalidates the drawing on size change.</summary>*/
		private void OnSizeChanged(object sender, SizeChangedEventArgs e) {
			InvalidateVisual();
		}
		/**<summary>Called when the ui needs to update.</summary>*/
		private void OnPlaybackUIUpdate(object sender, ElapsedEventArgs e) {
			bool playing = Config.Sequencer.Position > 1 && Config.Sequencer.IsPlaying;
			if (drawingPlayback || playing != drawingPlayback) {
				drawingPlayback = playing;
				Dispatcher.Invoke(InvalidateVisual);
			}
		}
		/**<summary>Redraws the control.</summary>*/
		protected override void OnRender(DrawingContext d) {
			// Set guidelines for pixel perfection
			GuidelineSet guidelines = new GuidelineSet();
			guidelines.GuidelinesX.Add(0.5);
			guidelines.GuidelinesX.Add(ActualHeight + 0.5);
			guidelines.GuidelinesY.Add(0.5);
			guidelines.GuidelinesY.Add(ActualWidth + 0.5);
			d.PushGuidelineSet(guidelines);

			// Highlight the midi's octave range
			if (!DesignerProperties.GetIsInDesignMode(this) && IsTrackLoaded) {
				int min = (AllTracksLoaded ? 0 : trackIndex);
				int max = (AllTracksLoaded ? Config.Midi.TrackCount : trackIndex + 1);
				for (int index = min; index < max; index++) {
					if (!Config.Midi.GetTrackSettingsAt(index).Enabled && AllTracksLoaded)
						continue;
					double high = GetSemitoneY((Config.Midi.GetTrackSettingsAt(index).OctaveOffset + 3) * 12);
					double low = GetSemitoneY((Config.Midi.GetTrackSettingsAt(index).OctaveOffset + 1) * 12);
					d.DrawRectangle(Brushes.LightYellow, null, Floor(new Rect(
						GetProgressX(0), high, ActualWidthSpacing, low - high
					)));
				}
				for (int index = min; index < max; index++) {
					if (!Config.Midi.GetTrackSettingsAt(index).Enabled && AllTracksLoaded)
						continue;
					double high = GetSemitoneY((Config.Midi.GetTrackSettingsAt(index).OctaveOffset + 3) * 12);
					double low = GetSemitoneY((Config.Midi.GetTrackSettingsAt(index).OctaveOffset + 1) * 12);
					d.DrawRectangle(null, new Pen(Brushes.Gold, 3), Floor(new Rect(
						GetProgressX(0), high, ActualWidthSpacing, low - high
					)));
				}
			}
			
			// Draw the octave lines and labels
			for (int i = 0; i <= OctaveRange; i++) {
				double y = TrackGraph.SpacingY + ActualHeightSpacing / OctaveRange * i;
				d.DrawLine(new Pen(Brushes.LightGray, 1),
					Floor(new Point(SpacingLeft, y)),
					Floor(new Point(ActualWidth - SpacingRight, y)));

				var formattedText = new FormattedText(
					"C" + (highestOctave - i),
					CultureInfo.GetCultureInfo("en-us"),
					FlowDirection.LeftToRight,
					new Typeface("Segoe UI"),
					12,
					Brushes.Black
				);
				y -= formattedText.Height / 2;
				d.DrawText(formattedText, Floor(new Point(SpacingLabel, y)));
			}

			// Stop using the guidelines
			d.Pop();

			// The milliseconds of the next playable note
			int nextPlayable = 0;
			// The use time in milliseconds (Checked if not in designer)
			int useTime = 0;
			
			// Current milliseconds of the midi playback
			int currentMilliseconds = 0;

			if (!DesignerProperties.GetIsInDesignMode(this)) {
				useTime = Config.UseTime * 1000 / 60;
				currentMilliseconds = Config.Sequencer.CurrentTime;
			}

			// Draw all notes
			for (int i = 0; i < notes.Count; i++) {
				TrackNote note = notes[i];
				note.Semitone += Config.Midi.NoteOffset;
				if (note.Semitone >= 0 && note.Semitone <= 132) {
					double x = GetProgressX(note.Progress);
					double y = GetSemitoneY(note.Semitone);
					
					bool skipped = note.Milliseconds < nextPlayable;
					bool inRange = IsNoteInsideRange(note);
					bool recentlyPlayed =
						note.Milliseconds <= currentMilliseconds &&
						note.Milliseconds + RecentlyPlayedThreshold >= currentMilliseconds &&
						Config.Midi.GetTrackSettingsAt(note.TrackIndex).Enabled;
					SolidColorBrush brush;
					if (skipped) {
						brush = SkippedNoteBrush;
						if (!ShowSkippedNotes)
							continue;
					}
					else {
						nextPlayable = note.Milliseconds + useTime + 2;
						if (!inRange) {
							brush = WrappedNoteBrush;
							if (!ShowWrappedNotes)
								continue;
						}
						else {
							brush = ValidNoteBrush;
							if (!ShowValidNotes)
								continue;
						}
					}
					if (recentlyPlayed && drawingPlayback && (!skipped || !Config.SkipPianoMode)) {
						double scale = (double)(currentMilliseconds - note.Milliseconds) / RecentlyPlayedThreshold;
						brush = Lerp(RecentlyPlayedNoteBrush, brush, scale);
					}

					// Don't floor the ellipse
					d.DrawEllipse(brush, null, new Point(x, y), 2, 2);
				}
			}
		}

		#endregion
		//=========== HELPERS ============
		#region Helpers

		/**<summary>Lerps between the primary and secondary color.</summary>*/
		private SolidColorBrush Lerp(SolidColorBrush a, SolidColorBrush b, double scale) {
			return new SolidColorBrush(Color.FromRgb(
				(byte)Math.Round(a.Color.R * (1 - scale) + b.Color.R * (scale)),
				(byte)Math.Round(a.Color.G * (1 - scale) + b.Color.G * (scale)),
				(byte)Math.Round(a.Color.B * (1 - scale) + b.Color.B * (scale))
			));
		}
		/**<summary>Gets if the note is within the midi's octave range.</summary>*/
		private bool IsNoteInsideRange(TrackNote note) {
			return (note.Semitone - (Config.Midi.GetTrackSettingsAt(note.TrackIndex).OctaveOffset + 1) * 12 >= 0 &&
					note.Semitone - (Config.Midi.GetTrackSettingsAt(note.TrackIndex).OctaveOffset + 1) * 12 <= 24);
		}
		/**<summary>Floors the rectangle.</summary>*/
		private Rect Floor(Rect rect) {
			rect.X = Math.Floor(rect.X);
			rect.Y = Math.Floor(rect.Y);
			rect.Width = Math.Floor(rect.Width);
			rect.Height = Math.Floor(rect.Height);
			return rect;
		}
		/**<summary>Floors the point.</summary>*/
		private Point Floor(Point point) {
			point.X = Math.Floor(point.X);
			point.Y = Math.Floor(point.Y);
			return point;
		}
		/**<summary>Floors the value.</summary>*/
		private double Floor(double value) {
			return Math.Floor(value);
		}
		/**<summary>Gets the Y value of the semitone.</summary>*/
		private double GetSemitoneY(int semitone) {
			return SpacingY + ActualHeightSpacing * (double)(SemitoneRange - (semitone - LowestSemitone)) / (double)SemitoneRange;
		}
		/**<summary>Gets the X value of the progress.</summary>*/
		private double GetProgressX(double progress) {
			return SpacingLeft + ActualWidthSpacing * progress;
		}

		#endregion
	}
}
