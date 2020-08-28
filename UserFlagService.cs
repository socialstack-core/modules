using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;

namespace Api.UserFlags
{
	/// <summary>
	/// Handles userFlags.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class UserFlagService : AutoService<UserFlag>, IUserFlagService
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public UserFlagService() : base(Events.UserFlag)
        {
			// Note that there's a unique index on the table which blocks users
			// from flagging something twice.
			
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
				
				content.UserFlagCount++;
				await Content.Update(context, content);
				
				return flag;
			});
		}
	}
    
}
