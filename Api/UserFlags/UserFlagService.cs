using Api.Database;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using Api.Startup;


namespace Api.UserFlags
{
	/// <summary>
	/// Handles userFlags.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class UserFlagService : AutoService<UserFlag>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public UserFlagService() : base(Events.UserFlag)
        {

			#warning todo upgrade this!
			// Flags are using the old ContentTypeId and ContentId format which is no longer permitted as there can be a variety of IDs.

			/*
			// Note that there's a unique index on the table which blocks users
			// from flagging something twice.
			Events.UserFlag.BeforeCreate.AddEventListener(async (Context ctx, UserFlag flag) =>
			{
				if(flag == null)
                {
					return flag;
                }

				// Let's see if a user flag by this user on this content exists.
				List<UserFlag> flagCheck = await List(ctx, new Filter<UserFlag>().Equals("UserId", flag.UserId).And().Equals("ContentId", flag.ContentId).And().Equals("ContentTypeId", flag.ContentTypeId), DataOptions.IgnorePermissions);

				// Does a flag exist for this content by this user?
				if(flagCheck.Count > 0)
                {
					throw new PublicException("You have already flagged this", "already_flagged");
				}

				return flag;
			});

			Events.UserFlag.AfterCreate.AddEventListener(async (Context context, UserFlag flag) => {
				
				if(flag == null)
				{
					return flag;
				}
				
				// Get the flagged content:
				var content = await Content.Get(context, flag.ContentTypeId, flag.ContentId) as IAmFlaggable;
				
				if(content == null)
				{
					// That doesn't exist, or isn't flaggable
					return null;
				}
				
				await Content.Update(
					context,
					content,
					(Context ctx, object ob) => {
						(ob as IAmFlaggable).UserFlagCount++;
					},
					DataOptions.IgnorePermissions
				);
				
				return flag;
			});

			Events.UserFlag.AfterDelete.AddEventListener(async (Context context, UserFlag flag) =>
			{
				if (flag == null)
                {
					return flag;
                }

				// Get the flagged content: 
				var content = await Content.Get(context, flag.ContentTypeId, flag.ContentId) as IAmFlaggable;

				if(content == null)
                {
					// That doesn't exist, or isn't flaggable
					return null;
				}

				content.UserFlagCount--;
				if(content.UserFlagCount < 0)
                {
					content.UserFlagCount = 0;
                }

				await Content.Update(
					context,
					content,
					(Context ctx, object ob) => {

						var ct = (ob as IAmFlaggable);

						ct.UserFlagCount--;
						if (ct.UserFlagCount < 0)
						{
							ct.UserFlagCount = 0;
						}
					},
					DataOptions.IgnorePermissions
				);

				return flag;
			});
			*/
		}
	}
    
}
