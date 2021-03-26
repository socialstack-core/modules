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
        /// Current max assigned ID.
        /// </summary>
        private static int _CurrentId = 0;

        /// <summary>
        /// Capability string name. Of the form "lead_create". Always lowercase.
        /// </summary>
        public string Name = "";

        /// <summary>
        /// Just the feature. Of the form "create". Always lowercase.
        /// </summary>
        public string Feature = "";
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
        /// Create a new capability.
        /// </summary>
        /// <param name ="contentType"></param>
        /// <param name="feature">
        /// Just the feature name, e.g. "List" or "Create".
        /// </param>
        public Capability(Type contentType, string feature = "")
        {
            if (string.IsNullOrEmpty(feature))
            {
                throw new ArgumentNullException(nameof(feature), "Capabilities require a feature name.");
            }

            ContentType = contentType;
            Feature = feature.ToLower();
            Name = contentType == null ? Feature : contentType.Name.ToLower() + "_" + Feature;
            InternalId = _CurrentId++;
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

    }
}
