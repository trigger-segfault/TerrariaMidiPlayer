#region License
// The MIT License (MIT)
// 
// Copyright (c) 2014 Emma 'Eniko' Maassen
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
#endregion
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using Sanford.Multimedia.Midi;

namespace TextPlayer.ABC {
	// http://abcnotation.com/wiki/abc:standard:v2.1#introduction
	// Note that this leaves many things unspecified and undocumented. Some undocumented features:
	// - Chord length should be the shortest note within the chords
	// - Chords can contain rests (and are used to calculate chord length as above)
	// - Lotro compatibility demands the default for accidentals is octave, not pitch (see '11.3 Accidental directives')
	// - Capitalised C is middle-C which is C4 in our system (or 261.6 Hz). This isn't explicitly stated but can be figured out by
	//   section '4.6 Clefs and transposition', where it states if the octave is reduced by 1 through a directive, 'c' will now be middle-C. 
	/// <summary>
	/// Abstract player which parses and plays ABC code. This class can load multiple tunes but can only play one of them at a time.
	/// </summary>
	public class ABCConverter {
		private const int DefaultOctave = 4;
		private const int MinOctave = -1;
		private const int MaxOctave = 10;

		private const int Ppqn = 192;
		private const int Tempo = 500000;

		private static readonly AccidentalPropagation DefaultAccidentalPropagation = AccidentalPropagation.Octave;
		
		private string version;
		private int versionMajor;
		private int versionMinor;

		private Dictionary<int, Tune> tunes;
		private bool inTune = false;

		private int tokenIndex;

		private int octave;
		private TimeSpan nextNote;

		private Dictionary<char, int> defaultAccidentals;
		private Dictionary<string, int> accidentals;
		private AccidentalPropagation accidentalPropagation;
		private Dictionary<string, int> tiedNotes;

		private double noteLength;
		private double meter;
		private byte timeSigNumerator;
		private byte timeSigDenominator;
		private double spm;
		private int selectedTune = 1;

		private Sequence sequence;
		private Track metaTrack;
		private Track mainTrack;

		/// <summary>
		/// Creates an ABC player. Uses static properties DefaultOctave and DefaultAccidentalPropagation and strict=true if
		/// arguments are left blank.
		/// </summary>
		private ABCConverter() {
			
			accidentals = new Dictionary<string, int>();
			tiedNotes = new Dictionary<string, int>();
			
			this.octave = DefaultOctave;
			this.accidentalPropagation = DefaultAccidentalPropagation;
		}

		private void SetDefaultValues() {
			tokenIndex = 0;
			meter = 1.0;
			timeSigNumerator = 4;
			timeSigDenominator = 4;
			noteLength = 0;
			spm = 60d / (120 * 0.25);
			tiedNotes.Clear();
		}

		private void SetHeaderValues(int index = 0, bool inferNoteLength = false) {
			List<string> values;

			if (tunes[index].Header.Information.TryGetValue('K', out values)) { // key
				GetKey(values[values.Count - 1]);
			}
			if (tunes[index].Header.Information.TryGetValue('M', out values)) { // meter
				meter = GetMeter(values[values.Count - 1]);
			}
			if (tunes[index].Header.Information.TryGetValue('L', out values)) { // note length
				noteLength = GetNoteLength(values[values.Count - 1]);
			}

			if (inferNoteLength && noteLength == 0)
				noteLength = (meter >= 0.75 ? 1.0 / 8 : 1.0 / 16);

			if (tunes[index].Header.Information.TryGetValue('Q', out values)) { // tempo
				SetTempo(values[values.Count - 1]);
			}
		}

		private void GetKey(string s) {
			defaultAccidentals = Keys.GetAccidentals(s);
		}

