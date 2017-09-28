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

			textBox.Text = Config.ExecutableNames;
			textBox.Focus();
			textBox.Select(textBox.Text.Length, 0);
		}

		#endregion
		//============ EVENTS ============
		#region Events

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
				Config.ExecutableNames = window.textBox.Text;
			}
		}

		#endregion
	}
}
