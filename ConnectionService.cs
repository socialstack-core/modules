using Api.Contexts;
using Api.Eventing;
using Api.Permissions;
using Api.Users;
using Api.Startup;

namespace Api.Connections
{
	/// <summary>
	/// Handles connections (subscribers).
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class ConnectionService : AutoService<Connection>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public ConnectionService(UserService users) : base(Events.Connection)
        {
			InstallAdminPages("Connections", "fa:fa-user-friends", new string[] { "id", "email" });

			// Handles before creation to check the invited user. 
			Events.Connection.BeforeCreate.AddEventListener(async (Context context, Connection connection) =>
			{
				if(connection == null)
                {
					return null;
                }

				var user = context.User;

				if(user == null)
                {
					return null;
                }

				// Is the user inviting themselves?
				if (connection.ConnectedToId == context.UserId)
                {
					// We already have a connection with the target user. 
					throw new PublicException("You can't send invites to yourself.", "is_target");
				}

				// Is the user inviting themselves?
				if (!string.IsNullOrEmpty(connection.Email) && !string.IsNullOrEmpty(user.Email) && connection.Email.Trim().ToLower() == user.Email.Trim().ToLower())
                {
					// We already have a connection with the target user. 
					throw new PublicException("You can't send invites to yourself.", "is_target");
				}

				// You can invite by email or id. If you invite by email, we need to look up to see if there is a user affiliated to that email.
				if (!string.IsNullOrEmpty(connection.Email))
                {
					var connectToUser = await users.GetByEmail(context, connection.Email.Trim().ToLower());

					if(connectToUser != null)
                    {
						connection.ConnectedToId = connectToUser.Id;
                    }
				}
				
				var results = await Where("ConnectedToId=?", DataOptions.IgnorePermissions).Bind(connection.ConnectedToId).ListAll(context);
				
				if(results.Count > 0 )
                {
					// We already have a connection with the target user. 
					throw new PublicException("You have already sent an invitation to this user or are friends already.", "already_sent");
				}

				// TODO: I noticed that for some reason the user Id is not being set as the creator user properly - so for now, just a manual set here is occuring. 
				// The real TODO here is to make sure there isn't a better way to handle this later.
				connection.UserId = context.UserId;

				return connection;
			});
		}
	}
}