		private void SetTempo(string s) {
			s = s.Trim();

			Match bpmMatch;
			double length = 0;

			if (!s.Contains("=")) {
				bpmMatch = Regex.Match(s, @"\d+", RegexOptions.IgnoreCase);
				if (!bpmMatch.Success)
					return;

				length = 0.25;
			}
			else if (s[0] == 'C') {
				bpmMatch = Regex.Match(s.Substring(s.IndexOf('=')), @"\d+", RegexOptions.IgnoreCase);
				if (!bpmMatch.Success)
					return;

				length = 0.25;
			}
			else {
				MatchCollection matches;

				matches = Regex.Matches(s, @"""[^""]*""", RegexOptions.IgnoreCase);
				for (int i = 0; i < matches.Count; ++i)
					s = s.Replace(matches[i].Value, "");

				bool leftSideBpm = false;

				matches = Regex.Matches(s, @"\d+/\d+", RegexOptions.IgnoreCase);
				for (int i = 0; i < matches.Count; ++i) {
					length += GetNoteLength(matches[i].Value);
					if (s.IndexOf(matches[i].Value) > s.IndexOf('='))
						leftSideBpm = true;
				}

				string bpmStr;
				if (leftSideBpm) {
					bpmStr = s.Substring(0, s.IndexOf('='));
				}
				else {
					bpmStr = s.Substring(s.IndexOf('='));
				}

				bpmMatch = Regex.Match(bpmStr, @"\d+", RegexOptions.IgnoreCase);
				if (!bpmMatch.Success)
					return;
			}

			double bpm = Convert.ToDouble(bpmMatch.Value);
			var divisor = bpm * length;
			spm = 60d / divisor;
		}

		private double GetMeter(string s) {
			Match match = Regex.Match(s, @"\d+/\d+", RegexOptions.IgnoreCase);

			if (!match.Success)
				return -1;

			string[] numbers = match.Value.Split('/');
			var len = Convert.ToDouble(numbers[0]) / Convert.ToDouble(numbers[1]);
			timeSigNumerator = Convert.ToByte(numbers[0]);
			timeSigDenominator = Convert.ToByte(numbers[1]);
			return len;
		}
		private double GetNoteLength(string s) {
			Match match = Regex.Match(s, @"\d+/\d+", RegexOptions.IgnoreCase);

			if (!match.Success)
				return -1;

			string[] numbers = match.Value.Split('/');
			var len = Convert.ToDouble(numbers[0]) / Convert.ToDouble(numbers[1]);
			return len;
		}

		public static Sequence CreateSequenceFromABCFile(string filePath) {
			ABCConverter abc = new ABCConverter();
			abc.Load(File.ReadAllText(filePath, Encoding.ASCII));
			return abc.CreateSequence();
		}

		private Sequence CreateSequence() {
			if (tunes == null || tunes.Count < 2)
				return null;
			
			selectedTune = 1;

			SetDefaultValues();
			nextNote = TimeSpan.Zero;
			SetHeaderValues();
			SetHeaderValues(selectedTune, true);
			StartMeasure();

			sequence = new Sequence(Ppqn);
			sequence.Format = 1;
			metaTrack = new Track();
			mainTrack = new Track();
			sequence.Add(metaTrack);
			TempoChangeBuilder tempoBuilder = new TempoChangeBuilder();
			tempoBuilder.Tempo = Tempo;
			tempoBuilder.Build();
			metaTrack.Insert(0, tempoBuilder.Result);
			TimeSignatureBuilder timeBuilder = new TimeSignatureBuilder();
			timeBuilder.Numerator = timeSigNumerator;
			timeBuilder.Denominator = timeSigDenominator;
			timeBuilder.Build();
			metaTrack.Insert(0, timeBuilder.Result);
			sequence.Add(mainTrack);

			MetaTextBuilder textBuilder = new MetaTextBuilder();
			textBuilder.Type = MetaType.TrackName;
			textBuilder.Text = "Tempo Track";
			textBuilder.Build();
			metaTrack.Insert(0, textBuilder.Result);

			textBuilder = new MetaTextBuilder();
			textBuilder.Type = MetaType.TrackName;
			textBuilder.Text = "Tune 1";
			textBuilder.Build();
			mainTrack.Insert(0, textBuilder.Result);

			while (tokenIndex < tokens.Count) {
				AddNextNote();
			}
			
			return sequence;
		}
		// Milliseconds * 1000 * Ppqn / Tempo
		private int TimeSpanToTicks(TimeSpan time) {
			return (int)((long)time.TotalMilliseconds * 1000L * Ppqn / Tempo);
		}

