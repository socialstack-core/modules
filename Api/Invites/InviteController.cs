using Api.Contexts;
using Api.Database;
using Api.PasswordAuth;
using Api.Permissions;
using Api.SocketServerLibrary;
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
		public async ValueTask Redeem([FromBody] InviteData inviteData, [FromQuery] string includes = null)
		{
			var context = await Request.GetContext();
			
			// Attempt to redeem invite:
			var invite = await (_service as InviteService).Redeem(context, inviteData);
			
			if (invite == null)
			{
				Response.StatusCode = 404;
				return;
			}

			// Special situation - must return both the context and the invite.
			// The following code is a combination of both OutputContext and OutputJson.
			var targetStream = Response.Body;

			var writer = Writer.GetPooled();
			writer.Start(null);
			writer.WriteASCII("{\"context\":");

			await Services.Get<ContextService>().ToJson(context, writer);
			
			// Copy to output:
			writer.WriteASCII(",\"invite\":{\"result\":");

			// Next, the invite:
			await _service.ToJson(context, invite, writer, targetStream, includes, false);

			writer.WriteASCII("}}");
			await writer.CopyToAsync(targetStream);

			writer.Release();
		}
	}
	
	/// <summary>
	/// Redeems an invite by its token. 
	/// As redeeming an invite may result in a login, additional login data can also be carried here.
	/// </summary>
	public class InviteData : UserLogin
	{
		/// <summary>
		/// The token.
		/// </summary>
		public string Token;
	}
	
}