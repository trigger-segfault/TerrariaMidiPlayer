using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using TerrariaMidiPlayer.Controls;

namespace TerrariaMidiPlayer {
	/**<summary>The main window running Terraria Midi Player.</summary>*/
	public partial class MainWindow : Window {
		//============ EVENTS ============
		#region Events
		//--------------------------------
		#region Settings

		private void OnUseTimeChanged(object sender, ValueChangedEventArgs<int> e) {
			if (!loaded)
				return;
			Config.UseTime = numericUseTime.Value;
		}
		private void OnClickTimeChanged(object sender, ValueChangedEventArgs<int> e) {
			if (!loaded)
				return;
			Config.ClickTime = numericClickTime.Value;
		}
		private void OnChecksChanged(object sender, ValueChangedEventArgs<int> e) {
			if (!loaded)
				return;
			Config.CheckFrequency = numericChecks.Value;
		}
		private void OnChecksEnabledClicked(object sender, RoutedEventArgs e) {
			if (!loaded)
				return;
			Config.ChecksEnabled = checkBoxChecks.IsChecked.Value;
			numericChecks.IsEnabled = Config.ChecksEnabled;
		}
		private void OnMountedChanged(object sender, RoutedEventArgs e) {
			if (!loaded)
				return;
			mounted = checkBoxMounted.IsChecked.Value;
		}
		private void OnTalkingChanged(object sender, RoutedEventArgs e) {
			if (!loaded)
				return;
			talking = checkBoxTalking.IsChecked.Value;
		}
		private void OnMountChanged(object sender, SelectionChangedEventArgs e) {
			if (!loaded)
				return;
			Config.MountIndex = comboBoxMount.SelectedIndex;
		}
		private void OnProjectileChanged(object sender, RoutedEventArgs e) {
			Config.ProjectileAngle = projectileControl.Angle;
			Config.ProjectileRange = projectileControl.Range;
		}

		#endregion
		//--------------------------------
		#region Player

		private void OnPianoToggled(object sender, RoutedEventArgs e) {
			if (!loaded)
				return;
			if (Config.Sequencer.Position > 1)
				Pause();
			else
				Stop();
			Config.PianoMode = toggleButtonPiano.IsChecked.Value;
		}
		private void OnStopToggled(object sender, RoutedEventArgs e) {
			if (server != null)
				HostStop();
			else
				Stop();
		}
		private void OnPlayToggled(object sender, RoutedEventArgs e) {
			if (server != null && !Config.PianoMode)
				HostStartPlay();
			else
				Play();
		}
		private void OnPauseToggled(object sender, RoutedEventArgs e) {
			if (server != null)
				HostStop();
			else
				Pause();
		}
		private void OnMidiPositionChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
			if (!loaded)
				return;
			Config.Sequencer.Position = Config.Sequencer.ProgressToTicks(e.NewValue);
			labelMidiPosition.Content = MillisecondsToString(Config.Sequencer.CurrentTime) + "/" + MillisecondsToString(Config.Sequencer.Duration);
			toggleButtonStop.IsChecked = (e.NewValue == 0);
			toggleButtonPlay.IsChecked = false;
			toggleButtonPause.IsChecked = (e.NewValue != 0);
		}

		#endregion
		//--------------------------------
		#endregion
	}
}