		private void AddNextNote() {
			bool noteFound = false;
			bool chord = false;
			List<ABCNote> chordNotes = new List<ABCNote>();

			while (!noteFound && tokenIndex < tokens.Count) {
				string token = tokens[tokenIndex];

				char c = token[0];
				if (c == '[' && token == "[")
					c = '!';

				switch (c) {
				case ']':
					if (chord) {
						noteFound = true;
						chord = false;
						var chordLen = GetChord(chordNotes);
						AddChord(chordNotes, nextNote);
						nextNote += chordLen;
					}
					break;
				case '!': // replacement for chord opener
					chord = true;
					chordNotes.Clear();
					break;
				case '|':
				case ':':
				case '[': // TODO: repeats (if repeats are allowed)
					if (c == '[' && token.EndsWith("]") && token.Length > 2 && token[2] == ':' && token[1] != '|' && token[1] != ':')
						InlineInfo(token);
					else
						StartMeasure();
					break;
				case '+': // dynamics
					//GetDynamics(token);
					break;
				case 'z':
				case 'Z':
				case 'x':
					Note rest = GetRest(token);
					if (!chord) {
						nextNote += rest.Length;
						noteFound = true;
					}
					else
						chordNotes.Add(new ABCNote(rest, tokenIndex));
					break;
				case 'a':
				case 'b':
				case 'c':
				case 'd':
				case 'e':
				case 'f':
				case 'g':
				case 'A':
				case 'B':
				case 'C':
				case 'D':
				case 'E':
				case 'F':
				case 'G':
				case '^':
				case '=':
				case '_':
					Note note = GetNote(token);
					if (!chord) {
						//nextNote += note.Length;
						var tied = TieNote(new ABCNote(note, tokenIndex));
						if (tied.Type != 'r') {
							ValidateAndAddNote(tied, 0);
						}
						nextNote += note.Length;
						noteFound = true;
					}
					else {
						chordNotes.Add(new ABCNote(note, tokenIndex));
					}

					break;
				}

				tokenIndex++;
			}
		}

		private void AddChord(List<ABCNote> notes, TimeSpan time) {
			List<Note> chord = new List<Note>(notes.Count);
			for (int i = 0; i < notes.Count; ++i) {
				var tied = TieNote(notes[i]);
				if (tied.Type != 'r') {
					chord.Add(tied);
				}
			}
			AddChord(chord, time);
		}

		private void AddChord(List<Note> notes, TimeSpan time) {
			for (int i = 0; i < notes.Count; ++i) {
				ValidateAndAddNote(notes[i], i + 1);
			}
		}

		private void ValidateAndAddNote(Note note, int channel) {
			if (note.Octave < MinOctave)
				note.Octave = MinOctave;
			else if (note.Octave > MaxOctave)
				note.Octave = MaxOctave;
			int step = note.GetStep();
			mainTrack.Insert(TimeSpanToTicks(nextNote), new ChannelMessage(ChannelCommand.NoteOn, 0, step, 100));
			mainTrack.Insert(TimeSpanToTicks(nextNote + note.Length), new ChannelMessage(ChannelCommand.NoteOff, 0, step));
		}

		private void StartMeasure() {
			accidentals.Clear();
		}

		private bool IsTiedNote(int _tokenIndex) {
			return _tokenIndex + 1 < tokens.Count && tokens[_tokenIndex + 1][0] == '-';
		}

		private bool IsPlayableNote(string s) {
			switch (s[0]) {
			case 'a':
			case 'b':
			case 'c':
			case 'd':
			case 'e':
			case 'f':
			case 'g':
			case 'A':
			case 'B':
			case 'C':
			case 'D':
			case 'E':
			case 'F':
			case 'G':
				return true;
			case '^':
			case '=':
			case '_':
				Note note = GetNote(s);
				return note.Type >= 97 && note.Type <= 103;
			}
			return false;
		}
		

