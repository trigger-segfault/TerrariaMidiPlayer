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
	/// Interaction logic for EditNameWindow.xaml
	/// </summary>
	public partial class EditNameDialog : Window {

		public EditNameDialog(string name) {
			InitializeComponent();

			textBox.Text = name;
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

		public static string ShowDialog(Window owner, string name) {
			EditNameDialog window = new EditNameDialog(name);
			window.Owner = owner;
			var result = window.ShowDialog();
			if (result != null && result.Value) {
				return window.textBox.Text;
			}
			return null;
		}
	}
}
