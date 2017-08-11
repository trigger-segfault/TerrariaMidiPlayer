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
using System.Windows.Shapes;

namespace TerrariaMidiPlayer {
	/// <summary>
	/// Interaction logic for EditKeybindsDialog.xaml
	/// </summary>
	public partial class ChangeKeybindsDialog : Window {
		
		public ChangeKeybindsDialog(Keybind play, Keybind pause, Keybind stop, Keybind close) {
			InitializeComponent();
			keybindReaderPlay.Keybind = play;
			keybindReaderPause.Keybind = pause;
			keybindReaderStop.Keybind = stop;
			keybindReaderClose.Keybind = close;
		}

		private void OnOKClicked(object sender, RoutedEventArgs e) {
			DialogResult = true;
		}
		
		public static bool ShowDialog(Window owner, ref Keybind play, ref Keybind pause, ref Keybind stop, ref Keybind close) {
			ChangeKeybindsDialog window = new ChangeKeybindsDialog(play, pause, stop, close);
			window.Owner = owner;
			var result = window.ShowDialog();
			if (result != null && result.Value) {
				play = window.keybindReaderPlay.Keybind;
				pause = window.keybindReaderPause.Keybind;
				stop = window.keybindReaderStop.Keybind;
				close = window.keybindReaderClose.Keybind;
				return true;
			}
			return false;
		}
	}
}