		private void InlineInfo(string s) {
			s = s.Substring(1, s.Length - 2).Trim();
			ABCInfo? info = ABCInfo.Parse(s);

			if (info.HasValue) {
				if (info.Value.Identifier == 'Q')
					SetTempo(info.Value.Text);
				else if (info.Value.Identifier == 'L')
					noteLength = GetNoteLength(info.Value.Text);
				else if (info.Value.Identifier == 'K')
					GetKey(info.Value.Text);
			}
		}

		private Note TieNote(ABCNote note) {
			var key = note.BaseNote.Type.ToString(System.Globalization.CultureInfo.InvariantCulture) + note.BaseNote.Octave.ToString(System.Globalization.CultureInfo.InvariantCulture);
			int count;

			if (tiedNotes.TryGetValue(key, out count)) {
				if (count > 0) {
					tiedNotes[key]--;
					note.BaseNote.Type = 'r';
					return note.BaseNote;
				}
			}

			if (IsTiedNote(note.TokenIndex)) {
				int nextIndex = note.TokenIndex + 1;

				while (nextIndex < tokens.Count) {
					if (IsPlayableNote(tokens[nextIndex])) {
						Note potential = GetNote(tokens[nextIndex]);
						if (potential.Type == note.BaseNote.Type && potential.Octave == note.BaseNote.Octave) {
							if (tiedNotes.ContainsKey(key)) {
								tiedNotes[key]++;
							}
							else {
								tiedNotes[key] = 1;
							}
							note.BaseNote.Length += potential.Length;
							if (!IsTiedNote(nextIndex)) {
								break;
							}
						}
					}

					nextIndex++;
				}
			}

			return note.BaseNote;
		}
		
		private Note GetRest(string s) {
			s = s.Trim();
			Note note = new Note();
			note.Type = 'r';

			if (s[0] != 'Z') {
				note.Length = new TimeSpan((long)(spm * ModifyNoteLength(s) * TimeSpan.TicksPerSecond)); //TimeSpan.FromSeconds(spm * ModifyNoteLength(s));
			}
			else {
				Match match = Regex.Match(s, @"\d+");
				double measures = 1;
				if (match.Success && match.Value.Length > 0)
					measures = Convert.ToDouble(match.Value);
				if (measures <= 0)
					measures = 1;
				note.Length = new TimeSpan((long)(spm * measures * TimeSpan.TicksPerSecond)); //TimeSpan.FromSeconds(spm * measures);
			}

			return note;
		}

		private Note GetNote(string s) {
			s = s.Trim();

			int? acc = null;

			Match match;
			match = Regex.Match(s, @"\^+", RegexOptions.IgnoreCase);
			if (match.Success)
				acc = match.Value.Length;
			match = Regex.Match(s, @"_+", RegexOptions.IgnoreCase);
			if (match.Success)
				acc = -match.Value.Length;
			match = Regex.Match(s, @"=+", RegexOptions.IgnoreCase);
			if (match.Success)
				acc = 0;

			int noteOctave = this.octave;

			for (int i = 0; i < s.Length; ++i) {
				if (s[i] == ',')
					noteOctave--;
				else if (s[i] == '\'')
					noteOctave++;
			}

			string tone = Regex.Match(s, @"[a-g]", RegexOptions.IgnoreCase).Value;
			if (tone.ToLowerInvariant() == tone) // is lower case
				noteOctave++;

			string accName = tone.ToUpperInvariant(); // key to use in the accidentals dictionary
			char keyAccName = tone.ToUpperInvariant()[0]; // key to use in the defaultAccidentals dictionary (specified by key)
			if (accidentalPropagation == AccidentalPropagation.Octave) {
				accName += noteOctave;
			}

			if (acc.HasValue && accidentalPropagation != AccidentalPropagation.Not)
				accidentals[accName] = acc.Value;

			int steps = 0;

			if (defaultAccidentals.ContainsKey(keyAccName))
				steps = defaultAccidentals[keyAccName];

			if (accidentals.ContainsKey(accName))
				steps = accidentals[accName];

			Note note = new Note();
			note.Type = tone.ToLowerInvariant()[0];
			note.Octave = noteOctave;

			Step(ref note, steps);

			note.Length = new TimeSpan((long)(spm * ModifyNoteLength(s) * TimeSpan.TicksPerSecond)); //TimeSpan.FromSeconds(spm * ModifyNoteLength(s));

			return note;
		}

