using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using TerrariaMidiPlayer.Windows;

namespace TerrariaMidiPlayer {
	/**<summary>The main window running Terraria Midi Player.</summary>*/
	public partial class MainWindow : Window {
		//============ EVENTS ============
		#region Events
		//--------------------------------
		#region File

		private void OnExit(object sender, RoutedEventArgs e) {
			Close();
		}

		#endregion
		//--------------------------------
		#region Options

		private void OnChangeKeybinds(object sender, RoutedEventArgs e) {
			StopOrPause();
			loaded = false;
			ChangeKeybindsDialog.ShowDialog(this);
			loaded = true;
			UpdateKeybindTooltips();
			if (!Config.DisableMountWhenTalking) {
				talking = false;
				checkBoxTalking.IsChecked = false;
			}
			checkBoxTalking.IsEnabled = Config.DisableMountWhenTalking;
		}
		private void OnExecutableName(object sender, RoutedEventArgs e) {
			loaded = false;
			ExecutableNameDialog.ShowDialog(this);
			loaded = true;
		}
		private void OnUseTrackNames(object sender, RoutedEventArgs e) {
			Config.UseTrackNames = menuItemTrackNames.IsChecked;
			UpdateMidi();
		}
		private void OnPianoModeWrap(object sender, RoutedEventArgs e) {
			Config.WrapPianoMode = menuItemWrapPianoMode.IsChecked;
		}
		private void OnPianoModeSkip(object sender, RoutedEventArgs e) {
			Config.SkipPianoMode = menuItemSkipPianoMode.IsChecked;
		}
		private void OnSaveConfig(object sender, RoutedEventArgs e) {
			SaveConfig(false);
		}

		#endregion
		//--------------------------------
		#region Help

		private void OnAbout(object sender, RoutedEventArgs e) {
			loaded = false;
			AboutWindow.Show(this);
			loaded = true;
		}
		private void OnHelp(object sender, RoutedEventArgs e) {
			Process.Start("https://github.com/trigger-death/TerrariaMidiPlayer/wiki");
		}
		private void OnCredits(object sender, RoutedEventArgs e) {
			loaded = false;
			CreditsWindow.Show(this);
			loaded = true;
		}
		private void OnOpenOnGitHub(object sender, RoutedEventArgs e) {
			Process.Start("https://github.com/trigger-death/TerrariaMidiPlayer");
		}
		private void OnAboutInstruments(object sender, RoutedEventArgs e) {
			Process.Start("https://terraria.gamepedia.com/Harp");
		}

		#endregion
		//--------------------------------
		#endregion
	}
}
