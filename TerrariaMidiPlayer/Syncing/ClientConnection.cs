using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TerrariaMidiPlayer.Syncing {
	public delegate void ClientMessageReceived(ClientConnection client, byte[] data, int size);
	public delegate void ClientConnectionStatus(ClientConnection client);

	public class ClientConnection {

		private const int IdleTime = 50;
		private const int PlayingIdleTime = 2000;
		private const int VerifyTime = 2000;
		private const int MaxSendAttempts = 8;

		private List<byte[]> messagesToSend = new List<byte[]>();
		private Thread senderThread = null;
		private Thread callbackThread = null;
		private DateTime lastVerifyTime = DateTime.UtcNow;
		private TcpClient client = null;
		private User user = new User();
		private int attemptCount = 0;
		private bool playing = false;

		public event ClientMessageReceived MessageReceived;
		public event ClientConnectionStatus ConnectionLost;

		public ClientConnection() {
			
		}

		public IPAddress IPAddress {
			get { return user.IPAddress; }
		}

		public int Port {
			get { return user.Port; }
		}
		
		public string Username {
			get { return user.Name; }
			set { user.Name = value; }
		}
		public bool IsAssigned {
			get { return user.Name != ""; }
		}

		public bool IsConnected {
			get { return (client != null && client.Connected); }
		}
		public bool HasMoreWork {
			get { return messagesToSend.Count > 0 || (client.Available > 0 && !CanStartNewThread); }
		}
		public bool CanStartNewThread {
			get {
				if (callbackThread == null)
					return true;
				return (callbackThread.ThreadState & (ThreadState.Aborted | ThreadState.Stopped)) != 0 &&
						(callbackThread.ThreadState & ThreadState.Unstarted) == 0;
			}
		}

		public bool IsPlaying {
			get { return playing; }
			set { playing = value; }
		}

		public bool Connect(IPAddress ipAddress, int port) {
			if (IsConnected)
				Disconnect();

			try {
				messagesToSend.Clear();
				client = new TcpClient();
				client.Connect(ipAddress, port);
				senderThread = new Thread(SenderThread);
				senderThread.Start();
				user.IPAddress = ipAddress;
				user.Port = port;
				attemptCount = 0;
				return true;
			}
			catch (Exception) {
				client = null;
				return false;
			}
		}

		public bool Disconnect(bool connectionLost = false) {
			if (!IsConnected)
				return false;

			try {
				user = new User();


				try {
					if (senderThread.IsAlive) {
						senderThread.Interrupt();

						Thread.Yield();
						if (senderThread.IsAlive) {
							senderThread.Abort();
						}
					}
				}
				catch (SecurityException) { }
				catch (ThreadAbortException) { }
				catch (ThreadInterruptedException) { }
				client.Close();
				client = null;
				if (connectionLost)
					ConnectionLost?.Invoke(this);
				return true;
			}
			catch (Exception) {
				client = null;
				if (connectionLost)
					ConnectionLost?.Invoke(this);
				return false;
			}
		}

		private bool VerifyConnection() {
			try {
				bool connected = client.Client.Available != 0 ||
					!client.Client.Poll(1, SelectMode.SelectRead) ||
					client.Client.Available != 0;
				lastVerifyTime = DateTime.UtcNow;
				return connected;
			}
			catch (ThreadAbortException) { }
			catch (ThreadInterruptedException) { }
			catch (Exception) {
				client.Close();
			}
			return false;
		}

		private void SenderThread() {
			try {
				while (IsConnected) {
					try {
						bool moreWork = false;
						if (callbackThread != null && CanStartNewThread) {
							//try {
								callbackThread = null;
							//}
							//catch (Exception ex) { }
						}

						if (callbackThread != null) {

						}
						else if (VerifyConnection()) {
							moreWork = moreWork || ProcessConnection();
						}
						else {
							callbackThread = new Thread(() => {
								Disconnect(true);
							});
							callbackThread.Start();
						}

						if (!moreWork) {
							Thread.Yield();
							if (HasMoreWork)
								moreWork = true;
						}
					}
					catch (SocketException) {
						client.Close();
					}
					Thread.Sleep(playing ? PlayingIdleTime : IdleTime);
				}
			}
			catch (ThreadAbortException) { }
			catch (ThreadInterruptedException) { }
		}

		private bool ProcessConnection() {
			bool moreWork = false;
			if (ProcessOutgoing()) {
				moreWork = true;
			}

			if (MessageReceived != null && client.Available > 0) {
				callbackThread = new Thread(() => {
					NetworkStream stream = client.GetStream();
					byte[] data = new byte[client.Available];
					stream.Read(data, 0, data.Length);

					MessageReceived?.Invoke(this, data, data.Length);
				});
				callbackThread.Start();
				Thread.Yield();
			}
			return moreWork;
		}


		public bool ProcessOutgoing() {
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
					if (attemptCount >= MaxSendAttempts) {
						//TODO log error

						lock (messagesToSend) {
							messagesToSend.RemoveAt(0);
						}
						attemptCount = 0;
						client.Close();
						return false;
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
			byte[] array = command.GetBytes();
			lock (messagesToSend) {
				messagesToSend.Add(array);
			}
		}
		public void SendNow(Command command) {
			lock (messagesToSend) {
				messagesToSend.Insert(0, command.GetBytes());
			}
			bool success = false;
			attemptCount = 0;
			while (!success && attemptCount < MaxSendAttempts) {
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
					if (attemptCount >= MaxSendAttempts) {
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
	}
}
