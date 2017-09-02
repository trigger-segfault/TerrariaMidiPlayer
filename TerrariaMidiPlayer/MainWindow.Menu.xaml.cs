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
			Stop();
			loaded = false;
			ChangeKeybindsDialog.ShowDialog(this);
			loaded = true;
			UpdateKeybindTooltips();
		}
		private void OnExecutableName(object sender, RoutedEventArgs e) {
			loaded = false;
			ExecutableNameDialog.ShowDialog(this);
			loaded = true;
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
