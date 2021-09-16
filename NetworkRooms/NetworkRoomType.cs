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
using Api.Signatures;

namespace Api.ContentSync
{
	/// <summary>
	/// Network room type.
	/// </summary>
	public class NetworkRoomType : Content<uint>
	{
		
		/// <summary>The unique name of the network room. Always lowercased.</summary>
		public string TypeName;
		
	}

}