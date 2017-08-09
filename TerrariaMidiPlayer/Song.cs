using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrariaMidiPlayer {
	enum Notes {
		C0 = 0,
		Cs0 = 1, Df0 = 1,
		D0 = 2,
		Ds0 = 3, Ef0 = 3,
		E0 = 4,
		F0 = 5,
		Fs0 = 6, Gf0 = 6,
		G0 = 7,
		Gs0 = 8, Af0 = 8,
		A0 = 9,
		As0 = 10, Bf0 = 10,
		B0 = 11,

		C1 = 12,
		Cs1 = 13, Df1 = 13,
		D1 = 14,
		Ds1 = 15, Ef1 = 15,
		E1 = 16,
		F1 = 17,
		Fs1 = 18, Gf1 = 18,
		G1 = 19,
		Gs1 = 20, Af1 = 20,
		A1 = 21,
		As1 = 22, Bf1 = 22,
		B1 = 23,

		C2 = 24
	}
	struct SongNote {
		public Notes Note;
		public double Duration;
		public SongNote(Notes note, double duration) {
			Note = note;
			Duration = duration;
		}
	}
	class Song {
		private List<SongNote> notes;

		public Song() {
			notes = new List<SongNote>();
		}

		public void Add(Notes note, double duration) {
			notes.Add(new SongNote(note, duration));
		}

		public SongNote this[int index] {
			get {
				return notes[index];
			}
		}

		public double TotalDuration {
			get {
				double totalDuration = 0;
				for (int i = 0; i < notes.Count; i++)
					totalDuration += notes[i].Duration;
				return totalDuration;
			}
		}
		public int TotalNotes {
			get {
				return notes.Count;
			}
		}
	}
}
