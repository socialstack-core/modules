using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Permissions;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.DrawingCore;
using System.DrawingCore.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Api.Users;

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

				var user = await context.GetUser();

				if(user == null)
                {
					return null;
                }

				// Is the user inviting themselves?
				if (connection.ConnectedToId == context.UserId)
                {
					return null;
                }

				// Is the user inviting themselves?
				if (!string.IsNullOrEmpty(connection.Email) && !string.IsNullOrEmpty(user.Email) && connection.Email.Trim().ToLower() == user.Email.Trim().ToLower())
                {
					return null;
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
				
				var filter = new Filter<Connection>().Equals("UserId", context.UserId).And().Equals("ConnectedToId", connection.ConnectedToId);

				var results = await List(context, filter);
				
				if(results.Count > 0 )
                {
					return null;
                }

				return connection;
			});


			// Creates Connected user entry
			Events.Connection.AfterCreate.AddEventListener(async (Context context, Connection connection) =>
			{
				if (connection == null)
				{
					return null;
				}

				if(connection.ConnectedToId.HasValue && connection.ConnectedToId > 0)
                {
					var user = await users.GetProfile(context, connection.ConnectedToId.Value);

					connection.ConnectedToUser = user;
                }
				else
				{
					connection.ConnectedToUser = null;
				}

				return connection;
			});

			Events.Connection.AfterUpdate.AddEventListener(async (Context context, Connection connection) =>
			{
				if (connection == null)
				{
					return null;
				}

				if (connection.ConnectedToId.HasValue && connection.ConnectedToId > 0)
				{
					var user = await users.GetProfile(context, connection.ConnectedToId.Value);

					connection.ConnectedToUser = user;
				}
				else
				{
					connection.ConnectedToUser = null;
				}

				return connection;
			});

			Events.Connection.AfterLoad.AddEventListener(async (Context context, Connection connection) =>
			{
				if (connection == null)
				{
					return null;
				}

				if (connection.ConnectedToId.HasValue && connection.ConnectedToId > 0)
				{
					var user = await users.GetProfile(context, connection.ConnectedToId.Value);

					connection.ConnectedToUser = user;
				}
				else
				{
					connection.ConnectedToUser = null;
				}

				return connection;
			});


			Events.Connection.AfterList.AddEventListener(async (Context context, List<Connection> connections) =>
			{
				if (connections == null)
				{
					return null;
				}

				var userMap = new Dictionary<int, UserProfile>();

				foreach(var connection in connections)
                {
					if (connection == null || connection.ConnectedToId == null || connection.ConnectedToId == 0)
                    {
						continue;
                    }

					userMap[connection.ConnectedToId.Value] = null;
                }

				if (userMap.Count != 0)
                {
					var profileSet = await users.ListProfiles(context, new Filter<User>().EqualsSet("Id", userMap.Keys));

					if (profileSet != null)
					{
						foreach(var profile in profileSet)
                        {
							userMap[profile.Id] = profile;
						}
					}

					foreach(var connection in connections)
                    {
						if(connection == null || connection.ConnectedToId == null || connection.ConnectedToId == 0)
                        {
							continue;
                        }

						if (userMap.TryGetValue(connection.ConnectedToId.Value, out UserProfile profile))
						{
							connection.ConnectedToUser = profile;
						}
						else
						{
							connection.ConnectedToUser = null;
						}
					}

				}

				return connections;
			});
		}
	}
}