using Api.Contexts;
using Api.Permissions;
using Api.SocketServerLibrary;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.ContentSync
{
	/// <summary>
	/// Handles contentSync.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IContentSyncService
    {

		/// <summary>
		/// Handshake opcode
		/// </summary>
		OpCode<SyncServerHandshake> HandshakeOpCode { get; set; }
		
		/// <summary>
		/// This server's ID from the ContentSync config.
		/// </summary>
		int ServerId {get; set;}

		/// <summary>
		/// Informs CSync to start syncing the given type as the given opcode.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="opcode"></param>
		void SyncRemoteType(Type type, int opcode);

		/// <summary>
		/// Removes the given server from the lookups.
		/// </summary>
		/// <param name="server"></param>
		void RemoveServer(ContentSyncServer server);

	}
}
