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

namespace TerrariaMidiPlayer {

	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {
		Stopwatch watch;
		IKeyboardMouseEvents globalHook;
		Random rand;
		int useTime;
		Song song;

		Midi midi;

		Sequencer sequencer;

		Size resolution;

		double projectileAngle;
		double projectileRange;

		Keys playKey;
		Keys pauseKey;
		Keys stopKey;
		Keys closeKey;

		int mount;

		static readonly int[] MountHeights = { 0 };

		List<Midi> midis;
		bool loaded = false;

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

			/*string fileName = @"C:\Users\Onii-chan\Music\shake-it-3.mid";
			midi = new Midi();
			midi.Load(fileName);
			sequencer.Sequence = midi.Sequence;

			midis.Add(midi);
			listMidis.Items.Add(System.IO.Path.GetFileName(fileName));
			listMidis.SelectedIndex = 0;*/

			resolution = new Size(1920, 1080);

			projectileAngle = 0;
			projectileRange = 360;

			closeKey = Keys.Escape;
			playKey = Keys.NumPad0;
			pauseKey = Keys.NumPad1;
			stopKey = Keys.NumPad2;

			mount = 0;

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

		private void OnLoaded(object sender, RoutedEventArgs e) {
			loaded = true;
		}

		private void PlaySemitone(int semitone, double direction) {
			double heightRatio = resolution.Height / 48.0;
			while (semitone < 0)
				semitone += 12;
			while (semitone > 24)
				semitone -= 12;
			double centerx = resolution.Width / 2;
			double centery = resolution.Height / 2 - MountHeights[mount];
			int x = (int)(centerx + Math.Cos(direction) * (heightRatio * semitone + 1));
			int y = (int)(centery + Math.Sin(direction) * (heightRatio * semitone + 1));
			if (x < 0) x = 0;
			if (x >= (int)resolution.Width) x = (int)resolution.Width - 1;
			if (y < 0) y = 0;
			if (y >= (int)resolution.Height) y = (int)resolution.Height - 1;
			MouseControl.SimulateClick(x, y, 1000 / 60 * 2 + 8);
		}

		private void OnPlayingCompleted(object sender, EventArgs e) {
			sequencer.Position = 0;
		}

		private void OnChannelMessagePlayed(object sender, ChannelMessageEventArgs e) {
			if (e.Message.Command == ChannelCommand.NoteOn) {
				if (e.Message.Data2 > 0 && midi.GetTrackSettingsByChannel(e.Message.MidiChannel).Enabled) {
					if (watch.ElapsedMilliseconds >= useTime * 1000 / 60 + 2) {
						int note = e.Message.Data1 - 12 * (midi.GetTrackSettingsByChannel(e.Message.MidiChannel).OctaveOffset + 1);
						PlaySemitone(note, (projectileAngle - projectileRange / 2 + rand.NextDouble() * projectileRange + 270) / 360.0 * Math.PI * 2.0);
						watch.Restart();
					}
				}
			}
		}

		private void OnGlobalKeyDown(object sender, System.Windows.Forms.KeyEventArgs e) {
			if (midi != null) {
				if (e.KeyCode == playKey) {
					watch.Start();
					sequencer.Continue();
				}
				else if (e.KeyCode == pauseKey) {
					sequencer.Stop();
				}
				else if (e.KeyCode == stopKey) {
					sequencer.Stop();
					sequencer.Position = 0;
				}
			}
			if (e.KeyCode == closeKey) {
				Close();
			}
		}

		private void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e) {
			globalHook.KeyDown -= OnGlobalKeyDown;
			globalHook.Dispose();
			globalHook = null;
			watch.Stop();
			sequencer.Stop();
		}

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

		private void OnProjectileChanged(object sender, RoutedEventArgs e) {
			projectileAngle = projectileControl.Angle;
			projectileRange = projectileControl.Range;
		}

		private void OnAddMidi(object sender, RoutedEventArgs e) {
			OpenFileDialog dialog = new OpenFileDialog();
			dialog.Filter = "Midi Files|*.mid;*.midi|All Files|*.*";
			dialog.FilterIndex = 0;
			var result = dialog.ShowDialog(this);
			if (result.HasValue && result.Value) {
				string fileName = dialog.FileName;
				midi = new Midi();
				midi.Load(fileName);
				watch.Stop();
				sequencer.Stop();
				sequencer.Sequence = midi.Sequence;
				sequencer.Position = 0;

				midis.Add(midi);
				listMidis.Items.Add(System.IO.Path.GetFileName(fileName));
				listMidis.SelectedIndex = listMidis.Items.Count - 1;
			}
		}

		private void OnRemoveMidi(object sender, RoutedEventArgs e) {
			var result = TriggerMessageBox.Show(this, MessageIcon.Warning, "Are you sure you want to remove this midi?", "Remove Midi", MessageBoxButton.YesNo);
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
				loaded = true;

				watch.Stop();
				sequencer.Stop();
				sequencer.Position = 0;
				if (index != -1) {
					midi = midis[index];
					sequencer.Sequence = midi.Sequence;
				}
				else {
					midi = null;
				}
			}
		}

		private void OnMidiChanged(object sender, SelectionChangedEventArgs e) {
			if (!loaded)
				return;
			
			watch.Stop();
			sequencer.Stop();
			sequencer.Position = 0;
			if (listMidis.SelectedIndex != -1) {
				midi = midis[listMidis.SelectedIndex];
				sequencer.Sequence = midi.Sequence;
			}
			else {
				midi = null;
			}
		}
	}
}
