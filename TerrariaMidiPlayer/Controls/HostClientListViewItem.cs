using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TerrariaMidiPlayer.Syncing;

namespace TerrariaMidiPlayer.Controls {
	/**<summary>A compact list view item for clients listed by the host.</summary>*/
	public class HostClientListViewItem : ListViewItem {
		//=========== MEMBERS ============
		#region Members

		// Controls
		private Grid grid;
		private TextBlock textBlockUsername;
		private TextBlock textBlockReady;

		/**<summary>The username of the client.</summary>*/
		private string username = "";
		/**<summary>True if the client is ready.</summary>*/
		private ReadyStates ready = ReadyStates.NotReady;

		#endregion
		//========= CONSTRUCTORS =========
		#region Constructors

		/**<summary>Constructs the host client list view item.</summary>*/
		public HostClientListViewItem(string username) : base() {
			this.username = username;

			InitializeComponents();
		}
		/**<summary>Initializes the components of the control.</summary>*/
		private void InitializeComponents() {
			grid = new Grid();
			grid.VerticalAlignment = VerticalAlignment.Top;
			grid.Height = 18;
			Content = grid;

			textBlockUsername = new TextBlock();
			textBlockUsername.HorizontalAlignment = HorizontalAlignment.Stretch;
			textBlockUsername.VerticalAlignment = VerticalAlignment.Center;
			textBlockUsername.FontSize = 10;
			textBlockUsername.Width = Double.NaN;
			textBlockUsername.Margin = new Thickness(2, 0, 132, 0);
			textBlockUsername.Text = username;
			textBlockUsername.TextTrimming = TextTrimming.CharacterEllipsis;
			grid.Children.Add(textBlockUsername);

			textBlockReady = new TextBlock();
			textBlockReady.HorizontalAlignment = HorizontalAlignment.Right;
			textBlockReady.VerticalAlignment = VerticalAlignment.Center;
			textBlockReady.FontSize = 10;
			textBlockReady.Width = 65;
			textBlockReady.Margin = new Thickness(0, 0, 2, 0);
			switch (ready) {
				case ReadyStates.None:
					textBlockReady.Text = "";
					break;
				case ReadyStates.NotReady:
					textBlockReady.Text = "Not Ready";
					textBlockReady.Foreground = Brushes.Red;
					break;
				case ReadyStates.Ready:
					textBlockReady.Text = "Ready";
					textBlockReady.Foreground = Brushes.Green;
					break;
			}
			grid.Children.Add(textBlockReady);

			Loaded += OnLoaded;
		}

		#endregion
		//========== PROPERTIES ==========
		#region Properties

		/**<summary>The username of the client.</summary>*/
		public string Username {
			get { return username; }
		}
		/**<summary>True if the client is ready.</summary>*/
		public ReadyStates Ready {
			get { return ready; }
			set {
				ready = value;
				switch (value) {
				case ReadyStates.None:
					textBlockReady.Text = "";
					break;
				case ReadyStates.NotReady:
					textBlockReady.Text = "Not Ready";
					textBlockReady.Foreground = Brushes.Red;
					break;
				case ReadyStates.Ready:
					textBlockReady.Text = "Ready";
					textBlockReady.Foreground = Brushes.Green;
					break;
				}
			}
		}

		#endregion
		//============ EVENTS ============
		#region Events

		private void OnLoaded(object sender, RoutedEventArgs e) {
			Width = ((ListView)Parent).ActualWidth - 4;
			grid.Width = Width - 8;
		}

		#endregion
	}
}
