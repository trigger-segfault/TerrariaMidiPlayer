using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using TerrariaMidiPlayer.Controls;
using TerrariaMidiPlayer.Syncing;
using TerrariaMidiPlayer.Windows;
using Timer = System.Timers.Timer;

namespace TerrariaMidiPlayer {
	/**<summary>The main window running Terraria Midi Player.</summary>*/
	public partial class MainWindow : Window {
		//=========== MEMBERS ============
		#region Members

		private Server server = null;
		private Dictionary<string, User> userMap = new Dictionary<string, User>();
		private List<User> userList = new List<User>();
		private Thread hostPlayThread;

		#endregion
		//========= CONSTRUCTORS =========
		#region Constructors

		/**<summary>Initializes the host settings.</summary>*/
		private void InitializeHost() {
			server = null;
			userMap = new Dictionary<string, User>();
			userList = new List<User>();
			hostPlayThread = new Thread(HostPlay);

			syncTime = DateTime.UtcNow;
			syncTickCount = unchecked((uint)Environment.TickCount);

			// Set the control states
			gridSyncHost.Visibility = Visibility.Hidden;
			gridHostPlaying.Visibility = Visibility.Hidden;
			textBoxHostNextSong.IsEnabled = false;
			buttonHostAssignSong.IsEnabled = false;
			listViewClients.IsEnabled = false;
			numericHostWait.IsEnabled = false;
		}

		#endregion
		//========== CONNECTION ==========
		#region Connection

		/**<summary>Starts up the host server.</summary>*/
		private void HostStartup() {
			int port = numericHostPort.Value;
			server = new Server();
			server.MessageReceived += OnHostMessageReceived;
			server.ClientConnected += OnHostClientConnected;
			server.ClientConnectionLost += OnHostClientConnectionLost;
			server.Error += OnHostError;
			if (!server.Start(port)) {
				TriggerMessageBox.Show(this, MessageIcon.Warning, "Failed to start host server!", "Host Error");
				server = null;
			}
			else {
				Dispatcher.Invoke(() => {
					comboBoxSyncType.IsEnabled = false;
					textBoxHostPassword.IsEnabled = false;
					numericHostPort.IsEnabled = false;
					numericHostWait.IsEnabled = true;
					textBoxHostNextSong.IsEnabled = true;
					buttonHostAssignSong.IsEnabled = true;
					listViewClients.IsEnabled = true;
					buttonHostStartup.Content = "Shutdown";
				});
			}
		}
		/**<summary>Shuts down the host server.</summary>*/
		private void HostShutdown() {
			Stop();
			server.Stop();
			server = null;
			userList.Clear();
			userMap.Clear();

			Dispatcher.Invoke(() => {
				listViewClients.Items.Clear();

				gridHostPlaying.Visibility = Visibility.Hidden;
				labelHostPlaying.Content = "Stopped";

				comboBoxSyncType.IsEnabled = true;
				textBoxHostPassword.IsEnabled = true;
				numericHostPort.IsEnabled = true;
				numericHostWait.IsEnabled = false;
				textBoxHostNextSong.IsEnabled = false;
				buttonHostAssignSong.IsEnabled = false;
				listViewClients.IsEnabled = false;
				buttonHostStartup.Content = "Startup";
				textBoxHostNextSong.Text = "";
			});
		}
		
		#endregion
		//============ EVENTS ============
		#region Events

		private void OnHostStartup(object sender, RoutedEventArgs e) {
			if (server == null)
				HostStartup();
			else
				HostShutdown();
		}
		private void OnHostAssignSong(object sender, RoutedEventArgs e) {
			server.Send(new StringCommand(Commands.AssingSong, Server.ServerName, textBoxHostNextSong.Text));
			for (int i = 0; i < userList.Count; i++) {
				userList[i].Ready = ReadyStates.NotReady;
				((HostClientListViewItem)listViewClients.Items[i]).Ready = userList[i].Ready;
			}
		}
		private void OnHostPortChanged(object sender, ValueChangedEventArgs<int> e) {
			if (!loaded)
				return;
			Config.Syncing.HostPort = numericHostPort.Value;
		}
		private void OnHostPasswordChanged(object sender, TextChangedEventArgs e) {
			if (!loaded)
				return;
			Config.Syncing.HostPassword = textBoxHostPassword.Text;
		}
		private void OnHostWaitChanged(object sender, ValueChangedEventArgs<int> e) {
			if (!loaded)
				return;
			Config.Syncing.HostWait = numericHostWait.Value;
		}

		#endregion
		//========= HOST EVENTS ==========
		#region Host Events

