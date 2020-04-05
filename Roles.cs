using Api.Contexts;


namespace Api.Permissions
{
    /// <summary>
    /// Available roles.
    /// </summary>
    public static partial class Roles
    {
        /// <summary>
        /// All roles, indexed by role ID. Never null but always bounds check.
        /// </summary>
        public static Role[] All = new Role[0];

		/// <summary>
		/// The role used by users who aren't logged in.
		/// </summary>
		public static Role Public;
		/// <summary>
		/// The super admin role. Can do everything.
		/// </summary>
		public static Role SuperAdmin;
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
		/// Role for banned people.
		/// </summary>
		public static Role Banned;


		/// <summary>
		/// Gets a role by its ID.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public static Role Get(int id)
		{
			if (id < 0 || id >= All.Length)
			{
				return null;
			}

			return All[id];
		}
    }
}