		private void Step(ref Note note, int steps) {
			if (steps == 0)
				return;

			if (steps > 0) {
				for (int i = 0; i < steps; i++) {
					switch (note.Type) {
					case 'a':
						if (!note.Sharp)
							note.Sharp = true;
						else {
							note.Type = 'b';
							note.Sharp = false;
						}
						break;
					case 'b':
						note.Type = 'c';
						note.Octave++;
						break;
					case 'c':
						if (!note.Sharp)
							note.Sharp = true;
						else {
							note.Type = 'd';
							note.Sharp = false;
						}
						break;
					case 'd':
						if (!note.Sharp)
							note.Sharp = true;
						else {
							note.Type = 'e';
							note.Sharp = false;
						}
						break;
					case 'e':
						note.Type = 'f';
						break;
					case 'f':
						if (!note.Sharp)
							note.Sharp = true;
						else {
							note.Type = 'g';
							note.Sharp = false;
						}
						break;
					case 'g':
						if (!note.Sharp)
							note.Sharp = true;
						else {
							note.Type = 'a';
							note.Sharp = false;
						}
						break;
					}
				}
			}
			else {
				for (int i = 0; i < Math.Abs(steps); i++) {
					switch (note.Type) {
					case 'a':
						if (note.Sharp)
							note.Sharp = false;
						else {
							note.Type = 'g';
							note.Sharp = true;
						}
						break;
					case 'b':
						note.Type = 'a';
						note.Sharp = true;
						break;
					case 'c':
						if (note.Sharp)
							note.Sharp = false;
						else {
							note.Type = 'b';
							note.Octave--;
						}
						break;
					case 'd':
						if (note.Sharp)
							note.Sharp = false;
						else {
							note.Type = 'c';
							note.Sharp = true;
						}
						break;
					case 'e':
						note.Type = 'd';
						note.Sharp = true;
						break;
					case 'f':
						if (note.Sharp)
							note.Sharp = false;
						else {
							note.Type = 'e';
						}
						break;
					case 'g':
						if (note.Sharp)
							note.Sharp = false;
						else {
							note.Type = 'f';
							note.Sharp = true;
						}
						break;
					}
				}
			}
		}

		private double ModifyNoteLength(string s) {
			bool div = false;
			string num = "";
			double l = 1;
			for (int i = 0; i < s.Length; i++) {
				if ((int)s[i] >= 48 && (int)s[i] <= 57)
					num += s[i];
				else if (s[i] == '/') {
					if (!div && !string.IsNullOrWhiteSpace(num))
						l = Convert.ToDouble(num);
					else if (div && !string.IsNullOrWhiteSpace(num))
						l /= Convert.ToDouble(num);
					else if (div)
						l /= 2;
					num = "";
					div = true;
				}
			}

			if (l == 0)
				l = 1;

			if (num == "" && div) {
				num = "2";
			}

			if (num != "") {
				double n = Convert.ToDouble(num);
				if (n > 0) {
					if (div)
						l /= n;
					else
						l *= n;
				}
				else {
					l = 1;
				}
			}

			return noteLength * l;
		}

