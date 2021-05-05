using Api.Contexts;
using Api.PasswordAuth;
using Api.Permissions;
using Api.Startup;
using Api.Users;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;


namespace Api.Invites
{
    /// <summary>
    /// Handles invite endpoints.
    /// </summary>
    [Route("v1/invite")]
	public partial class InviteController : AutoController<Invite>
    {
		/// <summary>
		/// Attempts to redeem an invite by the given token
		/// </summary>
		[HttpPost("redeem")]
		public async ValueTask Redeem([FromBody] InviteData inviteData)
		{
			var context = await Request.GetContext();
			
			// Attempt to redeem invite:
			var invite = await (_service as InviteService).Redeem(context, inviteData.Token);
			
			if (invite == null)
			{
				Response.StatusCode = 404;
				return;
			}
			
			// Context was updated:
			await OutputContext(context);
		}
	}
	
	/// <summary>
	/// Redeems an invite by its token
	/// </summary>
	public class InviteData
	{
		/// <summary>
		/// The token.
		/// </summary>
		public string Token;
	}
	
}