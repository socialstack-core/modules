using Api.Contexts;
using Api.Eventing;
using System.Net.Mail;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;

namespace Api.Emails;


/// <summary>
/// Hooks up the email system to use the faster and more reliable SendGrid API instead of its SMTP service.
/// </summary>
public class SendGridService : AutoService
{
	private SendGridClient _client;
	private SendGridConfig _cfg;
	private EmailAddress _from;
	/// <summary>
	/// Instanced automatically by [EventListener].
	/// </summary>
	public SendGridService()
	{
		_cfg = GetConfig<SendGridConfig>();
		_cfg.OnChange += () => {
			_client = null;
			return new System.Threading.Tasks.ValueTask();
		};

		Events.EmailTemplate.Send.AddEventListener(async (Context context, EmailToSend toSend) => {

			if (toSend.Handled)
			{
				return toSend;
			}

			if (string.IsNullOrEmpty(_cfg.ApiKey))
			{
				// Disabled
				return toSend;
			}

			var client = _client;
			var from = _from;

			if (_client == null)
			{
				client = new SendGridClient(_cfg.ApiKey);
				_client = client;
			}

			if (from == null)
			{
				from = new EmailAddress(_cfg.FromAddress, _cfg.FromName);
				_from = from;
			}

			var to = new EmailAddress(toSend.ToAddress, toSend.ToName);

			if (string.IsNullOrEmpty(toSend.Subject)) 
			{
				throw new Exception("E-Mail Subject cannot be null");
			}

			var msg = MailHelper.CreateSingleEmail(
				toSend.FromAccount != null ? new EmailAddress(toSend.FromAccount.FromAddress, null) : from,
				to,
				toSend.Subject,
				toSend.BodyPlain,
				toSend.Body
			);

			if (!string.IsNullOrEmpty(toSend.MessageId))
			{
				// Got a message ID:
				msg.AddHeader("Message-Id", toSend.MessageId);
			}

			if (toSend.AdditionalHeaders != null)
			{
				foreach (var header in toSend.AdditionalHeaders)
				{
					if (header.Key == "Reply-To")
					{
						msg.AddReplyTo(header.Value);
					}
					else
					{
						msg.AddHeader(header.Key, header.Value);
					}
				}
			}
			var response = await client.SendEmailAsync(msg);

			if (!response.IsSuccessStatusCode)
			{
				var sendGridResponse = await response.Body.ReadAsStringAsync();
				throw new Exception("Email send failed with response: " + sendGridResponse);
			}

			toSend.Handled = true;
			return toSend;
				
		}, 5); // Occurs before the default SMTP handler and sets Handled to true to block it.
		
	}
	
}