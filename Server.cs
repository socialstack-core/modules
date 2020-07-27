using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Reflection.Emit;


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
		public event Action<T> OnConnected;

		/// <summary>
		/// Called when a client connected.
		/// </summary>
		protected virtual void Connected(T client)
		{
		}

		/// <summary>
		/// Called when the given socket connected.
		/// </summary>
		/// <param name="socket"></param>
		protected override void SocketConnected(Socket socket)
		{
			T c = new T()
			{
				Server = this,
				Socket = socket,
				CanProcessSend = true
			};

			// Start listening for data:
			c.Start();

			Connected(c);
			OnConnected?.Invoke(c);
		}

	}

	/// <summary>
	/// Base server class. Use the generic form instead.
	/// </summary>
	public class Server
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
		/// Add an opcode handler.
		/// </summary>
		/// <param name="opcode"></param>
		/// <param name="onRequest"></param>
		public OpCode<U> RegisterOpCode<U>(uint opcode, MessageDelegate<U> onRequest) where U:IMessage, new()
		{
			if (opcode >= MaxOpCode)
			{
				MaxOpCode = opcode;
			}

			// Get the concrete opcode type:
			var concreteType = typeof(OpCode<>).MakeGenericType(new Type[] { typeof(U) });

			// Instance the message specific opcode class:
			var instance = (OpCode<U>)Activator.CreateInstance(concreteType);

			// Set the request delegate:
			instance.OnRequest = onRequest;

			OpCodeMap[opcode] = instance;

			return instance;
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

			ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

			ServerSocket.Bind(
				new IPEndPoint(BindAddress, Port)
			);

			ServerSocket.Blocking = false;
			ServerSocket.Listen(100);
			ServerSocket.BeginAccept(OnSocketConnect, null);
			Started();

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