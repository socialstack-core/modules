using System;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.ActiveLogins
{
	
	/// <summary>
	/// An ActiveLogin historical record.
	/// </summary>
	public partial class ActiveLoginHistory : DatabaseRow
	{
		/// <summary>
		/// The user logging in/ out.
		/// </summary>
		public int UserId;
		
		/// <summary>
		/// True if this is a login, false for logout.
		/// </summary>
		public bool IsLogin;
		
		/// <summary>
		/// Date/time this record was created.
		/// </summary>
		public DateTime CreatedUtc;
	}

}