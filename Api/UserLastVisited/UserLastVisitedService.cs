using System.Threading.Tasks;
using Api.Contexts;
using Api.Eventing;
using System;
using Api.Users;
using Microsoft.AspNetCore.Http;
using Api.Pages;
using Api.Permissions;
using Api.Startup;

namespace Api.UserLastVisited
{
	/// <summary>
	/// Handles users. Updates last visited timestamp.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class UserLastVisitedService : AutoService
	{
		private UserLastVisitedConfig _config;
		private UserService _users;
		
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public UserLastVisitedService(UserService users)
        {
			_users = users;
			_config = GetConfig<UserLastVisitedConfig>();

			Events.User.BeforeSettable.AddEventListener((Context context, JsonField<User, uint> field) => {

				if (field == null)
				{
					return new ValueTask<JsonField<User, uint>>(field);
				}

				if (field.Name == "LastVisitedUtc")
				{
					// Only the C# API can set this internally. It can't be done via the public web API.
					field.Writeable = false;
				}

				return new ValueTask<JsonField<User, uint>>(field);
			});

            Events.Page.BeforeNavigate.AddEventListener((Context context, PageWithTokens pageTokens) => {
			
				if (_config != null && !_config.Disabled && context.UserId > 0)
				{
					var ctx = context;

                    // Don't await this one:
                    _ = Task.Run(async () =>
					{
						if (ctx.User != null && ctx.User.LastVisitedUtc.Date != DateTime.UtcNow.Date)
						{
                            await _users.Update(ctx, ctx.User, (Context ctx, User usr, User orig) =>
							{
								usr.LastVisitedUtc = DateTime.UtcNow.Date;
							}, DataOptions.IgnorePermissions | DataOptions.CheckNotChanged);
						}
					});		
				}

                return new ValueTask<PageWithTokens>(pageTokens);
            });
		}
	}    
}
