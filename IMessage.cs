namespace Api.SocketServerLibrary{
	
	/// <summary>
	/// A message being handled by a socket server.
	/// </summary>
	public interface IMessage{
		
		/// <summary>
		/// The client that this message is passing through.
		/// </summary>
		public Client Client { get; set; }

		/// <summary>
		/// True if this message object is currently pooled.
		/// </summary>
		public bool Pooled { get; set; }
		
		/// <summary>
		/// The ID of this request.
		/// </summary>
		public int RequestId { get; set; }
		
		/// <summary>
		/// The opcode that is receiving this message.
		/// </summary>
		public OpCode OpCode { get; set; }

		/// <summary>
		/// The user ID in this current message context.
		/// Note that this is not stored in the client because server->server bridges are user stateless.
		/// </summary>
		public uint UserId { get; set; }
	}
	
}