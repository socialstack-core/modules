using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using Api.Startup;
using Api.Emails;

namespace Api.Messages
{
	/// <summary>
	/// Handles messages.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class MessageService : AutoService<Message>
    {
		private MessageServiceConfig _config;
		private readonly EmailTemplateService _emailService;


		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public MessageService(EmailTemplateService emailService) : base(Events.Message)
        {
			_emailService = emailService;
			_config = GetConfig<MessageServiceConfig>();
		}
		
		/// <summary>
		/// Sends a support email
		/// </summary>
		public async ValueTask SendSupportEmail(Context context, SupportEmailRequest supportEmailRequest)
		{

			if (string.IsNullOrEmpty(_config.SupportEmailAddress))
			{
				throw new PublicException("Support email is unavailable.", "not_configured");
			}

			string replyTo;

			if (!string.IsNullOrEmpty(supportEmailRequest.EmailAddress))
			{
				replyTo = supportEmailRequest.EmailAddress;
			}
			else if (context.User != null && !string.IsNullOrEmpty(context.User.Email))
			{
				replyTo = context.User.Email;
			}
			else
			{
				throw new PublicException("No email address provided.", "no_reply_to");
			}

			var additionalHeaders = new Dictionary<string, string>() {
				{ "Reply-To", replyTo }
			};

			await _emailService.Send(
				_config.SupportEmailAddress,
				"New support request", 
				"Their email address<br>" + replyTo + "<br><br>Their message<br>" + supportEmailRequest.Body + "<br><br>---<br><br>Replies to this email will be sent to " + replyTo + ".",
				null, 
				null,
				null,
				additionalHeaders
			);
		}
		
	}
    
}
