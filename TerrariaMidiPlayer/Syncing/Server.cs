using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TerrariaMidiPlayer.Syncing {
	public enum ReadyStates {
		None,
		NotReady,
		Ready
	}

	public class User {
		public string Name { get; set; }
		public ReadyStates Ready { get; set; }
		public IPAddress IPAddress { get; set; }
		public int Port { get; set; }

		public User() {
			Name = "";
			Ready = ReadyStates.None;
			IPAddress = IPAddress.None;
			Port = -1;
		}
		public User(string name) {
			Name = name;
			Ready = ReadyStates.None;
			IPAddress = IPAddress.None;
			Port = -1;
		}
	}

	public delegate void ServerClientConnection(Server server, ServerConnection connection);
	public delegate void ServerMessageReceived(Server server, ServerConnection connection, byte[] data, int size);
	public delegate void ServerError(Server server, Exception e);

	public class Server {

		public const string ServerName = "<Server>";

		private const int IdleTime = 50;
		private const int PauseIdleTime = 500;
		private const int MaxSendAttempts = 8;
		private const int MaxCallbackThreads = 10;
		private const int VerifyConnectionInterval = 1000;

		private List<ServerConnection> connections = new List<ServerConnection>();
		private TcpListener listener = null;
		private int port = -1;
		private Thread senderThread = null;
		private Thread listenerThread = null;
		private object activeThreadsLock = new object();
		private int activeThreads = 0;
		private SemaphoreSlim sem;
		private bool waiting = false;
		private bool paused = false;
		private bool precision = false;

		public event ServerClientConnection ClientConnected;
		public event ServerClientConnection ClientConnectionLost;
		public event ServerMessageReceived MessageReceived;
		public event ServerError Error;

		public Server() {
			sem = new SemaphoreSlim(0);
		}

		public int Port {
			get { return port; }
		}

		public bool IsRunning {
			get { return (listener != null); }
		}
		public bool PrecisionMode {
			get { return precision; }
			set { precision = value; }
		}

		public List<ServerConnection> Connections {
			get { return connections; }
		}

		public bool IsPlaying {
			get { return paused; }
			set { paused = value; }
		}

		public bool Start(int port) {
			if (IsRunning)
				Stop();

			try {
				listener = new TcpListener(IPAddress.Any, port);
				listener.Start();
				senderThread = new Thread(SenderThread);
				senderThread.Start();
				listenerThread = new Thread(ListenerThread);
				listenerThread.Start();
				this.port = port;
				return true;
			}
			catch {
				listener = null;
				return false;
			}
		}

		public bool Stop() {
			lock (this) {
				if (!IsRunning)
					return false;

				try {
					port = -1;
					try {
						if (listenerThread.IsAlive) {
							listenerThread.Interrupt();

							Thread.Yield();
							if (listenerThread.IsAlive) {
								listenerThread.Abort();
							}
						}
						listenerThread = null;
					}
					catch (ThreadInterruptedException) { }
					catch (ThreadAbortException) { }
					catch (SecurityException) { }
					try {
						if (senderThread.IsAlive) {
							senderThread.Interrupt();

							Thread.Yield();
							if (senderThread.IsAlive) {
								senderThread.Abort();
							}
						}
						senderThread = null;
					}
					catch (ThreadInterruptedException) { }
					catch (ThreadAbortException) { }
					catch (SecurityException) { }
					foreach (ServerConnection connection in connections) {
						while (connection.ProcessOutgoing(MaxSendAttempts)) ;
						connection.ForceDisconnect();
					}
					listener.Stop();
					listener = null;
					activeThreads = 0;
					connections.Clear();
					GC.Collect();
					return true;
				}
				catch {
					listener = null;
					return false;
				}
			}
		}

		private void SenderThread() {
			try {
				while (IsRunning) {
					if (paused) {
						Thread.Sleep(PauseIdleTime);
					}
					try {
						bool moreWork = false;
						for (int i = 0; i < connections.Count; i++) {
							if (connections[i].CallbackThread != null && connections[i].CanStartNewThread) {
								//try {
									connections[i].CallbackThread = null;
									lock (activeThreadsLock) {
										activeThreads--;
									}
								//}
								//catch (Exception e) { }
							}

							if (connections[i].IsMarkedForRemoval) {
								if (connections[i].CanStartNewThread) {
									lock (connections) {
										connections[i].ForceDisconnect();
										if (connections[i].CallbackThread != null)
											activeThreads--;
										connections.RemoveAt(i);
										i--;
									}
								}
							}
							else if (connections[i].CallbackThread != null) {

							}
							else if (connections[i].IsConnected &&
									(connections[i].LastVerifyTime.AddMilliseconds(VerifyConnectionInterval) > DateTime.UtcNow ||
									connections[i].VerifyConnection())) {
								moreWork = moreWork || ProcessConnection(connections[i]);
							}
							else if (ClientConnectionLost != null) {
								if (/*activeThreads < MaxCallbackThreads && */connections[i].CanStartNewThread) {
									/*lock (activeThreadsLock) {
										activeThreads++;
									}*/
									ServerConnection connection = connections[i];
									//connections[i].CallbackThread = new Thread(() => {
									ClientConnectionLost?.Invoke(this, connection);
									//});
									//connections[i].CallbackThread.Start();
									connections[i].IsMarkedForRemoval = true;
									Thread.Yield();
								}
							}
							else {
								connections[i].IsMarkedForRemoval = true;
								lock (connections) {
									if (connections[i].CallbackThread != null)
										activeThreads--;
									connections[i].ForceDisconnect();
									connections.RemoveAt(i);
									i--;
								}
							}
						}

						if (!moreWork) {
							Thread.Yield();
							lock (sem) {
								foreach (ServerConnection connection in connections) {
									if (connection.HasMoreWork) {
										moreWork = true;
										break;
									}
								}
							}
							if (!moreWork) {
								waiting = true;
								sem.Wait(IdleTime);
								waiting = false;
							}
						}
					}
					catch (SocketException ex) {
						if (IsRunning) {
							Error?.Invoke(this, ex);
						}
					}
					Thread.Sleep(precision ? 1 : IdleTime);
				}
			}
			catch (ThreadAbortException) { }
			catch (ThreadInterruptedException) { }
		}

		private bool ProcessConnection(ServerConnection connection) {
			bool moreWork = false;
			if (connection.ProcessOutgoing(MaxSendAttempts)) {
				moreWork = true;
			}

			if (MessageReceived != null && activeThreads < MaxCallbackThreads && connection.TcpClient.Available > 0) {
				lock (activeThreadsLock) {
					activeThreads++;
				}
				connection.CallbackThread = new Thread(() => {
					NetworkStream stream = connection.TcpClient.GetStream();
					byte[] data = new byte[connection.TcpClient.Available];
					stream.Read(data, 0, data.Length);

					MessageReceived?.Invoke(this, connection, data, data.Length);
				});
				connection.CallbackThread.Start();
				Thread.Yield();
			}
			return moreWork;
		}

		private void ListenerThread() {
			try {
				while (IsRunning) {
					if (paused) {
						Thread.Sleep(PauseIdleTime);
						continue;
					}
					try {
						if (listener.Pending()) {
							TcpClient client = listener.AcceptTcpClient();
							ServerConnection connection = new ServerConnection(client);
							if (ClientConnected != null) {
								lock (activeThreadsLock) {
									activeThreads++;
								}
								connection.CallbackThread = new Thread(() => {
									ClientConnected?.Invoke(this, connection);
								});
								connection.CallbackThread.Start();
							}

							lock (connections) {
								connections.Add(connection);
							}
						}
						else {
							Thread.Sleep(IdleTime);
						}
					}
					catch (SocketException ex) {
						if (IsRunning) {
							Error?.Invoke(this, ex);
						}
					}
				}
			}
			catch (ThreadAbortException) { }
			catch (ThreadInterruptedException) { }
			/*catch (Exception ex) {
				if (IsRunning) {
					Error?.Invoke(this, e);
					Stop();
				}
			}*/
		}

		public void Send(Command command) {
			foreach (ServerConnection connection in connections) {
				if (connection.Username == command.Name && connection.IsMarkedForRemoval)
					return;
			}
			lock (sem) {
				foreach (ServerConnection connection in connections) {
					if (connection.IsLoggedIn && connection.Username != command.Name)
						connection.Send(command);
				}
				Thread.Yield();
				if (waiting) {
					sem.Release();
					waiting = false;
				}
			}
		}
		public void SendTo(Command command, string username) {
			foreach (ServerConnection connection in connections) {
				if (connection.Username == command.Name && connection.IsMarkedForRemoval)
					return;
			}
			lock (sem) {
				foreach (ServerConnection connection in connections) {
					if (username == connection.Username) {
						connection.Send(command);
						break;
					}
				}
				Thread.Yield();
				if (waiting) {
					sem.Release();
					waiting = false;
				}
			}
		}
		public void SendToConnection(Command command, ServerConnection connection) {
			if (connection.IsMarkedForRemoval)
				return;
			lock (sem) {
				connection.Send(command);
				Thread.Yield();
				if (waiting) {
					sem.Release();
					waiting = false;
				}
			}
		}
		public void SendToNow(Command command, string username) {
			lock (sem) {
				foreach (ServerConnection connection in connections) {
					if (username == connection.Username) {
						connection.SendNow(command, MaxSendAttempts);
						break;
					}
				}
				Thread.Yield();
				if (waiting) {
					sem.Release();
					waiting = false;
				}
			}
		}
		public void ClearAllMessages() {
			foreach (ServerConnection connection in connections) {
				connection.ClearMessages();
			}
		}
	}
}