		private void Load(string str) {
			tunes = new Dictionary<int, Tune>();
			tunes.Add(0, new Tune());

			using (var stream = new StringReader(str)) {
				string line = stream.ReadLine();
				if (line == null)
					return;

				if (line.Length >= 6)
					version = line.Substring(5, line.Length - 5);

				if (version != null) {
					string[] majorMinor = version.Split('.');

					versionMajor = Convert.ToInt32(majorMinor[0]);
					versionMinor = Convert.ToInt32(majorMinor[1]);
				}

				while (line != null) {
					if (line != null)
						Interpret(line);
					line = stream.ReadLine();
				}

				ParseTune("");
			}

			foreach (var kvp in tunes) {
				if (kvp.Key > 0) {
					selectedTune = kvp.Key;
					if (tokens != null && tokens.Count > 0) {
						CalculateDuration(kvp.Value);
					}
				}
			}

			selectedTune = 1;
			SetDefaultValues();
		}

		private void CalculateDuration(Tune tune) {
			SetDefaultValues();
			SetHeaderValues();
			SetHeaderValues(selectedTune, true);

			TimeSpan dur = TimeSpan.Zero;
			bool chord = false;
			List<ABCNote> chordNotes = new List<ABCNote>();

			while (tokenIndex < tokens.Count) {
				string token = tokens[tokenIndex];

				char c = token[0];
				if (c == '[' && token == "[")
					c = '!';

				switch (c) {
				case ']':
					if (chord) {
						chord = false;
						var chordLen = GetChord(chordNotes);
						dur += chordLen;
					}
					break;
				case '!': // replacement for chord opener
					chord = true;
					chordNotes.Clear();
					break;
				case '|':
				case ':':
				case '[':
					if (c == '[' && token.EndsWith("]") && token.Length > 2 && token[2] == ':' && token[1] != '|' && token[1] != ':')
						InlineInfo(token);
					break;
				case 'z':
				case 'Z':
				case 'x':
					Note rest = GetRest(token);
					if (!chord) {
						dur += rest.Length;
					}
					else {
						chordNotes.Add(new ABCNote(rest, tokenIndex));
					}
					break;
				case 'a':
				case 'b':
				case 'c':
				case 'd':
				case 'e':
				case 'f':
				case 'g':
				case 'A':
				case 'B':
				case 'C':
				case 'D':
				case 'E':
				case 'F':
				case 'G':
				case '^':
				case '=':
				case '_':
					Note note = GetNote(token);
					if (!chord) {
						dur += note.Length;
					}
					else {
						chordNotes.Add(new ABCNote(note, tokenIndex));
					}
					break;
				}

				tokenIndex++;
			}

			tunes[selectedTune].Duration = dur;
		}

		private TimeSpan GetChord(List<ABCNote> chordNotes) {
			if (chordNotes.Count > 0) {
				TimeSpan minLength = TimeSpan.MaxValue;
				for (int i = chordNotes.Count - 1; i >= 0; i--) {
					var cnote = chordNotes[i];
					minLength = new TimeSpan((long)(Math.Min(minLength.TotalSeconds, cnote.BaseNote.Length.TotalSeconds) * TimeSpan.TicksPerSecond));
					if (cnote.BaseNote.Type == 'r') {
						chordNotes.RemoveAt(i);
					}
				}

				if (minLength == TimeSpan.MaxValue)
					minLength = TimeSpan.Zero;

				return minLength;
			}
			return TimeSpan.Zero;
		}

		private void Interpret(string rawLine) {
			// remove comments
			string line = rawLine.Split('%')[0].Trim();

			if (!inTune) {
				ParseHeader(line);
			}
			else {
				if (!(string.IsNullOrWhiteSpace(line) && rawLine != line)) { // skip commented empty lines so they dont end tunes
					if (!(string.IsNullOrWhiteSpace(line) && (tunes[tunes.Count - 1].RawCode == null || tunes[tunes.Count - 1].RawCode.Length == 0))) {
						ParseTune(line);
					}
				}
			}
		}

