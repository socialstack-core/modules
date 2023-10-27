using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Timers;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace Api.SocketServerLibrary {

	/// <summary>
	/// A realtime zero allocation socket server.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class Server<T> : Server where T: Client, new()
	{
		/// <summary>
		/// Called when a client connected.
		/// </summary>
		public Func<T, ValueTask> OnConnected;

		/// <summary>
		/// Called when a client connected.
		/// </summary>
		protected virtual void Connected(T client)
		{
		}

		/// <summary>
		/// Current latest locally assigned client ID.
		/// </summary>
		private uint Id = 1;

		/// <summary>
		/// Called when the given socket connected.
		/// </summary>
		/// <param name="socket"></param>
		protected override void SocketConnected(Socket socket)
		{
			var id = Id;
			Id++;

			T c = new T()
			{
				Id = id,
				Server = this,
				Socket = socket,
				CanProcessSend = true
			};

			// Start listening for data:
			c.Start();

			Connected(c);

			if (OnConnected != null)
			{
				_ = OnConnected(c);
			}
		}


		/// <summary>
		/// Explicitly connect to a remote host (another server of this type).
		/// </summary>
		public T ConnectTo(string host, int port, uint serverId, Action<T> onSetupRemote)
		{
			if (IPAddress.TryParse(host, out IPAddress addr))
			{
				return ConnectTo(addr, port, serverId, onSetupRemote);
			}

			var hostEntry = Dns.GetHostEntry(host);

			if (hostEntry.AddressList.Length == 0)
			{
				throw new Exception("DNS records for '" + host + "' not found");
			}

			// Connect now:
			return ConnectTo(hostEntry.AddressList[0], port, serverId, onSetupRemote);
		}

		/// <summary>
		/// A response to an explicit connect to.
		/// </summary>
		private void OnConnectResult(IAsyncResult result)
		{
			var remote = result.AsyncState as Client;

			if (!remote.Socket.Connected)
			{
				return;
			}

			// Doesn't require hello when we're connecting to it:
			remote.Hello = false;

			remote.Socket.EndConnect(result);

			// Start listening for data:
			remote.Start();
		}

		/// <summary>
		/// Explicitly connect to a remote host (another server of this type).
		/// </summary>
		public T ConnectTo(IPAddress targetIp, int port, uint serverId, Action<T> onSetupRemote)
		{
			var remote = new T()
			{
				Server = this,
				CanProcessSend = false
			};

			onSetupRemote?.Invoke(remote);

			var socket = new Socket(targetIp.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			remote.Socket = socket;
			socket.Blocking = false;
			socket.BeginConnect(targetIp, port, OnConnectResult, remote);

			var timer = new Timer();
			timer.Elapsed += (object source, ElapsedEventArgs e) => {

				if (!socket.Connected)
				{
                    // Timeout.
                    Log.Warn("socketserverlibrary", "Unable to contact remote server #" + serverId);
					socket.Close();
				}

				timer.Stop();
				timer.Dispose();
			};

			timer.Interval = 1000;
			timer.Enabled = true;

			return remote;
		}

	}

	/// <summary>
	/// Base server class. Use the generic form instead.
	/// </summary>
	public partial class Server
	{

		/// <summary>
		/// The port this server listens on.
		/// </summary>
		public int Port;

		/// <summary>
		/// True if this server is going down.
		/// </summary>
		public bool GoingDown;
		
		/// <summary>
		/// If this is set, the socket will also listen on a Unix socket file with the given name.
		/// </summary>
		public string UnixSocketFileName;
		
		/// <summary>
		/// The server socket.
		/// </summary>
		protected Socket ServerSocket;

		/// <summary>
		/// The bind address.
		/// </summary>
		public IPAddress BindAddress = IPAddress.Any;

		/// <summary>
		/// The map of opcodes that this server handles.
		/// </summary>
		public Dictionary<uint, OpCode> OpCodeMap = new Dictionary<uint, OpCode>();

		/// <summary>
		/// The max opcode registered.
		/// </summary>
		protected uint MaxOpCode;

		/// <summary>
		/// A more compact version of the opcode map. Instanced when Start is called, if you have a less than 5000 max opcode, after you've registered your opcodes.
		/// </summary>
		public OpCode[] FastOpCodeMap;

		/// <summary>
		/// Called when this server starts.
		/// </summary>
		public event Action OnStart;

		/// <summary>
		/// Called when this server stops.
		/// </summary>
		public event Action OnStopped;

		/// <summary>
		/// Adds a handler for an opcode - it just recognises the opcode, and then effectively does nothing.
		/// </summary>
		/// <param name="opcode"></param>
		public OpCode RegisterOpCode(uint opcode)
		{
			var instance = new OpCode();
			instance.Code = opcode;
			instance.HasSomethingToDo = false;
			AddToOpcodeMap(opcode, instance);
			return instance;
		}

		/// <summary>
		/// Adds a handler for an opcode - it just recognises the opcode, and then effectively does nothing.
		/// </summary>
		/// <param name="opcode"></param>
		/// <param name="onRequest"></param>
		public OpCode RegisterOpCode(uint opcode, Action<Client, Writer> onRequest)
		{
			var instance = new CompleteMessageOpCode();
			instance.Code = opcode;
			instance.OnRequest = onRequest;
			instance.MessageReader = new CompleteMessageReader(instance);
			AddToOpcodeMap(opcode, instance);
			return instance;
		}

		/// <summary>
		/// Add an opcode handler.
		/// </summary>
		/// <param name="opcode"></param>
		/// <param name="onRequest"></param>
		/// <param name="reader"></param>
		public OpCode<U> RegisterOpCode<U>(uint opcode, Func<Client, U, ValueTask> onRequest, MessageReader reader = null) where U : Message<U>, new()
		{
			// Get the concrete opcode type:
			var concreteType = typeof(OpCode<>).MakeGenericType(new Type[] { typeof(U) });

			// Instance the message specific opcode class:
			var instance = (OpCode<U>)Activator.CreateInstance(concreteType);

			// Set the request delegate:
			instance.OnRequestAsync = onRequest;

			instance.Async = true;

			instance.Code = opcode;

			if (reader == null)
			{
				// Get the reader:
				var boltMsg = BoltReaderWriter.Get<U>();

				instance.MessageReader = new GenericMessageReader<U>(boltMsg, instance);
			}
			else
			{
				instance.MessageReader = reader;
			}
			
			AddToOpcodeMap(opcode, instance);

			return instance;
		}

		/// <summary>
		/// Add an opcode handler.
		/// </summary>
		/// <param name="opcode"></param>
		/// <param name="onRequest"></param>
		/// <param name="reader"></param>
		public OpCode<U> RegisterOpCode<U>(uint opcode, Action<Client, U> onRequest, MessageReader reader = null) where U : Message<U>, new()
		{
			// Get the concrete opcode type:
			var concreteType = typeof(OpCode<>).MakeGenericType(new Type[] { typeof(U) });

			// Instance the message specific opcode class:
			var instance = (OpCode<U>)Activator.CreateInstance(concreteType);

			// Set the request delegate:
			instance.OnRequest = onRequest;

			instance.Code = opcode;

			if (reader == null)
			{
				// Get the reader:
				var boltMsg = BoltReaderWriter.Get<U>();

				instance.MessageReader = new GenericMessageReader<U>(boltMsg, instance);
			}
			else
			{
				instance.MessageReader = reader;
			}

			AddToOpcodeMap(opcode, instance);

			return instance;
		}

		/// <summary>
		/// Adds the given opcode instance to the opcode map
		/// </summary>
		/// <param name="opcode"></param>
		/// <param name="instance"></param>
		private void AddToOpcodeMap(uint opcode, OpCode instance)
		{
			if (opcode >= MaxOpCode)
			{
				MaxOpCode = opcode;
			}

			OpCodeMap[opcode] = instance;

			if (FastOpCodeMap != null)
			{
				if (MaxOpCode > 5000)
				{
					FastOpCodeMap = null;
				}
				else if (opcode >= FastOpCodeMap.Length)
				{
					// resize it:
					var map = new OpCode[MaxOpCode + 1];

					foreach (var kvp in OpCodeMap)
					{
						map[kvp.Key] = kvp.Value;
					}

					FastOpCodeMap = map;
				}
				else
				{
					FastOpCodeMap[opcode] = instance;
				}
			}
		}

		private void OnSocketConnect(IAsyncResult ar)
		{
			if (ServerSocket == null)
			{
				return;
			}

			// Get the socket:
			Socket socket = ServerSocket.EndAccept(ar);

			// Continue accepting more connections:
			ServerSocket.BeginAccept(OnSocketConnect, null);

			// Non-blocking socket:
			socket.Blocking = false;

			SocketConnected(socket);
		}

		/// <summary>
		/// Called when the given socket has connected.
		/// </summary>
		/// <param name="socket"></param>
		protected virtual void SocketConnected(Socket socket)
		{
			
		}

		/// <summary>
		/// Start the server.
		/// </summary>
		public void Start()
		{
			if (ServerSocket != null)
			{
				throw new Exception("Server already started.");
			}

			if (MaxOpCode <= 5000)
			{
				FastOpCodeMap = new OpCode[MaxOpCode + 1];

				foreach (var kvp in OpCodeMap)
				{
					FastOpCodeMap[kvp.Key] = kvp.Value;
				}
			}
			else
			{
				FastOpCodeMap = null;
			}
			
			string apiSocketFile = null;
			
			if(UnixSocketFileName != null && RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				// Listen on a Unix socket too:
				apiSocketFile = System.IO.Path.GetFullPath(UnixSocketFileName);
				
				try{
					// Delete if exists:
					System.IO.File.Delete(apiSocketFile);
				}catch{}
				
				ServerSocket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
				
				ServerSocket.Bind(
					new UnixDomainSocketEndPoint(apiSocketFile)
				);

			}
			else
			{
				ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				
				ServerSocket.Bind(
					new IPEndPoint(BindAddress, Port)
				);
			}

			ServerSocket.Blocking = false;
			ServerSocket.Listen(100);
			ServerSocket.BeginAccept(OnSocketConnect, null);
			Started();

			if (apiSocketFile != null)
			{
				Startup.Chmod.Set(apiSocketFile); // 777
			}

			OnStart?.Invoke();
		}
		
		/// <summary>
		/// Called when this server has started up.
		/// </summary>
		protected virtual void Started()
		{
		}

		/// <summary>
		/// Shutdown this server.
		/// </summary>
		public void Stop()
		{
			GoingDown = true;
			if (ServerSocket != null)
			{
				ServerSocket.Close();
				ServerSocket = null;
			}
			Stopped();
			OnStopped?.Invoke();
		}

		/// <summary>
		/// Called when this server stopped.
		/// </summary>
		protected virtual void Stopped()
		{
		}
		
	}
}