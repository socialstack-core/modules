using System;
using Api.Startup;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Api.Eventing;
using Api.Contexts;

namespace Api.Permissions
{

	/// <summary>
	/// Instances capabilities during the very earliest phases of startup.
	/// </summary>
	[EventListener]
	public class Init
	{

		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public Init()
		{
			var allPermittedEvents = Events.FindByPlacement(EventPlacement.NotSpecified);

			foreach (var permittedEvent in allPermittedEvents)
			{
				// Create a capability for this event type - e.g. "UserCreate". Creating it will automatically add it to the set.
				var capability = new Capability(permittedEvent.EntityName + permittedEvent.Verb);

				// Next, add an event handler at priority 1 (runs before others).
				permittedEvent.AddEventListener((Context context, object[] args) =>
				{
					// Check if the capability is granted.
					// If it is, return the first arg.
					// Otherwise, return null.
					var role = context == null ? Roles.Public : context.Role;

					if (role == null)
					{
						// No user role - can't grant this capability.
						// This is likely to indicate a deeper issue, so we'll warn about it:
						Console.WriteLine("Warning: User ID " + context.UserId + " has no role (or the role with that ID hasn't been instanced).");
						return null;
					}
					
					return args != null && args.Length > 0 && role.IsGranted(capability, context, args) ? args[0] : null;
				}, 1);
			}

			// Public - the role used by anonymous users.
			Roles.Public = new Role(0)
			{
				Name = "Public",
				Key = "public"
			}
			.GrantEverything();
			/*
			.Grant("UserCreate", "UserLogin", "UserPasswordForgot", "UserResetLoad", "CanvasLoad");
			
			// Public users can't see forum #1.
			Public.If()
				.Not().Id(typeof(Api.Forums.Forum), 1)
				.ThenGrant("ForumLoad");

			Public.Grant("AvailableEndpointList");

			// Public users can load forum replies if they can load the 
			// forum they're in or they're the creator of the thread.
			// The or is optional but permits a Nordic setup where private tickets are in a private forum
			// but as they're raised by a particular person, are viewable by them.
			Public.If()
				.Not().Equals(typeof(ForumReplies.Reply), "ForumId", 1)
				.Or()
				.IsSelf(typeof(ForumReplies.Reply), "ThreadCreatorId")
				.Or()
				.IsSelf(typeof(ForumReplies.Reply), "UserId")
				.ThenGrant("ForumReplyLoad");
			*/

			// Super admin - can do everything.
			// Note that you can grant everything and then revoke certain things if you want.
			Roles.SuperAdmin = new Role(1)
            {
                Name = "Super Admin",
                Key = "super_admin"
            }
            .GrantEverything();

			// Admin - can do almost everything. Usually everything super admin can do, minus system config options/ site level config.
			Roles.Admin = new Role(2)
            {
                Name = "Admin",
                Key = "admin"
            }
            .GrantTheSameAs(Roles.SuperAdmin); // <-- In this case, we grant the same as SA.

			// Guest - account created, not activated. Basically the same as a public account.
			Roles.Guest = new Role(3)
            {
                Name = "Guest",
                Key = "guest"
            }
            .GrantTheSameAs(Roles.Public); // <-- In this case, we grant the same as public.

			// Member - created and (optionally) activated.
			Roles.Member = new Role(4)
			{
				Name = "Member",
				Key = "member"
			}
			.GrantTheSameAs(Roles.Guest);
		}

	}
}
