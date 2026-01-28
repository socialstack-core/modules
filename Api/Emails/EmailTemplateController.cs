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
		/// <param name="context"></param>
		/// <param name="mailTest"></param>
		/// <returns></returns>
		[HttpPost("test")]
		public async ValueTask<EmailTestResponse> TestEmail(Context context, [FromBody] EmailTestRequest mailTest)
		{
			// Admin only
			if (context.Role == null || !context.Role.CanViewAdmin)
			{
				throw PermissionException.Create("test_email", context);
			}
			
			var emailService = _service as EmailTemplateService;

			var recipient = new Recipient(context)
			{
				CustomData = mailTest.CustomData == null ? null : Newtonsoft.Json.JsonConvert.DeserializeObject(mailTest.CustomData)
			};

			var state = await emailService.SendAndWaitForSuccess(recipient, mailTest.TemplateKey);

			return new EmailTestResponse()
			{
				Sent = state
			};
		}
		
    }

	/// <summary>
	/// Email test endpoint response.
	/// </summary>
	public struct EmailTestResponse
	{
		/// <summary>
		/// True if it succeeded.
		/// </summary>
		public bool Sent;
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
		public string CustomData;
		
	}
}