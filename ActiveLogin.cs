using System;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.ActiveLogins
{
	
	/// <summary>
	/// An ActiveLogin
	/// </summary>
	public partial class ActiveLogin : VersionedContent<int>
	{
		/// <summary>
		/// ContentSync server ID.
		/// </summary>
		public int Server;
	}

}