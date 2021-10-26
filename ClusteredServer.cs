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
	/// A server in the cluster. Each one owns one stripe, and they adjust (globally) when a new server joins the cluster.
	/// </summary>
	public class ClusteredServer : Content<uint>
	{
		/// <summary>
		/// The port number used for contentsync on this server.
		/// </summary>
		public int Port;

		/// <summary>
		/// The environment this server is in.
		/// </summary>
		[DatabaseField(Length = 20)]
		public string Environment;

		/// <summary>
		/// Private IPv4 address for this server.
		/// </summary>
		[DatabaseField(Length=4)]
		public byte[] PrivateIPv4;
		
		/// <summary>
		/// Public IPv4 address for this server.
		/// </summary>
		[DatabaseField(Length=4)]
		public byte[] PublicIPv4;
		
		/// <summary>
		/// Private IPv6 address for this server. Often matches the public one.
		/// </summary>
		[DatabaseField(Length=16)]
		public byte[] PrivateIPv6;
		
		/// <summary>
		/// Public IPv6 address for this server.
		/// </summary>
		[DatabaseField(Length=16)]
		public byte[] PublicIPv6;
		
		/// <summary>
		/// Hostname of this server. Used to identify if a server has been seen before.
		/// </summary>
		[DatabaseField(Length=80)]
		public string HostName;
		
		/// <summray>
		/// Socialstack server type. Often 0 if undeclared.
		/// </summary>
		public uint ServerTypeId;
		
		/// <summray>
		/// Socialstack host platform ID. Often 0 if undeclared.
		/// </summary>
		public uint HostPlatformId;
		
		/// <summray>
		/// Socialstack region ID for a given host platform.
		/// </summary>
		public uint RegionId;

	}

}