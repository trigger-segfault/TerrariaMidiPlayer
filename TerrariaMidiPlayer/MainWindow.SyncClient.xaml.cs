using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
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

		private ClientConnection client = null;
		private bool clientReady = false;
		private Timer clientTimeout = new Timer(4000);
		private bool clientAccepted = false;
		private Stopwatch reconnectWatch = new Stopwatch();
		private Thread clientPlayThread;
		private DateTime clientPlayTime;

		#endregion
		//========= CONSTRUCTORS =========
		#region Constructors

		/**<summary>Initializes the client settings.</summary>*/
		private void InitializeClient() {
			clientTimeout.Elapsed += OnClientConnectingTimeout;
			clientTimeout.AutoReset = false;
			clientPlayThread = new Thread(ClientPlay);

			// Set the control states
			gridSyncClient.Visibility = Visibility.Visible;
			buttonClientReady.IsEnabled = false;
			textBoxClientNextSong.IsEnabled = false;
			numericClientTimeOffset.IsEnabled = false;
		}

		#endregion
		//========== CONNECTION ==========
		#region Connection

		/**<summary>Attempts to connect to the host.</summary>*/
		private void ClientConnect() {
			// Validate input
			int port = Config.Syncing.ClientPort;
			IPAddress ip = IPAddress.Loopback;
			if (Config.Syncing.ClientIPAddress != "" && !IPAddress.TryParse(Config.Syncing.ClientIPAddress, out ip)) {
				TriggerMessageBox.Show(this, MessageIcon.Warning, "The entered IP address in invalid!", "Invalid IP");
				return;
			}
			if (Config.Syncing.ClientUsername == "") {
				TriggerMessageBox.Show(this, MessageIcon.Warning, "Cannot connect with an empty username!", "Invalid Username");
				return;
			}

			// Connect to the host
			client = new ClientConnection();
			client.MessageReceived += OnClientMessageReceived;
			client.ConnectionLost += OnClientConnectionLost;
			if (!client.Connect(ip, port)) {
				TriggerMessageBox.Show(this, MessageIcon.Warning, "Failed to connect to server!", "Connection Failed");
				client = null;
			}
			else {
				// Final setup before waiting to login
				// Set the control states
				Dispatcher.Invoke(() => {
					comboBoxSyncType.IsEnabled = false;
					textBoxClientIP.IsEnabled = false;
					numericClientPort.IsEnabled = false;
					textBoxClientUsername.IsEnabled = false;
					textBoxClientPassword.IsEnabled = false;
					textBoxClientNextSong.IsEnabled = false;
					buttonClientConnect.IsEnabled = false;
					buttonClientConnect.Content = "Connecting...";
				});

				clientTimeout.Start();

				client.Send(new StringCommand(Commands.Login, Config.Syncing.ClientUsername, Config.Syncing.ClientPassword));
			}
		}
		/**<summary>Disconnects from the host.</summary>*/
		private void ClientDisconnect() {
			Stop();
			client.Disconnect();
			client = null;

			clientAccepted = false;
			clientReady = false;
			reconnectWatch.Restart();

			// Set the control states
			Dispatcher.Invoke(() => {
				comboBoxSyncType.IsEnabled = true;
				textBoxClientIP.IsEnabled = true;
				numericClientPort.IsEnabled = true;
				textBoxClientUsername.IsEnabled = true;
				textBoxClientPassword.IsEnabled = true;
				buttonClientReady.IsEnabled = false;
				textBoxClientNextSong.IsEnabled = false;
				buttonClientConnect.IsEnabled = true;
				numericClientTimeOffset.IsEnabled = false;
				buttonClientReady.Content = "Ready";
				buttonClientConnect.Content = "Connect";
				textBoxClientNextSong.Text = "";
				labelClientPlaying.Content = "Stopped";
			});
		}

		#endregion
		//============ EVENTS ============
		#region Events

		private void OnClientConnect(object sender, RoutedEventArgs e) {
			if (client == null)
				ClientConnect();
			else
				ClientDisconnect();
		}
		private void OnClientIPChanged(object sender, TextChangedEventArgs e) {
			if (!loaded)
				return;
			Config.Syncing.ClientIPAddress = textBoxClientIP.Text;
		}
		private void OnClientPortChanged(object sender, ValueChangedEventArgs<int> e) {
			if (!loaded)
				return;
			Config.Syncing.ClientPort = numericClientPort.Value;
		}
		private void OnClientUsernameChanged(object sender, TextChangedEventArgs e) {
			if (!loaded)
				return;
			Config.Syncing.ClientUsername = textBoxClientUsername.Text;
		}
		private void OnClientPasswordChanged(object sender, TextChangedEventArgs e) {
			if (!loaded)
				return;
			Config.Syncing.ClientPassword = textBoxClientPassword.Text;
		}
		private void OnClientReady(object sender, RoutedEventArgs e) {
			if (!clientReady) {
				clientReady = true;
				buttonClientReady.Content = "Not Ready";
				client.Send(new Command(Commands.Ready, Config.Syncing.ClientUsername));
			}
			else {
				clientReady = false;
				buttonClientReady.Content = "Ready";
				client.Send(new Command(Commands.NotReady, Config.Syncing.ClientUsername));
			}
		}
		private void OnClientTimeOffsetChanged(object sender, ValueChangedEventArgs<int> e) {
			if (!loaded)
				return;
			Config.Syncing.ClientTimeOffset = numericClientTimeOffset.Value;
		}

		private void OnClientConnectingTimeout(object sender, ElapsedEventArgs e) {
			if (!clientAccepted && client != null) {
				Dispatcher.Invoke(() => {
					OnClientConnect(null, new RoutedEventArgs());
					if (reconnectWatch.ElapsedMilliseconds < 1500 + (int)clientTimeout.Interval)
						TriggerMessageBox.Show(this, MessageIcon.Warning, "Failed to login! You are trying to reconnect to the server too quickly. Wait at least one second before reconnecting.", "Login Failed");
					else
						TriggerMessageBox.Show(this, MessageIcon.Warning, "Failed to login!", "Login Failed");
				});
			}
		}

		#endregion
		//======== CLIENT EVENTS =========
		#region Client Events

		private void OnClientConnectionLost(ClientConnection client) {
			OnClientConnect(null, new RoutedEventArgs());
		}
		private void OnClientMessageReceived(ClientConnection client, byte[] data, int size) {
			switch (Command.GetCommandType(data, data.Length)) {
			case Commands.InvalidPassword:
				if (!clientAccepted) {
					ClientDisconnect();
					Dispatcher.Invoke(() => {
						if (textBoxClientPassword.Text.Length == 0)
							TriggerMessageBox.Show(this, MessageIcon.Warning, "A password is required.", "Password Required");
						else
							TriggerMessageBox.Show(this, MessageIcon.Warning, "The chosen password is incorrect.", "Invalid Password");
					});
				}
				break;

			case Commands.NameTaken:
				if (!clientAccepted) {
					ClientDisconnect();
					Dispatcher.Invoke(() => {
						if (reconnectWatch.ElapsedMilliseconds < 1200)
							TriggerMessageBox.Show(this, MessageIcon.Warning, "You are trying to reconnect to the server too quickly.\nWait at least one second before reconnecting.", "Name Taken");
						else
							TriggerMessageBox.Show(this, MessageIcon.Warning, "The chosen username is already in use.", "Name Taken");
					});
				}
				break;

			case Commands.AcceptedUser:
				if (!clientAccepted) {
					// Finish logging in
					clientAccepted = true;
					clientTimeout.Stop();

					// Update the control states
					Dispatcher.Invoke(() => {
						textBoxClientNextSong.IsEnabled = true;
						numericClientTimeOffset.IsEnabled = true;
						buttonClientReady.IsEnabled = true;
						buttonClientConnect.IsEnabled = true;
						buttonClientConnect.Content = "Disconnect";
					});
				}
				break;

			case Commands.AssingSong:
				if (clientAccepted) {
					var cmd = new StringCommand(data, data.Length);
					clientReady = false;
					Dispatcher.Invoke(() => {
						textBoxClientNextSong.Text = cmd.Text;
						buttonClientReady.Content = "Ready";
					});
				}
				break;

			case Commands.PlaySong:
				if (clientAccepted && clientReady && !client.IsPlaying) {
					if (clientPlayThread.ThreadState == System.Threading.ThreadState.Unstarted) {
						clientPlayTime = new TimeCommand(data, size).DateTime;
						clientPlayThread.Start();
					}
					else if (clientPlayThread.ThreadState == System.Threading.ThreadState.Stopped) {
						clientPlayTime = new TimeCommand(data, size).DateTime;
						clientPlayThread = new Thread(ClientPlay);
						clientPlayThread.Start();
					}
				}
				break;

			case Commands.StopSong:
				if (clientAccepted && clientReady) {
					ClientStop();
				}
				break;
			}
		}

		#endregion
		//============= PLAY =============
		#region Play

		/**<summary>Starts the client countdown to play the song in sync.</summary>*/
		private void ClientPlay() {
			try {
				clientPlayTime = clientPlayTime.AddMilliseconds(Config.Syncing.ClientTimeOffset);
				client.IsPlaying = true;

				TimeSpan difference = clientPlayTime - CalculateSyncDateTime();
				while (difference.TotalMilliseconds > 0) {
					if (difference.TotalMilliseconds >= 250) {
						// See if we need to break out of the look and return
						if (client == null)
							return;
						lock (client) {
							if (!client.IsPlaying)
								return;
						}

						// Update the playing in time
						Dispatcher.Invoke(() => {
							labelClientPlaying.Content = "Playing in " + MillisecondsToString((int)difference.TotalMilliseconds, false, true);
						});

						// Wait a bit
						Thread.Sleep(20);
					}
					else {
						// Wait a bit less
						Thread.Sleep(2);
					}
					difference = clientPlayTime - CalculateSyncDateTime();
				}

				// Start playing
				Play();

				// Update the control states
				Dispatcher.Invoke(() => {
					if (difference.TotalMilliseconds < -200)
						labelClientPlaying.Content = "Played " + ((long)-difference.TotalMilliseconds).ToString() + "ms early";
					else
						labelClientPlaying.Content = "Playing now";
				});
			}
			catch (Exception) { }
		}
		/**<summary>Stops playing from the client side.</summary>*/
		private void ClientStop() {
			Stop();
			client.IsPlaying = false;
			// Update the control states
			Dispatcher.Invoke(() => {
				labelClientPlaying.Content = "Stopped";
			});
		}
		/**<summary>Updates after the client has finished playing.</summary>*/
		private void ClientSongFinished() {
			client.IsPlaying = false;
			// Update the control states
			Dispatcher.Invoke(() => {
				labelClientPlaying.Content = "Stopped";
			});
		}

		#endregion

	}
}