		private void OnHostMessageReceived(Server server, ServerConnection connection, byte[] data, int size) {
			switch (Command.GetCommandType(data, data.Length)) {
			case Commands.Login: {
					var cmd = new StringCommand(data, data.Length);
					connection.User.IPAddress = connection.IPAddress;
					connection.User.Port = connection.Port;
					if (cmd.Text != Config.Syncing.HostPassword) {
						server.SendToConnection(new Command(Commands.InvalidPassword, Server.ServerName), connection);
					}
					else if (userMap.ContainsKey(cmd.Name)) {
						server.SendToConnection(new Command(Commands.NameTaken, Server.ServerName), connection);
					}
					else {
						connection.User.Name = cmd.Name;
						connection.IsLoggedIn = true;
						AddUser(connection.User);
						server.SendTo(new Command(Commands.AcceptedUser, Server.ServerName), cmd.Name);
					}
					break;
				}
			case Commands.Ready: {
					connection.User.Ready = ReadyStates.Ready;
					int index = userList.IndexOf(connection.User);
					if (index != -1) {
						Dispatcher.Invoke(() => {
							((HostClientListViewItem)listViewClients.Items[index]).Ready = ReadyStates.Ready;
						});
					}
					break;
				}
			case Commands.NotReady: {
					connection.User.Ready = ReadyStates.NotReady;
					int index = userList.IndexOf(connection.User);
					if (index != -1) {
						Dispatcher.Invoke(() => {
							((HostClientListViewItem)listViewClients.Items[index]).Ready = ReadyStates.NotReady;
						});
					}
					break;
				}
			}
		}
		private void OnHostClientConnected(Server server, ServerConnection connection) {

		}
		private void OnHostClientConnectionLost(Server server, ServerConnection connection) {
			RemoveUser(connection.Username);
		}
		private void OnHostError(Server server, Exception ex) {
			var result = TriggerMessageBox.Show(this, MessageIcon.Error, "A host error occurred. Would you like to see the error?", "Host Error", MessageBoxButton.YesNo);
			if (result == MessageBoxResult.Yes)
				ErrorMessageBox.Show(ex);
		}

		#endregion
		//============= PLAY =============
		#region Play

		/**<summary>Starts the host play thread.</summary>*/
		private void HostStartPlay() {
			if (hostPlayThread.ThreadState == System.Threading.ThreadState.Unstarted) {
				hostPlayThread.Start();
			}
			else if (hostPlayThread.ThreadState == System.Threading.ThreadState.Stopped) {
				hostPlayThread = new Thread(HostPlay);
				hostPlayThread.Start();
			}
		}
		/**<summary>Starts the countdown to play the song and tells all the clients to do the same.</summary>*/
		private void HostPlay() {
			try {
				// Calculate playtime
				DateTime playTime = CalculateSyncDateTime();
				playTime = playTime.AddMilliseconds(numericHostWait.Value);
				server.IsPlaying = true;

				// Tell every user to play
				foreach (User user in userList) {
					server.SendToNow(new TimeCommand(Commands.PlaySong, Server.ServerName, playTime), user.Name);
				}

				TimeSpan difference = playTime - CalculateSyncDateTime();
				while (difference.TotalMilliseconds > 0) {
					if (difference.TotalMilliseconds >= 250) {
						// See if we need to break out of the look and return
						if (server == null)
							return;
						lock (server) {
							if (!server.IsPlaying) {
								Dispatcher.Invoke(() => {
									gridHostPlaying.Visibility = Visibility.Hidden;
									labelHostPlaying.Content = "Stopped";
								});
								return;
							}
						}

						// Update the playing in time
						Dispatcher.Invoke(() => {
							labelHostPlaying.Content = "Playing in " + MillisecondsToString((int)difference.TotalMilliseconds, false, true);
						});

						// Wait a bit
						Thread.Sleep(20);
					}
					else {
						// Wait a bit less
						Thread.Sleep(2);
					}
					difference = playTime - CalculateSyncDateTime();
				}

				// Start playing
				Play();

				// Update the control states
				Dispatcher.Invoke(() => {
					labelHostPlaying.Content = "Playing now";
				});
			}
			catch (Exception) { }
		}
		/**<summary>Stops the song and every connected client.</summary>*/
		private void HostStop() {
			Stop();
			server.IsPlaying = false;
			// Tell everyone else to stop
			server.Send(new Command(Commands.StopSong, Server.ServerName));
			// Update the control states
			Dispatcher.Invoke(() => {
				gridHostPlaying.Visibility = Visibility.Hidden;
				labelHostPlaying.Content = "Stopped";
			});
		}
		/**<summary>Updates after the host has finished playing.</summary>*/
		private void HostSongFinished() {
			server.IsPlaying = false;
			// Update the control states
			Dispatcher.Invoke(() => {
				gridHostPlaying.Visibility = Visibility.Hidden;
				labelHostPlaying.Content = "Stopped";
			});
		}

		#endregion
		//============ USERS =============
		#region Users

		/**<summary>Adds the user to the list.</summary>*/
		private void AddUser(User user) {
			user.Ready = ReadyStates.NotReady;
			userMap.Add(user.Name, user);
			userList.Add(user);
			// Update the controls
			Dispatcher.Invoke(() => {
				listViewClients.Items.Add(new HostClientListViewItem(user.Name));
			});
		}
		/**<summary>Removes the user from the list.</summary>*/
		private void RemoveUser(string username) {
			if (userMap.ContainsKey(username)) {
				int index = userList.FindIndex(u => u.Name == username);
				userMap.Remove(username);
				userList.RemoveAt(index);
				// Update the controls
				Dispatcher.Invoke(() => {
					listViewClients.Items.RemoveAt(index);
				});
			}
		}

		#endregion
	}
}
