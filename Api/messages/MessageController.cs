using Api.Contexts;
using Api.SocketServerLibrary;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Api.Messages
{
    /// <summary>Handles message endpoints.</summary>
    [Route("v1/message")]
	public partial class MessageController : AutoController<Message>
    {
		
		/// <summary>
		/// Sends a support email.
		/// </summary>
		[HttpPost("support")]
		public async ValueTask SendSupportEmail([FromBody] SupportEmailRequest supportEmail)
		{
			var context = await Request.GetContext();
			
			// Attempt to send:
			await (_service as MessageService).SendSupportEmail(context, supportEmail);
			
			// It threw a public ex if something went wrong.
			
			// Just a simple 200 json response:
			
			Response.ContentType = "application/json";
			
			var writer = Writer.GetPooled();
			writer.Start(null);
			writer.WriteASCII("{\"success\":1}");
			await writer.CopyToAsync(Response.Body);
			writer.Release();
		}
		
    }
	
	/// <summary>
	/// Support email request.
	/// </summary>
	public class SupportEmailRequest
	{
		/// <summary>
		/// Email body.
		/// </summary>
		public string Body;
		
		/// <summary>
		/// Users email address if not logged in.
		/// </summary>
		public string EmailAddress;
		
	}
}