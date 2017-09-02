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
	/**<summary>A dialog for changing the name of the Terraria executable.</summary>*/
	public partial class ExecutableNameDialog : Window {
		//========= CONSTRUCTORS =========
		#region Constructors

		/**<summary>Constructs the executable name dialog.</summary>*/
		public ExecutableNameDialog() {
			InitializeComponent();

			textBox.Text = Config.ExecutableName;
			textBox.Focus();
			textBox.SelectAll();
		}

		#endregion
		//============ EVENTS ============
		#region Events

		private void OnPreviewKeyDown(object sender, KeyEventArgs e) {
			if (e.Key == Key.Return) {
				DialogResult = true;
			}
		}
		private void OnOKClicked(object sender, RoutedEventArgs e) {
			DialogResult = true;
		}

		#endregion
		//=========== SHOWING ============
		#region Showing

		/**<summary>Shows the executable name dialog.</summary>*/
		public static void ShowDialog(Window owner) {
			ExecutableNameDialog window = new ExecutableNameDialog();
			window.Owner = owner;
			var result = window.ShowDialog();
			if (result != null && result.Value) {
				Config.ExecutableName = window.textBox.Text;
			}
		}

		#endregion
	}
}
