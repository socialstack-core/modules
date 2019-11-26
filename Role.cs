using System;
using System.Text;
using Api.Contexts;


namespace Api.Permissions
{
    /// <summary>
    /// A role which defines a set of capabilities to a user who is granted this particular role.
    /// </summary>
    public partial class Role
    {
		/// <summary>
		/// The role ID, as used by user rows.
		/// </summary>
        public int Id;
		/// <summary>
		/// The nice name of the role, usually in the site default language.
		/// </summary>
        public string Name;
		/// <summary>
		///  The role key - usually the lowercase, underscores instead of spaces variant of the first set name.
		///  This shouldn't change after it has been set.
		/// </summary>
        public string Key;


		/// <summary>
		/// Create a new role with the given ID.
		/// </summary>
		/// <param name="id"></param>
        public Role(int id)
        {
            Id = id;
            Add();
        }

		/// <summary>
		/// Indexed by capability InternalId. Never null.
		/// </summary>
		private FilterNode[] CapabilityLookup = new FilterNode[0];

		/// <summary>
		/// Grants the given capabilities unconditionally.
		/// </summary>
		/// <param name="capabilityNames"></param>
		/// <returns></returns>
		public Role Grant(params string[] capabilityNames)
		{
			for (var i = 0; i < capabilityNames.Length; i++)
			{
				Grant(capabilityNames[i], new FilterTrue());
			}

			return this;
		}
		
		/// <summary>
		/// Grants the given single capability conditionally. The chain must resolve to true to grant the capability.
		/// Note that this is used via a .If() chain.
		/// </summary>
		/// <param name="capabilityName"></param>
		/// <param name="node"></param>
		/// <returns></returns>
		public Role Grant(string capabilityName, FilterNode node)
		{
			Capability cap;
			if (!Capabilities.All.TryGetValue(capabilityName.ToLower(), out cap))
			{
				// If you ended up here, please check to that you're instancing a capability with the given name 
				// (it's probably a typo in your grant call).
				throw new Exception("Role '" + Name + "' tried to grant '" + capabilityName + "' but that capability wasn't found.");
			}
			
			// Resize lookup if we need to:
			if (cap.InternalId >= CapabilityLookup.Length)
			{
				// Resize it:
				var newLookup = new FilterNode[cap.InternalId + 1];
				Array.Copy(CapabilityLookup, newLookup, CapabilityLookup.Length);
				CapabilityLookup = newLookup;
			}

			if (CapabilityLookup[cap.InternalId] != null)
			{
				// Can't replace grants here. Revoke first to avoid, but the dev probably meant to use an Or()/ And() call.
				throw new Exception(
					"Attempted to grant '" + capabilityName + "' on role '" + Name + "' again. " +
					"If you did want to replace the existing grant, use Revoke first to avoid this message. " +
					"Otherwise, if you wanted to merge them, use Or() or And() calls instead."
				);
			}

			// Add to our fast internal lookup (as 'always granted' here):
			// NOTE: We're intentionally generating new objects for each one.
			// The capabilities must be completely independent at all times to avoid any unexpected crossover.
			CapabilityLookup[cap.InternalId] = node;
			
            return this;
        }

		/// <summary>
		/// Builds this filter node as a query string, writing it into the given string builder.
		/// </summary>
		/// <param name="capability"></param>
		/// <param name="builder"></param>
		/*
		public void BuildQuery(Capability capability, StringBuilder builder, int )
		{
			var filter = CapabilityLookup[capability.InternalId];
			filter.BuildQuery(builder, 0);
		}
		*/

		/// <summary>
		/// Start conditional grants. For example, theRole.If().IsSelf().ThenGrant("UserEdit") - 
		/// this means if the current user is the user being edited then the permission is granted.
		/// </summary>
		/// <returns></returns>
		public Filter If()
		{
			return new Filter() { Role = this };
		}
		
