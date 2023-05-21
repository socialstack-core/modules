using System;
using Api.SocketServerLibrary;
using Api.Startup;
using Api.Signatures;

namespace Api.ContentSync
{
	/// <summary>
	/// Another ContentSync server in the cluster connected to "us".
	/// </summary>
	public class ContentSyncServer : Client
	{
		/// <summary>
		/// Called on disconnect
		/// </summary>
		public override void Close()
		{
			base.Close();

			Log.Info("contentsyncservice", "A server disconnected. Bye!");

			// Tell the sync service to remove this:
			Services.Get<ContentSyncService>().RemoveServer(this);
		}

		/// <summary>
		/// Called just after connecting to a remote server.
		/// </summary>
		public override void Start()
		{
			base.Start();

			if (!Hello)
			{
				// It's up to us to send the hello:
				var syncService = Services.Get<ContentSyncService>();

				// Send the hello.
				// Sign our ID + their ID:
				var signature = Services.Get<SignatureService>().Sign(syncService.ServerId + "=>" + Id);

				var msg = SyncServerHandshake.Get();
				msg.ServerId = syncService.ServerId;
				msg.Signature = signature;
				var writer = msg.Write(3);
				Send(writer);
				writer.Release();
				msg.Release();
			}
		}
	}

}