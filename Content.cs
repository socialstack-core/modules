using Api.Contexts;
using Api.Startup;
using Api.Users;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;


namespace Api.Database
{
	/// <summary>
	/// Gets content generically by its content type ID.
	/// </summary>
	public static class Content
	{

		/// <summary>
		/// Gets a piece of content from only its content ID and type.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="contentTypeId"></param>
		/// <param name="contentId"></param>
		/// <param name="convertUser">Converts User objects to UserProfile if true (default).</param>
		/// <returns></returns>
		public static async Task<object> Get(Context context, int contentTypeId, int contentId, bool convertUser = true)
		{
			// Get the service:
			var service = Services.GetByContentTypeId(contentTypeId);

			if (service == null)
			{
				return null;
			}

			var objResult = await service.GetObject(context, contentId);

			// Special case for users, up until UserProfile is removed.
			if (convertUser && objResult is User)
			{
				objResult = await Services.Get<IUserService>().GetProfile(context, objResult as User);
			}

			return objResult;
		}

    }
}
