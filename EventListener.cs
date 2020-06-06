using Api.Contexts;
using Api.Eventing;
using Api.Startup;
using Api.Users;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace Api.LoginOnRegister
{

	/// <summary>
	/// Logs in a user when they register.
	/// </summary>
	[EventListener]
	public class EventListener
	{
		private IContextService _contexts;

		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public EventListener()
		{
            Events.User.Created.AddEventListener(async (Context context, User user, HttpResponse response) => {

				if (user == null)
				{
					return null;
				}

				// If you're anonymous then it logs in.
				// Otherwise you stay as-is.

				// Ensure user exists:
				var usr = await context.GetUser();

				if (usr != null)
				{
					// Not anon - probaably being made by admin.
					return user;
				}

				if (_contexts == null)
				{
					_contexts = Services.Get<IContextService>();
				}

				context.UserId = user.Id;
				context.UserRef = user.LoginRevokeCount;
				context.RoleId = user.Role;

				context.SendToken(response);

				return user;
			});
		}
	}
}
