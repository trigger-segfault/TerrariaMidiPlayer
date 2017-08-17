using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrariaMidiPlayer.Syncing {

	public enum Commands : int {
		None = 0,		 // None

		// Sent from Client
		Login, // String
		//LatencyResult,  // Time
		Ready,          // None
		NotReady,       // None

		// Sent from Server
		//AssignID,       // None
		AcceptedUser,   // None
		NameTaken,      // None
		InvalidPassword,// None
		AssingSong,     // String
		//LatencyTest,    // None
		//CheckReady,     // None
		//CancelReady,    // None
		PlaySong,       // Time
		StopSong        // None
	}

	public class Command {
		private Commands type;
		private string name;

		public Commands Type {
			get { return type; }
		}
		public string Name {
			get { return name; }
		}

		public Command(Commands type, string name) {
			this.type = type;
			this.name = name;
		}
		public Command(byte[] data, int size) {
			ReadBytes(data, size);
		}

		public void ReadBytes(byte[] data, int size) {
			BinaryReader reader = new BinaryReader(new MemoryStream(data, 0, size, false));
			ReadFromByteArray(reader);
			reader.Close();
		}
		public byte[] GetBytes() {
			MemoryStream stream = new MemoryStream();
			BinaryWriter writer = new BinaryWriter(stream);
			WriteToByteArray(writer);
			byte[] data = stream.ToArray();
			writer.Close();
			return data;
		}

		public virtual void ReadFromByteArray(BinaryReader reader) {
			type = (Commands)reader.ReadInt32();
			name = reader.ReadString();
		}
		public virtual void WriteToByteArray(BinaryWriter writer) {
			writer.Write((int)type);
			writer.Write(name);
		}

		public static Commands GetCommandType(byte[] data, int size) {
			if (size >= 7) {
				return (Commands)BitConverter.ToInt32(data, 0);
			}
			return Commands.None;
		}
		public static string GetCommandName(byte[] data, int size) {
			if (size >= 7) {
				BinaryReader reader = new BinaryReader(new MemoryStream(data, 0, size, false));
				reader.BaseStream.Position = 4;
				string name = reader.ReadString();
				reader.Close();
				return name;
			}
			return "";
		}
	}

	public class StringCommand : Command {

		public string Text { get; set; }

		public StringCommand(Commands type, string name) : base(type, name) {
			this.Text = "";
		}
		public StringCommand(Commands type, string name, string text) : base(type, name) {
			this.Text = text;
		}
		public StringCommand(byte[] data, int size) : base(data, size) {
			ReadBytes(data, size);
		}

		public override void ReadFromByteArray(BinaryReader reader) {
			base.ReadFromByteArray(reader);
			Text = reader.ReadString();
		}
		public override void WriteToByteArray(BinaryWriter writer) {
			base.WriteToByteArray(writer);
			writer.Write(Text);
		}
	}

	public class TimeCommand : Command {

		public long Ticks { get; set; }
		public DateTime DateTime {
			get { return DateTime.FromBinary(Ticks); }
			set { Ticks = value.Ticks; }
		}
		public TimeSpan TimeSpan {
			get { return TimeSpan.FromTicks(Ticks); }
			set { Ticks = value.Ticks; }
		}

		public TimeCommand(Commands type, string name) : base(type, name) {
			this.Ticks = 0;
		}
		public TimeCommand(Commands type, string name, long ticks) : base(type, name) {
			this.Ticks = ticks;
		}
		public TimeCommand(Commands type, string name, DateTime dateTime) : base(type, name) {
			this.DateTime = dateTime;
		}
		public TimeCommand(Commands type, string name, TimeSpan timeSpan) : base(type, name) {
			this.TimeSpan = timeSpan;
		}
		public TimeCommand(byte[] data, int size) : base(data, size) {
			ReadBytes(data, size);
		}

		public override void ReadFromByteArray(BinaryReader reader) {
			base.ReadFromByteArray(reader);
			Ticks = reader.ReadInt64();
		}
		public override void WriteToByteArray(BinaryWriter writer) {
			base.WriteToByteArray(writer);
			writer.Write(Ticks);
		}
	}
}
