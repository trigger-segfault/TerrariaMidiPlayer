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
	public class HostClientListViewItem : ListViewItem {
		
		private Grid grid;
		private TextBlock textBlockUsername;
		private TextBlock textBlockReady;
		//private TextBlock textBlockPing;

		private string username;
		private ReadyStates ready;
		//private int ping;

		public HostClientListViewItem(string username) : base() {
			this.username = username;
			this.ready = ReadyStates.NotReady;
			//this.ping = -1;

			InitializeComponents();
		}

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

			/*textBlockPing = new TextBlock();
			textBlockPing.HorizontalAlignment = HorizontalAlignment.Right;
			textBlockPing.VerticalAlignment = VerticalAlignment.Center;
			textBlockPing.TextAlignment = TextAlignment.Right;
			textBlockPing.FontSize = 10;
			textBlockPing.Width = 65;
			textBlockPing.Margin = new Thickness(0, 0, 2, 0);
			textBlockPing.Text = "??ms";
			grid.Children.Add(textBlockPing);*/

			Loaded += OnLoaded;
		}

		private void OnLoaded(object sender, RoutedEventArgs e) {
			Width = ((ListView)Parent).ActualWidth - 4;
			grid.Width = Width - 8;
		}

		public string Username {
			get { return username; }
		}
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
		/*public int Ping {
			get { return ping; }
			set {
				ping = value;
				textBlockPing.Text = (ping == -1 ? "??" : ping.ToString()) + "ms";
			}
		}*/
	}
}
