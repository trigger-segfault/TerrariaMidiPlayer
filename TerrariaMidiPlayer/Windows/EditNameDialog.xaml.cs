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

namespace TerrariaMidiPlayer.Windows {
	/**<summary>A dialog for editing the name of something.</summary>*/
	public partial class EditNameDialog : Window {
		//========= CONSTRUCTORS =========
		#region Constructors

		/**<summary>Constructs the edit name window.</summary>*/
		public EditNameDialog(string name) {
			InitializeComponent();

			textBox.Text = name;
			textBox.Focus();
			textBox.SelectAll();
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

		/**<summary>Shows the edit name window.</summary>*/
		public static string ShowDialog(Window owner, string name) {
			EditNameDialog window = new EditNameDialog(name);
			window.Owner = owner;
			var result = window.ShowDialog();
			if (result != null && result.Value) {
				return window.textBox.Text;
			}
			return null;
		}

		#endregion
	}
}
