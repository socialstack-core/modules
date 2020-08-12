using System;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.ActiveLogins
{
	
	/// <summary>
	/// An ActiveLogin
	/// </summary>
	public partial class ActiveLogin : RevisionRow
	{
		/// <summary>
		/// ContentSync server ID.
		/// </summary>
		public int Server;
	}

}