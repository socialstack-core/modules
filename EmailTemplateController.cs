using Api.Contexts;
using Api.Permissions;
using Api.SocketServerLibrary;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Api.Emails
{
    /// <summary>Handles emailTemplate endpoints.</summary>
    [Route("v1/emailtemplate")]
	public partial class EmailTemplateController : AutoController<EmailTemplate>
    {
	
		/// <summary>
		/// Sends a test email.
		/// </summary>
		/// <param name="mailTest"></param>
		/// <returns></returns>
		[HttpPost("test")]
		public async ValueTask TestEmail([FromBody] EmailTestRequest mailTest)
		{
			// Admin only:
			var context = await Request.GetContext();
			
			if (context.Role == null || !context.Role.CanViewAdmin)
			{
				throw PermissionException.Create("test_email", context);
			}
			
			var emailService = _service as EmailTemplateService;

			var recipients = new List<Recipient>();

			recipients.Add(new Recipient(context)
			{
				CustomData = mailTest.CustomData
			});

			var state = await emailService.SendAsync(recipients, mailTest.TemplateKey);

			var writer = Writer.GetPooled();
			writer.Start(null);

			writer.WriteASCII("{\"sent\":");
			writer.WriteASCII(state ? "true" : "false");
			writer.Write((byte)'}');

			await writer.CopyToAsync(Response.Body);
			writer.Release();
		}
		
    }
	
	/// <summary>
	/// Used to send a test email (admin only). Sends to the person who requests the test.
	/// </summary>
	public class EmailTestRequest
	{
		/// <summary>
		/// Template ID.
		/// </summary>
		public string TemplateKey;
		
		/// <summary>
		/// Custom scope.
		/// </summary>
		public object CustomData;
		
	}
}