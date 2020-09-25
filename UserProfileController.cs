using System;
using System.Threading.Tasks;
using Api.Permissions;
using Microsoft.AspNetCore.Mvc;
using Api.Database;
using Api.Emails;
using Api.Contexts;
using Api.Uploader;
using Api.Results;
using Api.Eventing;
using Newtonsoft.Json.Linq;
using Api.AutoForms;

namespace Api.Users
{
    /// <summary>
    /// Handles public user account endpoints.
    /// </summary>
    [Route("v1/userprofile")]
	[ApiController]
	public partial class UserProfileController : ControllerBase
    {
		private UserService _users;
		
		/// <summary>
		/// Instanced automatically.
		/// </summary>
		/// <param name="users"></param>
		public UserProfileController(UserService users){
			_users = users;
		}
		
		/// <summary>
		/// GET /v1/userprofile/2/
		/// Returns the data for 1 entity.
		/// </summary>
		[HttpGet("{id}")]
		public virtual async Task<object> Load([FromRoute] int id)
		{
			var context = Request.GetContext();
			var result = await _users.GetProfile(context, id);
			return await Events.UserProfileLoad.Dispatch(context, result, Response);
		}
		
		/// <summary>
		/// GET /v1/userprofile/list
		/// Lists all entities of this type available to this user.
		/// </summary>
		/// <returns></returns>
		[HttpGet("list")]
		public virtual async Task<object> List()
		{
			return await List(null);
		}

		/// <summary>
		/// POST /v1/userprofile/list
		/// Lists filtered entities available to this user.
		/// See the filter documentation for more details on what you can request here.
		/// </summary>
		/// <returns></returns>
		[HttpPost("list")]
		public virtual async Task<object> List([FromBody] JObject filters)
		{
			var context = Request.GetContext();
			var filter = new Filter<User>(filters);

			filter = await Events.UserProfileList.Dispatch(context, filter, Response);

			if (filter == null)
			{
				// A handler rejected this request.
				return null;
			}
			
			ListWithTotal<UserProfile> response;

			if (filter.PageSize != 0 && filters != null && filters["includeTotal"] != null)
			{
				// Get the total number of non-paginated results as well:
				response = await _users.ListProfilesWithTotal(context, filter);
			}
			else
			{
				// Not paginated or requestor doesn't care about the total.
				var results = await _users.ListProfiles(context, filter);
				
				response = new ListWithTotal<UserProfile>()
				{
					Results = results
				};

				if (filter.PageSize == 0)
				{
					// Trivial instance - pagination is off so the total is just the result set length.
					response.Total = results == null ? 0 : results.Count;
				}
			}
			
			response.Results = await Events.UserProfileListed.Dispatch(context, response.Results, Response);

			return response;
		}
		
	}	
}