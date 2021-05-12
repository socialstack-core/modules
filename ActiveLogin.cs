using System;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.ActiveLogins
{
	
	/// <summary>
	/// An ActiveLogin
	/// </summary>
	public partial class ActiveLogin : VersionedContent<uint>
	{
		/// <summary>
		/// ContentSync server ID.
		/// </summary>
		public uint Server;

		/// <summary>
		/// 0 = Offline, 1 = Online, 2 = Away (away unused at the moment).
		/// </summary>
		public int? OnlineState;
	}

}