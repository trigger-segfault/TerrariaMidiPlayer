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
using TerrariaMidiPlayer.Util;

namespace TerrariaMidiPlayer.Windows {
	/// <summary>
	/// Interaction logic for ExecutableNameDialog.xaml
	/// </summary>
	public partial class ExecutableNameDialog : Window {

		public ExecutableNameDialog() {
			InitializeComponent();

			textBox.Text = TerrariaWindowLocator.ExeName;
			textBox.Focus();
			textBox.SelectAll();
		}

		private void OnOKClicked(object sender, RoutedEventArgs e) {
			DialogResult = true;
		}

		private void OnPreviewKeyDown(object sender, KeyEventArgs e) {
			if (e.Key == Key.Return) {
				DialogResult = true;
			}
		}

		public static void ShowDialog(Window owner) {
			ExecutableNameDialog window = new ExecutableNameDialog();
			window.Owner = owner;
			var result = window.ShowDialog();
			if (result != null && result.Value) {
				TerrariaWindowLocator.ExeName = window.textBox.Text;
			}
		}
	}
}
