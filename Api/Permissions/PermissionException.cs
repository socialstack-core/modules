using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using Api.Contexts;
using Api.Startup;
using Api.Users;

namespace Api.Permissions
{
    /// <summary>
    /// The requested resource is not accessible
    /// </summary>
    public class PermissionException : PublicException
    {
		/// <summary>
		/// Creates a new permission exception.
		/// </summary>
		/// <param name="capability"></param>
		/// <param name="context"></param>
		/// <param name="notes">Optional notes</param>
		/// <returns></returns>
		public static PermissionException Create(string capability, Context context, string notes = null)
		{
			string msg;

			if (context == null || context.UserId == 0)
			{
				msg = "Anonymous users don't have access to " + capability;
			}
			else
			{
				msg = $"The user #{context.UserId} has no access to {capability}";
			}

			if (notes != null)
			{
				msg += ". " + notes;
			}
			return new PermissionException(capability, context, msg);
		}

		/// <summary>
		/// The capability this occurred for.
		/// </summary>
		public string Capability;

		/// <summary>
		/// The context this occurred in.
		/// </summary>
		public Context Context;

		/// <summary>
		/// Use Create instead.
		/// </summary>
		/// <param name="capability"></param>
		/// <param name="context"></param>
		/// <param name="msg"></param>
		internal PermissionException(string capability, Context context, string msg) : base(msg, "permissions", 403)
        {
			Capability = capability;
			Context = context;
		}

    }
}
