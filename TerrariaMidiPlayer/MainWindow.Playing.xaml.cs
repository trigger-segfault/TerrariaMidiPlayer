using Sanford.Multimedia.Midi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using TerrariaMidiPlayer.Util;
using TerrariaMidiPlayer.Windows;

namespace TerrariaMidiPlayer {
	/**<summary>The main window running Terraria Midi Player.</summary>*/
	public partial class MainWindow : Window {
		//============ EVENTS ============
		#region Events

		private void OnPlayingCompleted(object sender, EventArgs e) {
			Stop();
		}
		private void OnChannelMessagePlayed(object sender, ChannelMessageEventArgs e) {
			if (Config.Midi.IsMessagePlayable(e) && (watch.ElapsedMilliseconds >= Config.UseTime * 1000 / 60 + 2 || firstNote)) {
				if (Config.ChecksEnabled) {
					checkCount++;
					if (!TerrariaWindowLocator.Update(Config.ChecksEnabled && checkCount > Config.CheckFrequency)) {
						Pause();
						Dispatcher.Invoke(() => {
							TriggerMessageBox.Show(this, MessageIcon.Error, "Failed to keep track of the Terraria Window!", "Tracking Error");
						});
						return;
					}
					if (checkCount > Config.CheckFrequency)
						checkCount = 0;
					if (!TerrariaWindowLocator.HasFocus) {
						TerrariaWindowLocator.Focus();
						Thread.Sleep(100);
						return;
					}
					if (!TerrariaWindowLocator.IsOpen) {
						Pause();
						Dispatcher.Invoke(() => {
							TriggerMessageBox.Show(this, MessageIcon.Warning, "Terraria window has been closed.", "Terraria Closed");
						});
						return;
					}
					clientArea = TerrariaWindowLocator.ClientArea;
				}
				firstNote = false;
				int note = e.Message.Data1 - 12 * (Config.Midi.GetTrackSettingsByTrackObj(e.Track).OctaveOffset + 1) + Config.Midi.NoteOffset;
				watch.Restart();
				PlayNote(note);
			}
		}

		#endregion
		//============= PLAY =============
		#region Play

		/**<summary>Starts or continues the song.</summary>*/
		private void Play() {
			if (Config.HasMidi) {
				firstNote = true;
				TerrariaWindowLocator.Update(true);
				if (!TerrariaWindowLocator.HasFocus) {
					TerrariaWindowLocator.Focus();
					Thread.Sleep(400);
				}
				if (TerrariaWindowLocator.IsOpen) {
					clientArea = TerrariaWindowLocator.ClientArea;
					noteWatch.Restart();

					// When the sequencer finishes it leaves its position at 1
					if (sequencer.Position <= 1)
						sequencer.Start();
					else
						sequencer.Continue();

					checkCount = 0;
					Dispatcher.Invoke(() => {
						toggleButtonStop.IsChecked = false;
						toggleButtonPlay.IsChecked = true;
						toggleButtonPause.IsChecked = false;
						playbackUITimer.Start();
					});
				}
				else {
					Dispatcher.Invoke(() => {
						toggleButtonPlay.IsChecked = false;
						TriggerMessageBox.Show(this, MessageIcon.Warning, "You cannot play a midi when Terraria isn't running! Have you specified the correct executable name in Options?", "Terraria not Running");
					});
				}
			}
		}
		/**<summary>Pause the song.</summary>*/
		private void Pause() {
			noteWatch.Stop();
			sequencer.Stop();
			if (Config.HasMidi) {
				Dispatcher.Invoke(() => {
					toggleButtonStop.IsChecked = false;
					toggleButtonPlay.IsChecked = false;
					toggleButtonPause.IsChecked = true;
					OnPlaybackUIUpdate(null, null);
					playbackUITimer.Stop();
				});
			}
		}
		/**<summary>Stop the song.</summary>*/
		private void Stop() {
			noteWatch.Stop();
			sequencer.Stop();
			sequencer.Position = 0;
			if (Config.HasMidi) {
				Dispatcher.Invoke(() => {
					toggleButtonStop.IsChecked = true;
					toggleButtonPlay.IsChecked = false;
					toggleButtonPause.IsChecked = false;
					OnPlaybackUIUpdate(null, null);
					playbackUITimer.Stop();
					labelClientPlaying.Content = "Stopped";
				});
			}
			if (server != null)
				HostSongFinished();
			if (client != null)
				ClientSongFinished();
		}

