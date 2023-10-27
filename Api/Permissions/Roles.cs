using Api.Contexts;
using System;

namespace Api.Permissions
{
    /// <summary>
    /// Available roles.
    /// </summary>
    public static partial class Roles
    {
		/// <summary>
		/// The developer role. Can do everything.
		/// </summary>
		public static Role Developer;
		/// <summary>
		/// The main administrative role. 
		/// Can do most things in the admin panel except e.g. change site configuration.
		/// </summary>
		public static Role Admin;
		/// <summary>
		/// A role used when a user account has been created but not yet activated.
		/// </summary>
		public static Role Guest;
		/// <summary>
		/// The default role used when a user is created.
		/// </summary>
		public static Role Member;
		/// <summary>
		/// The role used by users that have been marked as banned.
		/// </summary>
		public static Role Banned;
		/// <summary>
		/// The role used by users who aren't logged in.
		/// </summary>
		public static Role Public;

		/// <summary>
		/// The developer role. Can do everything.
		/// </summary>
		[Obsolete("Use Roles.Developer instead.")]
		public static Role SuperAdmin
		{
			get
			{
				return Developer;
			}
		}

    }
}
