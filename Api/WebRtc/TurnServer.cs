using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Api.WebRTC;


/// <summary>
/// A TURN server implementation
/// </summary>
public class TurnServer
{
	private int Port;
	private Socket ServerSocketUdp;
	private Socket ServerSocketTcp;

	private const int bufSize = 8 * 1024;
	private State state = new State();
	private EndPoint epFrom = new IPEndPoint(IPAddress.Any, 0);
	private AsyncCallback recv = null;

	/// <summary>
	/// 
	/// </summary>
	public class State
	{
		/// <summary>
		/// 
		/// </summary>
		public byte[] buffer = new byte[bufSize];
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="port"></param>
	public TurnServer(int port)
	{
		
		Port = port;

		ServerSocketUdp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

		ServerSocketUdp.Bind(
			new IPEndPoint(IPAddress.Any, Port)
		);

		ServerSocketUdp.Blocking = false;
		Receive();


		ServerSocketTcp = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

		ServerSocketTcp.Bind(
			new IPEndPoint(IPAddress.Any, Port)
		);

		ServerSocketTcp.Blocking = false;
		ServerSocketTcp.Listen(100);
		ServerSocketTcp.BeginAccept(OnSocketConnect, null);

	}

	private void OnSocketConnect(IAsyncResult ar)
	{
		if (ServerSocketTcp == null)
		{
			return;
		}

		// Get the socket:
		Socket socket = ServerSocketTcp.EndAccept(ar);

		// Continue accepting more connections:
		ServerSocketTcp.BeginAccept(OnSocketConnect, null);

		// Non-blocking socket:
		socket.Blocking = false;

		Console.WriteLine("TCP connected");
	}
	
	private void Receive()
	{
		ServerSocketUdp.BeginReceiveFrom(state.buffer, 0, bufSize, SocketFlags.None, ref epFrom, recv = (ar) =>
		{
			State so = (State)ar.AsyncState;
			int bytes = ServerSocketUdp.EndReceiveFrom(ar, ref epFrom);
			ServerSocketUdp.BeginReceiveFrom(so.buffer, 0, bufSize, SocketFlags.None, ref epFrom, recv, so);
			Console.WriteLine("RECV: {0}: {1}", epFrom.ToString(), bytes);
		}, state);
	}

}