		#endregion
		//========== PLAY NOTE ===========
		#region Play Note

		/**<summary>Plays the specified note.</summary>*/
		private void PlayNote(int semitone) {
			double direction;

			// Calculate the distance per semitone
			double heightRatio = clientArea.Height / 48.0;

			// Shift the note info a valid octave
			if (semitone < 0)
				semitone -= ((semitone - 11) / 12) * 12;
			if (semitone > 24) // Remember, there's one extra C
				semitone -= ((semitone - 14) / 12) * 12;

			// The center of the player
			double centerx = clientArea.Width / 2;
			double centery = clientArea.Height / 2 - (mounted ? Config.Mount.Offset : 0);
			
			// The right & left boundary before notes go bad
			double maxAngle = (Math.Acos(centery / (heightRatio * semitone)) / Math.PI * 180) % 360;
			double minAngle = 360 - maxAngle;
			double rangeStart = ((Config.ProjectileAngle - Config.ProjectileRange / 2 + 360)) % 360;
			double rangeEnd = (Config.ProjectileAngle + Config.ProjectileRange / 2) % 360;

			// Fix mount offsets reducing vertical note range
			int testY = (int)(centery - heightRatio * semitone);
			if (testY < 0 && ((rangeStart > minAngle || rangeStart < maxAngle) ||
				(rangeEnd > minAngle || rangeEnd < maxAngle) || (rangeStart > rangeEnd))) {
				#region Mount Offset Range
				double start1 = 0, start2 = 0;
				double stop1 = 0, stop2 = 0;
				if (rangeStart <= minAngle && rangeStart >= maxAngle) {
					start1 = rangeStart;
					stop1 = minAngle;
				}
				if (rangeEnd >= maxAngle) {
					start2 = maxAngle;
					stop2 = rangeEnd;
				}
				if (start1 == 0 && start2 == 0) {
					if (Config.ProjectileAngle == 0)
						direction = (rand.Next() % 2 == 0 ? minAngle : maxAngle);
					else if (Config.ProjectileAngle < 180)
						direction = maxAngle;
					else
						direction = minAngle;
				}
				else if (start2 == 0) {
					direction = start1 + rand.NextDouble() * (stop1 - start1);
				}
				else if (start1 == 0) {
					direction = start2 + rand.NextDouble() * (stop2 - start2);
				}
				else {
					double angle = rand.NextDouble() * ((stop1 - start1) + (stop2 - start2));
					if (angle >= (stop1 - start1))
						direction = start2 + (angle - (stop1 - start1));
					else
						direction = start1 + angle;
				}
				direction = (direction + 270) / 180 * Math.PI;
				#endregion
			}
			else {
				direction = (Config.ProjectileAngle - Config.ProjectileRange / 2 + rand.NextDouble() * Config.ProjectileRange + 270) / 180 * Math.PI;
			}

			// Calculate the mouse position
			int x = (int)(centerx + Math.Cos(direction) * (heightRatio * semitone));
			int y = (int)(centery + Math.Sin(direction) * (heightRatio * semitone));
			if (x < 0) x = 0;
			if (x >= (int)clientArea.Width) x = (int)clientArea.Width - 1;
			if (y < 0) y = 0;
			if (y >= (int)clientArea.Height) y = (int)clientArea.Height - 1;

			// Offset based on the window position
			x += (int)clientArea.X;
			y += (int)clientArea.Y;

			// Click to perform the note
			MouseControl.SimulateClick(x, y, Config.ClickTime);
		}

		#endregion
	}
}