		/// <summary>
		/// Revokes the named capabilities. Often used when merging or copying from roles.
		/// </summary>
		/// <param name="capabilityNames"></param>
		/// <returns></returns>
        public Role Revoke(params string[] capabilityNames)
        {
            for (var i = 0; i < capabilityNames.Length; i++)
            {
                var capabilityName = capabilityNames[i];

                Capability cap;
                if (!Capabilities.All.TryGetValue(capabilityName.ToLower(), out cap))
                {
                    // If you ended up here, please check to that you're instancing a capability with the given name 
                    // (it's probably a typo in your revoke call).
                    throw new Exception("Role '" + Name + "' tried to revoke '" + capabilityName + "' but that capability wasn't found.");
                }

                // Revoke at that index:
                if (cap.InternalId >= CapabilityLookup.Length)
                {
                    // Out of range - already not granted.
                    continue;
                }

                // Remove from lookup:
                CapabilityLookup[cap.InternalId] = null;
            }

            return this;
        }

        /// <summary>
        /// Grants every capability. You can revoke certain ones afterwards.
        /// </summary>
        /// <returns></returns>
        public Role GrantEverything()
        {
            foreach (var kvp in Capabilities.All)
            {
                Grant(kvp.Key);
            }
            return this;
        }

        /// <summary>
        /// Grants the same perms as the given role. 
        /// Replaces any grants already done, but you can still call grant after this.
        /// </summary>
        /// <param name="copyFrom"></param>
        /// <returns></returns>
        public Role GrantTheSameAs(Role copyFrom)
        {
            // Must clone each grant chain individually just in case someone uses the chain API directly.
            CapabilityLookup = new FilterNode[copyFrom.CapabilityLookup.Length];

			for (var i = 0; i < copyFrom.CapabilityLookup.Length; i++)
			{
				var node = CapabilityLookup[i];

				if (node == null)
				{
					continue;
				}

				CapabilityLookup[i] = node.Copy();
			}

            return this;
        }
		
        /// <summary>
        /// Add this role to the lookup.
        /// </summary>
        /// <returns></returns>
        private void Add()
        {
            if (Roles.All == null || Roles.All.Length <= Id) {
                // Resize it:
                var all = new Role[Id + 1];

                if (Roles.All != null)
                {
                    Array.Copy(Roles.All, all, Roles.All.Length);
                }

                Roles.All = all;
            }

            Roles.All[Id] = this;
        }

		/// <summary>
		/// Is the given capability granted to this role? Don't use this directly - use ACapability.IsGranted instead.
		/// </summary>
		/// <param name="capability">The capability to check for. This is required.</param>
		/// <param name="token">The requesting user account.</param>
		/// <param name="extraObjectsToCheck">
		/// Any additional args used by the capability to check access rights.
		/// For example - can a user access forum post x? Check ForumPostLoad perm and give it x.
		/// </param>
		/// <returns></returns>
		public bool IsGranted(Capability capability, Context token, params object[] extraObjectsToCheck)
        {
            if (capability.InternalId >= CapabilityLookup.Length)
            {
                // Nope!
                return false;
            }

            var handler = CapabilityLookup[capability.InternalId];

            if (handler == null)
            {
                return false;
            }

            // Ask the handler:
            return handler.IsGranted(capability, token, extraObjectsToCheck);
        }

		/// <summary>
		/// Is the given capability granted to this role? Don't use this directly - use ACapability.IsGranted instead.
		/// </summary>
		/// <param name="capability">The capability to check for. This is required.</param>
		/// <param name="token">The requesting user account.</param>
		/// <param name="extraObjectsToCheck">
		/// Any additional args used by the capability to check access rights.
		/// For example - can a user access forum post x? Check ForumPostLoad perm and give it x.
		/// </param>
		/// <param name="filter">Used to role restrict searches.</param>
		/// <returns></returns>
		public bool IsGranted(Capability capability, Context token, Filter filter, params object[] extraObjectsToCheck)
		{
			if (capability.InternalId >= CapabilityLookup.Length)
			{
				// Nope!
				return false;
			}

			var handler = CapabilityLookup[capability.InternalId];

			if (handler == null)
			{
				// Definitely not.
				return false;
			}

			// This role does have some kind of grant, so we're good to go ahead and add to the filter.
			// We don't use IsGranted in this scenario though - instead, we'll copy the rules *into the filter* with an AND.
			filter.AndAppendConstructed(handler);

			return true;
		}
		
	}
	
	/// <summary>
	/// Used to define a method which returns true/ false depending on if a capability should be granted.
	/// </summary>
	/// <param name="capability"></param>
	/// <param name="token"></param>
	/// <param name="extraObjectsToCheck"></param>
	/// <returns></returns>
	public delegate bool IsGrantedHandler(Capability capability, Context token, object[] extraObjectsToCheck);
}