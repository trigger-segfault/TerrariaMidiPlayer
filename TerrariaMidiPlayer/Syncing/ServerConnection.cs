using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TerrariaMidiPlayer.Syncing {
	public class ServerConnection {

		private const int IdleTime = 20;
		private const int VerifyTime = 2000;
		private const int BufferSize = 1024;

		private List<byte[]> messagesToSend = new List<byte[]>();
		private DateTime lastVerifyTime = DateTime.UtcNow;
		private TcpClient client = null;
		private int attemptCount = 0;
		private bool markedForRemoval = false;

		private User user = new User();
		private bool loggedIn = false;

		private bool safelyDisconnected = false;

		private Thread callbackThread = null;

		public ServerConnection(TcpClient client) {
			this.client = client;
			user.IPAddress = ((IPEndPoint)client.Client.RemoteEndPoint).Address;
			user.Port = ((IPEndPoint)client.Client.RemoteEndPoint).Port;
		}

		public TcpClient TcpClient {
			get { return client; }
		}

		public DateTime LastVerifyTime {
			get { return lastVerifyTime; }
		}

		public Thread CallbackThread {
			get { return callbackThread; }
			set { callbackThread = value; }
		}


		public User User {
			get { return user; }
		}
		public string Username {
			get { return user.Name; }
		}
		public IPAddress IPAddress {
			get { return user.IPAddress; }
		}
		public int Port {
			get { return user.Port; }
		}
		public bool IsLoggedIn {
			get { return loggedIn; }
			set { loggedIn = value; }
		}
		public bool IsNameAssigned {
			get { return user.Name != ""; }
		}

		public bool SafelyDisconnected {
			get { return safelyDisconnected; }
			set { safelyDisconnected = value; }
		}

		public bool IsConnected {
			get { return (client != null && client.Connected); }
		}

		public bool IsMarkedForRemoval {
			get { return markedForRemoval; }
			set { markedForRemoval = value; }
		}

		public bool HasMoreWork {
			get { return messagesToSend.Count > 0 || (client.Available > 0 && CanStartNewThread); }
		}
		public bool HasMoreMessages {
			get { return messagesToSend.Count > 0; }
		}
		public bool CanStartNewThread {
			get {
				if (callbackThread == null)
					return true;
				return (callbackThread.ThreadState & (ThreadState.Aborted | ThreadState.Stopped)) != 0 &&
						(callbackThread.ThreadState & ThreadState.Unstarted) == 0;
			}
		}

		public bool VerifyConnection() {
			try {
				bool connected = client.Client.Available != 0 ||
					!client.Client.Poll(1, SelectMode.SelectRead) ||
					client.Client.Available != 0;
				lastVerifyTime = DateTime.UtcNow;
				return connected;
			}
			catch {
				return false;
			}
		}

		public bool ProcessOutgoing(int maxSendAttempts) {
			lock (client) {
				if (!client.Connected) {
					messagesToSend.Clear();
					return false;
				}

				if (messagesToSend.Count == 0) {
					return false;
				}

				NetworkStream stream = client.GetStream();
				try {
					stream.Write(messagesToSend[0], 0, messagesToSend[0].Length);

					lock (messagesToSend) {
						messagesToSend.RemoveAt(0);
					}
					attemptCount = 0;
				}
				catch (IOException) {
					//occurs when there's an error writing to network
					attemptCount++;
					if (attemptCount >= maxSendAttempts) {
						//TODO log error

						lock (messagesToSend) {
							messagesToSend.RemoveAt(0);
						}
						attemptCount = 0;
						client.Close();
					}
				}
				catch (ObjectDisposedException) {
					//occurs when stream is closed
					client.Close();
					return false;
				}
			}
			return messagesToSend.Count != 0;
		}

		public void Send(Command command) {
			lock (messagesToSend) {
				messagesToSend.Add(command.GetBytes());
			}
		}
		public void SendNow(Command command, int maxSendAttempts) {
			lock (messagesToSend) {
				messagesToSend.Insert(0, command.GetBytes());
			}
			bool success = false;
			attemptCount = 0;
			while (!success && attemptCount < maxSendAttempts) {
				NetworkStream stream = client.GetStream();
				try {
					stream.Write(messagesToSend[0], 0, messagesToSend[0].Length);

					lock (messagesToSend) {
						messagesToSend.RemoveAt(0);
					}
					attemptCount = 0;
					success = true;
				}
				catch (IOException) {
					//occurs when there's an error writing to network
					attemptCount++;
					if (attemptCount >= maxSendAttempts) {
						//TODO log error

						lock (messagesToSend) {
							messagesToSend.RemoveAt(0);
						}
						attemptCount = 0;
					}
				}
				catch (ObjectDisposedException) { }
			}
		}

		public void ClearMessages() {
			lock (messagesToSend) {
				messagesToSend.Clear();
			}
		}

		public void ForceDisconnect() {
			lock (client) {
				client.Close();
			}
		}
	}
}
