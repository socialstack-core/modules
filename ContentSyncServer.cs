using System;
using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using Microsoft.Extensions.Configuration;
using Api.Configuration;
using Api.StackTools;
using System.Diagnostics;
using Api.SocketServerLibrary;
using Api.Startup;
using Api.Users;
using System.Linq;
using System.IO;


namespace Api.ContentSync
{
	/// <summary>
	/// Another ContentSync server in the cluster connected to "us".
	/// </summary>
	public class ContentSyncServer : Client
	{
		/// <summary>
		/// Remote server Id.
		/// </summary>
		public int ServerId;
		/// <summary>
		/// Content type -> message writer.
		/// </summary>
		public Dictionary<Type, OpCodeMessageWriter> OpCodeMap = new Dictionary<Type, OpCodeMessageWriter>();

		/// <summary>
		/// Called on disconnect
		/// </summary>
		public override void Close()
		{
			base.Close();

			// Clear opcodeMap set and the entries in the fast lookups:
			var map = OpCodeMap;
			OpCodeMap = null;

			if (map == null)
			{
				return;
			}

			Console.WriteLine("[CSync] A server disconnected. Bye!");

			// Tell the sync service to remove this:
			Services.Get<IContentSyncService>().RemoveServer(this);
		}
	}

}