using System;
using System.Text;
using System.Threading.Tasks;
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
		/// True if this role can view the admin panel.
		/// </summary>
		public bool CanViewAdmin;

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
		/// Indexed by capability InternalId.
		/// </summary>
		private FilterNode[] CapabilityLookup = new FilterNode[0];

		/// <summary>
		/// Indexed by capability InternalId.
		/// </summary>
		private Filter[] CapabilityFilterLookup = new Filter[0];

		/// <summary>
		/// Grants the given capabilities unconditionally.
		/// </summary>
		/// <param name="capabilityNames"></param>
		/// <returns></returns>
		public Role Grant(params string[] capabilityNames)
		{
			for (var i = 0; i < capabilityNames.Length; i++)
			{
				Grant(capabilityNames[i], new FilterTrue(), null);
			}

			return this;
		}

		/// <summary>
		/// Grants the given verbs unconditionally. Any capability that ends with this verb will be granted.
		/// For example, GrantVerb("Load") will permit UserLoad, ForumReplyLoad etc.
		/// </summary>
		/// <param name="verbs"></param>
		/// <returns></returns>
		public Role GrantVerb(params string[] verbs)
		{
			return GrantVerb(null, null, verbs);
		}

		/// <summary>
		/// Grants the given verbs unconditionally. Any capability that ends with this verb will be granted.
		/// For example, GrantVerb("Load") will permit UserLoad, ForumReplyLoad etc.
		/// </summary>
		/// <param name="node"></param>
		/// <param name="srcFilter"></param>
		/// <param name="verbs"></param>
		/// <returns></returns>
		public Role GrantVerb(FilterNode node, Filter srcFilter, params string[] verbs)
		{
			for (var i = 0; i < verbs.Length; i++)
			{
				verbs[i] = "_" + verbs[i].ToLower();
			}
			
			foreach (var kvp in Capabilities.All)
			{
				for (var i = 0; i < verbs.Length; i++)
				{
					if (kvp.Key.EndsWith(verbs[i]))
					{
						if (node == null)
						{
							Grant(kvp.Value, new FilterTrue(), null);
						}
						else
						{
							Grant(kvp.Value, node, srcFilter);
						}
						break;
					}
				}
			}
			
			return this;
		}

		/// <summary>
		/// Grants the given single capability conditionally. The chain must resolve to true to grant the capability.
		/// Note that this is used via a .If() chain.
		/// </summary>
		/// <param name="capabilityName"></param>
		/// <param name="node"></param>
		/// <param name="srcFilter"></param>
		/// <returns></returns>
		public Role Grant(string capabilityName, FilterNode node, Filter srcFilter)
		{
			Capability cap;
			if (!Capabilities.All.TryGetValue(capabilityName.ToLower(), out cap))
			{
				// If you ended up here, please check to that you're instancing a capability with the given name 
				// (it's probably a typo in your grant call).
				throw new Exception("Role '" + Name + "' tried to grant '" + capabilityName + "' but that capability wasn't found.");
			}

			return Grant(cap, node, srcFilter);
		}

		/// <summary>
		/// Grants the given single capability conditionally. The chain must resolve to true to grant the capability.
		/// Note that this is used via a .If() chain.
		/// </summary>
		/// <param name="cap"></param>
		/// <param name="node"></param>
		/// <param name="srcFilter"></param>
		public Role Grant(Capability cap, FilterNode node, Filter srcFilter)
		{
			// Resize lookup if we need to:
			if (cap.InternalId >= CapabilityLookup.Length)
			{
				// Resize it:
				var newLookup = new FilterNode[cap.InternalId + 1];
				Array.Copy(CapabilityLookup, newLookup, CapabilityLookup.Length);
				CapabilityLookup = newLookup;

				var newFilterLookup = new Filter[cap.InternalId + 1];
				Array.Copy(CapabilityFilterLookup, newFilterLookup, CapabilityFilterLookup.Length);
				CapabilityFilterLookup = newFilterLookup;
			}

			if (CapabilityLookup[cap.InternalId] != null)
			{
				// Can't replace grants here. Revoke first to avoid, but the dev probably meant to use an Or()/ And() call.
				throw new Exception(
					"Attempted to grant '" + cap.Name + "' on role '" + Name + "' again. " +
					"If you did want to replace the existing grant, use Revoke first to avoid this message. " +
					"Otherwise, if you wanted to merge them, use Or() or And() calls instead."
				);
			}

			// Add to our fast internal lookup (as 'always granted' here):
			// NOTE: We're intentionally generating new objects for each one.
			// The capabilities must be completely independent at all times to avoid any unexpected crossover.
			CapabilityLookup[cap.InternalId] = node;
			CapabilityFilterLookup[cap.InternalId] = srcFilter;
			
            return this;
        }

		/// <summary>
		/// Extends a cap filter with a role restriction. Note that this happens after e.g. GrantTheSameAs calls, because it's always role specific.
		/// </summary>
		/// <param name="cap"></param>
		/// <param name="contentType"></param>
		/// <param name="fieldName"></param>
		public void AddRoleRestrictionToFilter(Capability cap, Type contentType, string fieldName)
		{
			var filter = CapabilityFilterLookup[cap.InternalId];
			if (filter == null)
			{
				filter = new Filter(contentType) {
					Role = this
				};
				CapabilityFilterLookup[cap.InternalId] = filter;
			}

			if (filter.HasContent)
			{
				filter.And();
			}

			filter.Equals(contentType, fieldName, 1);
			CapabilityLookup[cap.InternalId] = filter.Construct(true);
		}

		/// <summary>
		/// Start conditional grants. For example, theRole.If((Filter f) => f.IsSelf()).ThenGrant("user_update") - 
		/// this means if the current user is the user being edited then the permission is granted.
		/// </summary>
		/// <returns></returns>
		public RoleIfResolver If(Func<Filter, Filter> resolver)
		{
			return new RoleIfResolver() { Role = this, FilterResolver = resolver };
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
                CapabilityFilterLookup[cap.InternalId] = null;
            }

            return this;
        }

		/// <summary>
		/// Revokes every capability. You can grant certain ones afterwards.
		/// </summary>
		/// <returns></returns>
		public Role RevokeEverything()
		{
			if (CapabilityLookup == null)
			{
				return this;
			}

			for (var i = 0; i < CapabilityLookup.Length; i++)
			{
				CapabilityLookup[i] = null;
				CapabilityFilterLookup[i] = null;
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
		/// Revokes all caps which end with the given text.
		/// </summary>
		/// <param name="capabilityNames"></param>
		/// <returns></returns>
		public Role RevokeIfEndsWith(params string[] capabilityNames)
		{
			for (var i = 0; i < capabilityNames.Length; i++)
			{
				var capabilityName = capabilityNames[i].ToLower();

				foreach (var capKvp in Capabilities.All)
				{
					if (!capKvp.Key.EndsWith(capabilityName))
					{
						continue;
					}

					var cap = capKvp.Value;

					// Revoke at that index:
					if (cap.InternalId >= CapabilityLookup.Length)
					{
						// Out of range - already not granted.
						continue;
					}

					// Remove from lookup:
					CapabilityLookup[cap.InternalId] = null;
					CapabilityFilterLookup[cap.InternalId] = null;
				}
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
            CapabilityFilterLookup = new Filter[copyFrom.CapabilityFilterLookup.Length];

			for (var i = 0; i < copyFrom.CapabilityLookup.Length; i++)
			{
				var node = copyFrom.CapabilityLookup[i];
				var srcFilter = copyFrom.CapabilityFilterLookup[i];

				if (node == null)
				{
					continue;
				}

				CapabilityLookup[i] = node.Copy();
				CapabilityFilterLookup[i] = srcFilter == null ? null : srcFilter.Copy();
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

		/// <summary>Gets the raw grant rule for the given capability. This is readonly.</summary>
		public Filter GetSourceFilter(Capability capability)
		{
			if (capability.InternalId >= CapabilityLookup.Length)
			{
				// Nope!
				return null;
			}

			return CapabilityFilterLookup[capability.InternalId];
		}

		/// <summary>Gets the raw grant rule for the given capability. This is readonly.</summary>
		public FilterNode GetGrantRule(Capability capability)
		{
			if (capability.InternalId >= CapabilityLookup.Length)
			{
				// Nope!
				return null;
			}

			return CapabilityLookup[capability.InternalId];
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
		public Task<bool> IsGranted(Capability capability, Context token, params object[] extraObjectsToCheck)
        {
            if (capability.InternalId >= CapabilityLookup.Length)
            {
                // Nope!
                return Task.FromResult(false);
            }

            var handler = CapabilityLookup[capability.InternalId];

            if (handler == null)
            {
                return Task.FromResult(false);
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
	/// Resolves If() statements on roles.
	/// </summary>
	public class RoleIfResolver
	{
		/// <summary>
		/// The role that this is for.
		/// </summary>
		public Role Role;

		/// <summary>
		/// Called for each grant.
		/// </summary>
		public Func<Filter, Filter> FilterResolver;


		/// <summary>
		/// If the previous chain resolves to true, then all the given capabilities will be granted.
		/// </summary>
		/// <param name="capabilityNames"></param>
		/// <returns></returns>
		public Role ThenGrant(params string[] capabilityNames)
		{
			// Grant the given set of caps to the given role
			// Using new instances of this grant chain.
			// We always use new instances in case people start directly using the grant chain on a particular capability
			// after applying a bulk if to a bunch of them.
			for (var i = 0; i < capabilityNames.Length; i++)
			{
				// Get the cap:
				var capabilityName = capabilityNames[i];

				Capability cap;
				if (!Capabilities.All.TryGetValue(capabilityName.ToLower(), out cap))
				{
					// If you ended up here, please check to that you're instancing a capability with the given name 
					// (it's probably a typo in your grant call).
					throw new Exception("Role '" + Role.Name + "' tried to grant '" + capabilityName + "' but that capability wasn't found.");
				}

				GrantInternal(cap);
			}

			return Role;
		}

		private void GrantInternal(Capability cap)
		{
			var filterType = typeof(Filter<>).MakeGenericType(new Type[] { cap.ContentType });
			var filter = Activator.CreateInstance(filterType) as Filter;
			filter = FilterResolver(filter);

			if (filter == null)
			{
				// Don't actually grant this.
				return;
			}

			var rootNode = filter.Construct();
			Role.Grant(cap, rootNode, filter);
		}

		/// <summary>
		/// If the previous chain resolves to true, then all the given capabilities will be granted.
		/// </summary>
		/// <param name="verbs"></param>
		/// <returns></returns>
		public Role ThenGrantVerb(params string[] verbs)
		{
			for (var i = 0; i < verbs.Length; i++)
			{
				verbs[i] = "_" + verbs[i].ToLower();
			}

			foreach (var kvp in Capabilities.All)
			{
				for (var i = 0; i < verbs.Length; i++)
				{
					if (kvp.Key.EndsWith(verbs[i]))
					{
						GrantInternal(kvp.Value);
						break;
					}
				}
			}

			/*
			var rootNode = Construct();

			// Grant the given set of caps to the given role
			// Using *duplicates* of this grant chain.
			// We duplicate in case people start directly using the grant chain on a particular capability
			// after applying a bulk if to a bunch of them.
			for (var i = 0; i < verbNames.Length; i++)
			{
				Role.GrantVerb(rootNode.Copy(), this, verbNames[i]);
			}
			*/

			return Role;
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