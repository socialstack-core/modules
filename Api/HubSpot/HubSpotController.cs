using Api.Contexts;
using Api.Startup;
using Api.Users;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Api.HubSpot
{
    /// <summary>Handles hubspot endpoints.</summary>
    [Route("v1/hubspot")]
    public partial class HubSpotController : AutoController
    {
		private UserService _userService;

		/// <summary>
		/// List contact details (test/debug of connection)
		/// </summary>
		[HttpGet("/contacts")]
		public async ValueTask<HubSpotListResponse<HubSpotEntity>> Contacts(Context context)
		{
			if (context.Role == null || !context.Role.CanViewAdmin)
			{
				throw new PublicException("Admin only", "HubSpot/admin_required");
			}

			return await Services.Get<HubSpotService>().List("contacts");
		}

		/// <summary>
		/// List company details (test/debug of connection)
		/// </summary>
		[HttpGet("/companies")]
		public async ValueTask<HubSpotListResponse<HubSpotEntity>> Companies(Context context)
		{
			if (context.Role == null || !context.Role.CanViewAdmin)
			{
				throw new PublicException("Admin only", "HubSpot/admin_required");
			}

			return await Services.Get<HubSpotService>().List("companies");
		}



		/// <summary>
		/// List contact details (test/debug of connection)
		/// </summary>
		[HttpGet("/contact/{email}")]
		public async ValueTask<HubSpotEntity> ContactByEmail(Context context, [FromRoute] string email)
		{
			if (context.Role == null || !context.Role.CanViewAdmin)
			{
				throw new PublicException("Admin only", "HubSpot/admin_required");
			}

			var result = await Services.Get<HubSpotService>().Search(
				new[] { (property: "email", op: "EQ", value: email) },
				limit: 1,
				properties: null,
				type:"contacts"
			);

			if (result != null && result.Results.Count == 1) { 
				return result.Results[0];
			}

			return null;
		}

		/// <summary>
		/// Push a ss user into hubspot (normally performed via automation)
		/// </summary>
		[HttpGet("/contact/create/{userId}")]
		public async ValueTask<HubSpotEntity> CreateContact(Context context, [FromRoute] uint userId)
		{
			if (context.Role == null || !context.Role.CanViewAdmin)
			{
				throw new PublicException("Admin only", "HubSpot/admin_required");
			}

			_userService ??= Services.Get<UserService>();
			var user = await _userService.Get(context, userId);

			if (user == null) 
			{ 
				return null;
			}

			var result = await Services.Get<HubSpotService>().CreateEntity<User,uint>(context, user, user.Email, "email", "contacts");

			return result.entity;
		}
	}
}