		private void ParseHeader(string line) {
			Tune tune = tunes[tunes.Count - 1];

			// this does not handle new global information after the first tune properly
			ABCInfo? i = ABCInfo.Parse(line);
			if (i.HasValue) {
				ABCInfo info = i.Value;

				if (info.Identifier == 'X') {
					// start new tune
					tune = new Tune();
					tunes.Add(tunes.Count, tune);
				}
				else if (info.Identifier == 'K') {
					// start interpreting notes
					inTune = true;
				}

				tune.Header.AddInfo(i.Value);
			}
		}

		private void ParseTune(string line) {
			const int kDefaultTuneLength = 1024;
			Tune tune = tunes[tunes.Count - 1];

			if (tune.RawCode == null) {
				tune.RawCode = new StringBuilder(kDefaultTuneLength);
			}

			if (!string.IsNullOrWhiteSpace(line)) {
				char c = line.Trim()[0];

				// add custom tokens for inlined stuff
				if (c == 'K' || c == 'L' || c == 'Q') {
					tune.RawCode.Append("[").Append(line.Trim()).Append("]");
				}
				else if (!(c == 'I' || c == 'M' || c == 'm' || c == 'N' || c == 'O' || c == 'P' || c == 'R' || c == 'r' || c == 's' ||
					c == 'T' || c == 'U' || c == 'V' || c == 'W' || c == 'w'))
					tune.RawCode.Append(line);
			}
			else {

				// strip code of all stuff we don't care about
				StringBuilder newCode = new StringBuilder(kDefaultTuneLength);
				List<char> filteredChars = new List<char>() {
					'\\', '\n', '\r', '\t'
				};

				if (tune.RawCode.Length == 0) {
					tune.Tokens = new List<string>();
					return;
				}

				for (int i = 0; i < tune.RawCode.Length; ++i) {
					if (!filteredChars.Contains(tune.RawCode[i]))
						newCode.Append(tune.RawCode[i]);
				}

				tune.Tokens = Tokenize(newCode);
			}
		}

