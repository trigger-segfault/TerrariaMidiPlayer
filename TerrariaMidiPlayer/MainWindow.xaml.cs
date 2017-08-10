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

		Rect clientArea;

		double projectileAngle;
		double projectileRange;

		Keys playKey;
		Keys pauseKey;
		Keys stopKey;
		Keys closeKey;

		int mount;

		bool checksEnabled;
		int checkFrequency;
		int checkCount;

		static readonly int[] MountHeights = { 0 };

		List<Midi> midis;
		bool loaded = false;

		int clickTime;

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
			clientArea = new Rect();

			projectileAngle = 0;
			projectileRange = 360;

			closeKey = Keys.Escape;
			playKey = Keys.NumPad0;
			pauseKey = Keys.NumPad1;
			stopKey = Keys.NumPad2;

			checksEnabled = true;
			checkFrequency = 0;
			checkCount = 0;
			clickTime = 40;

			mount = 0;

			UpdateMidi();

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
			double heightRatio = clientArea.Height / 48.0;
			while (semitone < 0)
				semitone += 12;
			while (semitone > 24)
				semitone -= 12;
			double centerx = clientArea.Width / 2;
			double centery = clientArea.Height / 2 - MountHeights[mount];
			int x = (int)(centerx + Math.Cos(direction) * (heightRatio * semitone + 2));
			int y = (int)(centery + Math.Sin(direction) * (heightRatio * semitone + 2));
			if (x < 0) x = 0;
			if (x >= (int)clientArea.Width) x = (int)clientArea.Width - 1;
			if (y < 0) y = 0;
			if (y >= (int)clientArea.Height) y = (int)clientArea.Height - 1;
			x += (int)clientArea.X;
			y += (int)clientArea.Y;
			MouseControl.SimulateClick(x, y, clickTime);
		}

		private void OnPlayingCompleted(object sender, EventArgs e) {
			Stop();
		}

		private void OnChannelMessagePlayed(object sender, ChannelMessageEventArgs e) {
			if (midi.IsMessagePlayable(e.Message) && watch.ElapsedMilliseconds >= useTime * 1000 / 60 + 2) {
				if (checksEnabled) {
					checkCount++;
					if (checkCount > checkFrequency) {
						checkCount = 0;
						TerrariaWindowLocator.Update();
						if (!TerrariaWindowLocator.HasFocus) {
							TerrariaWindowLocator.Focus();
							Thread.Sleep(100);
						}
						if (!TerrariaWindowLocator.IsOpen) {
							Pause();
							return;
						}
						clientArea = TerrariaWindowLocator.ClientArea;
					}
				}
				int note = e.Message.Data1 - 12 * (midi.GetTrackSettingsByChannel(e.Message.MidiChannel).OctaveOffset + 1) + midi.NoteOffset;
				PlaySemitone(note, (projectileAngle - projectileRange / 2 + rand.NextDouble() * projectileRange + 270) / 360.0 * Math.PI * 2.0);
				watch.Restart();
			}
		}

		private void OnGlobalKeyDown(object sender, System.Windows.Forms.KeyEventArgs e) {
			if (!loaded || keybindReaderTrack.IsReading)
				return;

			if (midi != null) {
				if (e.KeyCode == playKey) {
					Play();
				}
				else if (e.KeyCode == pauseKey) {
					Pause();
				}
				else if (e.KeyCode == stopKey) {
					Stop();
				}
			}
			for (int i = 0; i < midis.Count; i++) {
				if (midis[i].Keybind.IsDown(e)) {
					Stop();
					
					loaded = false;
					listMidis.SelectedIndex = i;
					loaded = true;
					midi = midis[listMidis.SelectedIndex];
					sequencer.Sequence = midi.Sequence;

					UpdateMidi();
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

		private void OnUseTimeChanged(object sender, RoutedEventArgs e) {
			if (!loaded)
				return;

			useTime = numericUseTime.Value;
		}
		
		private void Play() {
			if (midi != null) {
				TerrariaWindowLocator.Update();
				if (!TerrariaWindowLocator.HasFocus) {
					TerrariaWindowLocator.Focus();
					Thread.Sleep(400);
				}
				if (TerrariaWindowLocator.IsOpen) {
					clientArea = TerrariaWindowLocator.ClientArea;
					watch.Start();
					sequencer.Continue();
					checkCount = 0;
				}
				else {
					TriggerMessageBox.Show(this, MessageIcon.Error, "You cannot play a song when Terraria is not running!", "Terraria not Running");
				}
			}
		}

		private void Pause() {
			sequencer.Stop();
		}

		private void Stop() {
			watch.Stop();
			sequencer.Stop();
			sequencer.Position = 0;
		}

		private void OnMidiChanged(object sender, SelectionChangedEventArgs e) {
			if (!loaded)
				return;

			Stop();
			if (listMidis.SelectedIndex != -1) {
				midi = midis[listMidis.SelectedIndex];
				sequencer.Sequence = midi.Sequence;
			}
			else {
				midi = null;
			}
			UpdateMidi();
		}

		private void OnAddMidi(object sender, RoutedEventArgs e) {
			Stop();

			loaded = false;
			OpenFileDialog dialog = new OpenFileDialog();
			dialog.Filter = "Midi Files|*.mid;*.midi|All Files|*.*";
			dialog.FilterIndex = 0;
			var result = dialog.ShowDialog(this);
			loaded = true;
			if (result.HasValue && result.Value) {
				string fileName = dialog.FileName;
				midi = new Midi();
				midi.Load(fileName);

				midis.Add(midi);
				listMidis.Items.Add("Loading...");
				listMidis.SelectedIndex = listMidis.Items.Count - 1;
				listMidis.ScrollIntoView(listMidis.Items[listMidis.SelectedIndex]);

				sequencer.Sequence = midi.Sequence;
				listMidis.Items[listMidis.SelectedIndex] = midi.ProperName;
				listMidis.SelectedIndex = listMidis.Items.Count - 1;
				UpdateMidi();
			}
		}

		private void OnRemoveMidi(object sender, RoutedEventArgs e) {
			Stop();
			loaded = false;
			var result = TriggerMessageBox.Show(this, MessageIcon.Warning, "Are you sure you want to remove this midi?", "Remove Midi", MessageBoxButton.YesNo);
			loaded = true;
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
				if (index != -1)
					listMidis.ScrollIntoView(listMidis.Items[index]);
				loaded = true;

				if (index != -1) {
					midi = midis[index];
					sequencer.Sequence = midi.Sequence;
				}
				else {
					midi = null;
				}
				UpdateMidi();
			}
		}

		private void OnEditMidiName(object sender, RoutedEventArgs e) {
			Stop();
			if (midi != null) {
				loaded = false;
				string newName = EditNameDialog.ShowDialog(this, midi.ProperName);
				loaded = true;
				if (newName != null) {
					midi.Name = newName;
					listMidis.Items[listMidis.SelectedIndex] = newName;
				}
			}
		}

		private void OnMoveMidiUp(object sender, RoutedEventArgs e) {
			Stop();
			int index = listMidis.SelectedIndex;
			if (midi != null && index > 0) {
				loaded = false;
				listMidis.Items.RemoveAt(index);
				listMidis.Items.Insert(index - 1, midi.ProperName);
				listMidis.SelectedIndex = index - 1;
				midis.RemoveAt(index);
				midis.Insert(index - 1, midi);
				loaded = true;
				UpdateMidiButtons();
			}
		}

		private void OnMoveMidiDown(object sender, RoutedEventArgs e) {
			Stop();
			int index = listMidis.SelectedIndex;
			if (midi != null && index + 1 < listMidis.Items.Count) {
				loaded = false;
				listMidis.Items.RemoveAt(index);
				listMidis.Items.Insert(index + 1, midi.ProperName);
				listMidis.SelectedIndex = index + 1;
				midis.RemoveAt(index);
				midis.Insert(index + 1, midi);
				loaded = true;
				UpdateMidiButtons();
			}
		}

		private void OnChecksChanged(object sender, RoutedEventArgs e) {
			checkFrequency = numericChecks.Value;
		}

		private void OnChecksEnabledClicked(object sender, RoutedEventArgs e) {
			if (!loaded)
				return;

			checksEnabled = checkBoxChecks.IsChecked.Value;
			numericChecks.IsEnabled = checksEnabled;
		}

		private void OnNoteOffsetChanged(object sender, RoutedEventArgs e) {
			if (!loaded)
				return;

			midi.NoteOffset = numericNoteOffset.Value;
			labelHighestNote.Content = "Highest Note: " + NoteToString(midi.GetTrack(listTracks.SelectedIndex).HighestNote + midi.NoteOffset);
			labelLowestNote.Content = "Lowest Note: " + NoteToString(midi.GetTrack(listTracks.SelectedIndex).LowestNote + midi.NoteOffset);
		}

		private void OnTrackEnabledClicked(object sender, RoutedEventArgs e) {
			if (!loaded)
				return;
			
			int index = listTracks.SelectedIndex;
			midi.GetTrackSettings(index).Enabled = checkBoxTrackEnabled.IsChecked.Value;

			loaded = false;
			listTracks.Items.RemoveAt(index);
			ListBoxItem item = new ListBoxItem();
			item.Content = "Track " + (index + 1);
			if (!midi.GetTrackSettings(index).Enabled)
				item.Foreground = Brushes.Gray;
			listTracks.Items.Insert(index, item);
			loaded = true;
		}

		private void OnOctaveOffsetChanged(object sender, RoutedEventArgs e) {
			if (!loaded)
				return;

			midi.GetTrackSettings(listTracks.SelectedIndex).OctaveOffset = numericOctaveOffset.Value;
		}

		private void OnTrackChanged(object sender, SelectionChangedEventArgs e) {
			if (!loaded)
				return;

			UpdateTrack();
		}

		private void OnSpeedChanged(object sender, RoutedEventArgs e) {
			if (!loaded)
				return;

			midi.Speed = numericSpeed.Value;
			sequencer.AltTempo = 100.0 / (double)midi.Speed;
		}

		public void OnMidiKeybindChanged(object sender, RoutedEventArgs e) {
			if (!loaded)
				return;

			midi.Keybind = keybindReaderTrack.Keybind;
		}

		private void OnClickTimeChanged(object sender, RoutedEventArgs e) {
			if (!loaded)
				return;

			clickTime = numericClickTime.Value;
		}

		public void UpdateMidi() {
			loaded = false;
			listTracks.Items.Clear();
			loaded = true;
			if (midi != null) {
				loaded = false;
				labelTotalNotes.Content = "Total Notes: " + midi.TotalNotes;
				labelDuration.Content = "Duration: " + MillisecondsToString(sequencer.Duration);
				keybindReaderTrack.Keybind = midi.Keybind;
				numericNoteOffset.IsEnabled = true;
				numericSpeed.IsEnabled = true;
				numericNoteOffset.Value = midi.NoteOffset;
				numericSpeed.Value = midi.Speed;
				keybindReaderTrack.IsEnabled = true;
				if (midi.TrackCount > 0) {
					for (int i = 0; i < midi.TrackCount; i++) {
						ListBoxItem item = new ListBoxItem();
						item.Content = "Track " + (i + 1);
						if (!midi.GetTrackSettings(i).Enabled)
							item.Foreground = Brushes.Gray;
						listTracks.Items.Add(item);
					}
					listTracks.SelectedIndex = 0;
				}
				listTracks.IsEnabled = (midi.TrackCount > 0);
				loaded = true;
			}
			else {
				labelTotalNotes.Content = "Total Notes: ";
				labelDuration.Content = "Duration: ";
				numericNoteOffset.IsEnabled = false;
				numericSpeed.IsEnabled = false;
				listTracks.IsEnabled = false;
				keybindReaderTrack.IsEnabled = false;
			}
			UpdateTrack();
			UpdateMidiButtons();
		}
		private void UpdateTrack() {
			if (midi != null && midi.TrackCount > 0) {
				loaded = false;
				labelHighestNote.Content = "Highest Note: " + NoteToString(midi.GetTrack(listTracks.SelectedIndex).HighestNote + midi.NoteOffset);
				labelLowestNote.Content = "Lowest Note: " + NoteToString(midi.GetTrack(listTracks.SelectedIndex).LowestNote + midi.NoteOffset);
				labelNotes.Content = "Notes: " + midi.GetTrack(listTracks.SelectedIndex).Notes;
				checkBoxTrackEnabled.IsChecked = midi.GetTrackSettings(listTracks.SelectedIndex).Enabled;
				numericOctaveOffset.Value = midi.GetTrackSettings(listTracks.SelectedIndex).OctaveOffset;
				numericOctaveOffset.IsEnabled = true;
				checkBoxTrackEnabled.IsEnabled = true;
				loaded = true;
			}
			else {
				labelHighestNote.Content = "Highest Note: ";
				labelLowestNote.Content = "Lowest Note: ";
				labelNotes.Content = "Notes: ";
				numericOctaveOffset.IsEnabled = false;
				checkBoxTrackEnabled.IsEnabled = false;
			}
		}
		private void UpdateMidiButtons() {
			buttonRemoveMidi.IsEnabled = (listMidis.SelectedIndex != -1);
			buttonEditMidiName.IsEnabled = (listMidis.SelectedIndex != -1);
			buttonMoveMidiUp.IsEnabled = (listMidis.SelectedIndex > 0);
			buttonMoveMidiDown.IsEnabled = (listMidis.SelectedIndex != -1 && listMidis.SelectedIndex + 1 < listMidis.Items.Count);
		}

		private string NoteToString(int note) {
			string[] notes = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
			string[] altNotes = { "", "D\u266D", "", "E\u266D", "", "", "G\u266D", "", "A\u266D", "", "B\u266D", "" };
			int semitone = note % 12;
			note -= 12;
			string noteStr = notes[semitone] + (note / 12);
			if (altNotes[semitone].Length > 0)
				noteStr += " (" + altNotes[semitone] + (note / 12) + ")";
			return noteStr;
		}

		private string MillisecondsToString(int milliseconds, bool showHours = false, bool showMilliseconds = false) {
			int ms = milliseconds % 1000;
			int seconds = (milliseconds / 1000) % 60;
			int minutes = (milliseconds / 1000 / 60);
			int hours = (milliseconds / 1000 / 60 / 60);
			if (showHours)
				minutes %= 60;

			string timeStr = "";
			if (showHours) {
				timeStr += hours.ToString() + ":";
				if (minutes < 10)
					timeStr += "0";
			}
			timeStr += minutes.ToString() + ":";
			if (seconds < 10)
				timeStr += "0";
			timeStr += seconds.ToString();
			if (showMilliseconds) {
				timeStr += "." + ms.ToString();
			}
			return timeStr;
		}
	}
}
