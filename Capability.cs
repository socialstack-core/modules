using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using Api.Contexts;
using System.Threading.Tasks;

namespace Api.Permissions
{
	/// <summary>
	/// A particular capability. Functionality asks if capabilities are granted or not.
	/// Modules can define capabilities via simply extending the Capabilities class.
	/// </summary>
	public class Capability
    {
        /// <summary>
        /// Capability string name. Of the form "LeadCreate". 
        /// </summary>
        public string Name = "";
        /// <summary>
        /// An index for high speed capability lookups within roles. 
        /// Not consistent across runs - don't store in the database. Use Name instead.
        /// </summary>
        public readonly int InternalId;

        /// <summary>
        /// The content type that this cap came from (if any).
        /// </summary>
        public Type ContentType;

        /// <summary>
        /// Create a new capability. Should only do this once during startup, and typically via extending Capabilities class.
        /// </summary>
        /// <param name="name">
        /// A textual name to represent your capability. Always the same as the field name in the Capabilities class.
        /// </param>
        public Capability(string name = "")
        {
            Name = name;

            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("Capabilities require a name.");
            }
			
			if(Capabilities.All == null){
				Capabilities.All = new Dictionary<string, Capability>();
			}
			
            // Add to name lookup and set the internal ID:
            InternalId = Capabilities.All.Count;
            Capabilities.All[name.ToLower()] = this;
        }

        /// <summary>
        /// Use this to check if the capability is granted to the current user.
        /// Note that this isn't virtual for a reason: All capabilities are the same.
        /// It is all about how they are granted.
        /// </summary>
        /// <param name="request">The current http request where we'll obtain the user from.</param>
        /// <param name="extraArg">
        /// E.g. if you're checking to see if something can be edited by the current user, pass that something.
        /// </param>
        /// <returns>True if it's permitted, false otherwise.</returns>
        public Task<bool> IsGranted(HttpRequest request, object extraArg)
        {
            var token = (request.HttpContext.User as Context);
            var role = token == null ? Roles.Public : token.Role;

            if (role == null) {
				// No user role - can't grant this capability.
				// This is likely to indicate a deeper issue, so we'll warn about it:
				Console.WriteLine("Warning: User ID " + token.UserId + " has no role (or the role with that ID hasn't been instanced).");
				return Task.FromResult(false);
            }
            
            return role.IsGranted(this, token, extraArg);
        }

		/// <summary>
		/// Use this to check if the capability is granted to the current user.
		/// Note that this isn't virtual for a reason: All capabilities are the same.
		/// It is all about how they are granted.
		/// </summary>
		/// <param name="request">The current http request where we'll obtain the user from.</param>
		/// <param name="filter">
		/// If this capability is granted at all, the given runtime only filter is updated with additional role restrictions.
		/// Use this to easily role restrict searches.</param>
		/// <param name="extraObjectsToCheck">
		/// E.g. if you're checking to see if something can be edited by the current user, pass that something.
		/// </param>
		/// <returns>True if it's permitted, false otherwise.</returns>
		public bool IsGranted(HttpRequest request, Filter filter, params object[] extraObjectsToCheck)
		{
			var token = (request.HttpContext.User as Context);
			var role = token == null ? Roles.Public : token.Role;

			if (role == null)
			{
				// No user role - can't grant this capability.
				// This is likely to indicate a deeper issue, so we'll warn about it:
				Console.WriteLine("Warning: User ID " + token.UserId + " has no role (or the role with that ID hasn't been instanced).");
				return false;
			}

			return role.IsGranted(this, token, filter, extraObjectsToCheck);
		}

		/// <summary>
		/// Always grants a capability.
		/// Used internally by Role.Grant("..") when no handler is given.
		/// </summary>
		/// <returns></returns>
		public static bool AlwaysGrant(Capability capability, Context token, object[] extraObjectsToCheck)
        {
            // You shall pass!
            return true;
        }

        /// <summary>
        /// Rejects a capability.
        /// Used internally by isAlsoGranted if the target cap hasn't actually been granted yet.
        /// </summary>
        /// <returns></returns>
        public static bool AlwaysReject(Capability capability, Context token, object[] extraObjectsToCheck)
        {
            // You shall not pass!
            return false;
        }
		
    }
}