		private List<string> Tokenize(StringBuilder code) {
			List<char> tokenStarters = new List<char>() {
				'|', ':',
				'[', '{', ']', '}',
				'z', 'x', 'Z',
				'A', 'B', 'C', 'D', 'E', 'F', 'G',
				'a', 'b', 'c', 'd', 'e', 'f', 'g',
				'_', '=', '^',
				'<', '>', '(',
				' ', '-', '"', '+'
			};
			List<char> tokenNotes = new List<char>() {
				'A', 'B', 'C', 'D', 'E', 'F', 'G',
				'a', 'b', 'c', 'd', 'e', 'f', 'g',
			};
			List<char> tokenBars = new List<char>() {
				'|', ':', '[', ']',
				'0', '1', '2', '3', '4', '5', '6', '7', '8', '9'
			};
			List<char> tokenTuplets = new List<char>() {
				'(', ':',
				'0', '1', '2', '3', '4', '5', '6', '7', '8', '9'
			};
			List<char> tokenInline = new List<char>() {
				'I', 'K', 'L', 'M', 'm', 'N', 'P', 'Q',
				'R', 'r', 's', 'T', 'U', 'V', 'W', 'w'
			};

			List<string> firstPass = new List<string>();
			StringBuilder curTokenText = new StringBuilder(code.Length);

			for (int j = 0; j < code.Length; ++j) {
				if (tokenStarters.Contains(code[j])) {
					if (curTokenText.Length > 0)
						firstPass.Add(curTokenText.ToString());
					curTokenText.Length = 0;
				}
				curTokenText.Append(code[j]);
			}
			if (curTokenText.Length > 0)
				firstPass.Add(curTokenText.ToString());

			const int kInitialTokenSize = 10;
			List<string> tokens = new List<string>();
			StringBuilder curToken = new StringBuilder(kInitialTokenSize);

			for (int i = 0; i < firstPass.Count; ++i) {
				curToken.Length = 0;

				if (firstPass[i][0] == '^') {
					while (firstPass[i][0] == '^' || tokenNotes.Contains(firstPass[i][0])) {
						curToken.Append(firstPass[i]);
						if (tokenNotes.Contains(firstPass[i][0]))
							break;
						i++;
						if (i >= firstPass.Count)
							break;
					}
				}
				else if (firstPass[i][0] == '+') {
					curToken.Append(firstPass[i]);
					i++;
					while (i < firstPass.Count) {
						curToken.Append(firstPass[i]);
						if (firstPass[i][0] == '+')
							break;
						i++;
						if (i >= firstPass.Count)
							break;
					}
				}
				else if (firstPass[i][0] == '_') {
					while (firstPass[i][0] == '_' || tokenNotes.Contains(firstPass[i][0])) {
						curToken.Append(firstPass[i]);
						if (tokenNotes.Contains(firstPass[i][0]))
							break;
						i++;
						if (i >= firstPass.Count)
							break;
					}
				}
				else if (firstPass[i][0] == '=') {
					curToken.Length = 0;
					curToken.Append("=");
					while (firstPass[i][0] == '=' || tokenNotes.Contains(firstPass[i][0])) {
						if (tokenNotes.Contains(firstPass[i][0])) {
							curToken.Append(firstPass[i]);
							break;
						}
						i++;
						if (i >= firstPass.Count)
							break;
					}
				}
				else if (firstPass[i][0] == '[' &&
							((firstPass[i].Length > 1 && tokenInline.Contains(firstPass[i][1])) ||
							(i < firstPass.Count - 1 && tokenInline.Contains(firstPass[i + 1][0])))) {
					char? cmdChar = null;
					if (firstPass[i].Length > 1)
						cmdChar = firstPass[i][1];
					else if (i < firstPass.Count - 1)
						cmdChar = firstPass[i + 1][0];

					if (cmdChar.HasValue) {
						if (tokenInline.Contains(cmdChar.Value)) {
							curToken.Length = 0;
							curToken.Append(firstPass[i]);
							i++;

							while (curToken[curToken.Length-1] != ']') {
								curToken.Append(firstPass[i]);
								i++;
								if (i >= firstPass.Count)
									break;
							}
							i--;
						}
					}
				}
				else if ((firstPass[i][0] == '[' && i < firstPass.Count - 1 && tokenBars.Contains(firstPass[i + 1][0]) && firstPass[i + 1][0] != ']') ||
					(firstPass[i][0] == '|' || firstPass[i][0] == ':' ||
					firstPass[i][0] == '0' || firstPass[i][0] == '1' || firstPass[i][0] == '2' || firstPass[i][0] == '3'
					 || firstPass[i][0] == '4' || firstPass[i][0] == '5' || firstPass[i][0] == '6' || firstPass[i][0] == '7'
					 || firstPass[i][0] == '8' || firstPass[i][0] == '9')) {
					while (tokenBars.Contains(firstPass[i][0])) {
						if (i > 0 && firstPass[i][0] == '[' && firstPass[i - 1][0] == '|')
							break;
						curToken.Append(firstPass[i]);
						i++;
						if (i >= firstPass.Count)
							break;
					}
					i--;
				}
				else if (firstPass[i][0] == '(') {
					while (tokenTuplets.Contains(firstPass[i][0])) {
						curToken.Append(firstPass[i]);
						i++;
						if (i >= firstPass.Count)
							break;
					}
					i--;
				}
				else if (firstPass[i][0] == '"') {
					i++;
					while (firstPass[i][0] != '"') {
						i++;
						if (i >= firstPass.Count)
							break;
					}
				}
				/*else if (firstPass[i][0] == '+') {
                    string text = firstPass[i].Trim();
                    if (text.Length == 1) {
                        curToken = null;
                    }
                    else {
                        curToken = text.ToLowerInvariant();
                    }
                }*/
				else {
					curToken.Length = 0;
					curToken.Append(firstPass[i]);
				}

				if (curToken.Length > 0)
					tokens.Add(curToken.ToString());

			}

			return tokens;
		}
		
		private List<string> tokens { get { return tunes[selectedTune].Tokens; } }
		private TimeSpan duration { get { return tunes[selectedTune].Duration; } }
	}
}
