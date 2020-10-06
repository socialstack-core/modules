using System;
using Api.Database;
using Api.Translate;
using Api.Users;
using Api.WebSockets;


namespace Api.Presence
{
	
	/// <summary>
	/// A PagePresenceRecord
	/// </summary>
	public partial class PagePresenceRecord : UserCreatedRow<ulong>, IAmLive
	{
		/// <summary>
		/// Current page this user is on. Because of url tokens, these aren't as unique as URL is.
		/// </summary>
		public int PageId;		
		/// <summary>
		/// Current page url this user is on. More unique than page ID.
		/// </summary>
		[DatabaseField(Length=100)]
		public string Url;
		/// <summary>
		/// ContentSync server ID.
		/// </summary>
		public uint ServerId;
		/// <summary>
		/// Websocket connection ID. Server+this is the row ID.
		/// </summary>
		public uint WebSocketId;
	}

}