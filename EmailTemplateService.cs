using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Api.Database;
using Microsoft.Extensions.Configuration;
using System.Text;
using Microsoft.AspNetCore.Http;
using System.ComponentModel;
using Api.Configuration;
using Api.CanvasRenderer;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using Api.Startup;
using Api.Users;

namespace Api.Emails
{
	/// <summary>
	/// Handles emailTemplates.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class EmailTemplateService : AutoService<EmailTemplate>, IEmailTemplateService
	{
		/// <summary>
		/// The priority value used when adding an email event handler automatically.
		/// A high value essentially means everything else happens, then the email is sent.
		/// </summary>
		public const int EmailHandlerPriority = 1000;

		private EmailConfig _configuration;

		private ICanvasRendererService _canvasRendererService;

		private IUserService _users;

		/// <summary>
		/// A reference to the key index from the cache.
		/// </summary>
		private int KeyIndexId;

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public EmailTemplateService(ICanvasRendererService canvasRendererService, IUserService users) : base(Events.EmailTemplate)
		{
			_users = users;
			_canvasRendererService = canvasRendererService;
			_configuration = AppSettings.GetSection("Email").Get<EmailConfig>();
			
			InstallAdminPages("Emails", "fa:fa-paper-plane", new string[] { "id", "name", "key" });

			// Cache all in memory:
			Cache();

			// Get the index ID of the DB index called 'Key':
			KeyIndexId = GetCacheIndexId("Key");
		}

		/// <summary>
		/// Gets an email template by its key.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="key"></param>
		/// <returns></returns>
		public Task<EmailTemplate> GetByKey(Context context, string key)
		{
			// Note for future: Would be nice if this was automatic, rather than explicitly using a cache key.
			// I.e. the rest of the service interface - namely List(..) - figures out a key to use.
			var cache = GetCacheForLocale(context == null ? 1 : context.LocaleId);

			if (cache == null)
			{
				return Task.FromResult((EmailTemplate)null);
			}

			var template = cache.GetUsingIndex(KeyIndexId, key);
			return Task.FromResult(template);
		}

		/// <summary>
		/// Renders an email with the given key using the given recipient info.
		/// </summary>
		/// <param name="key">Template key to use.</param>
		/// <param name="recipient">Mainly used for localisation. The end user's context.</param>
		/// <returns></returns>
		public async Task<RenderedCanvas> Render(string key, Recipient recipient)
		{
			var template = await GetByKey(recipient.Context, key);

			if (template == null || recipient == null)
			{
				return null;
			}

			// Render the template now:
			return await _canvasRendererService.Render(template.BodyJson, recipient.CustomData);
		}

		/// <summary>
		/// Ensures each recipient instance has a User loaded, and that it's also set into the CustomData.
		/// Note that we don't support sending to emails only, as a user is required to be able to track opt-out state.
		/// </summary>
		/// <param name="recipients"></param>
		private async Task LoadUsers(IList<Recipient> recipients)
		{
			List<int> idsToLoad = null;

			foreach (var recipient in recipients)
			{
				if (recipient == null)
				{
					continue;
				}

				if (recipient.CustomData == null)
				{
					recipient.CustomData = new CanvasContext();
				}

				if (recipient.User != null)
				{
					// User is always provided:
					recipient.CustomData["User"] = recipient.User;
					continue;
				}

				if (recipient.UserId != 0)
				{
					// Load this user:
					if (idsToLoad == null)
					{
						idsToLoad = new List<int>();
					}

					idsToLoad.Add(recipient.UserId);
				}
			}

			if (idsToLoad != null)
			{
				var loadedUsers = await _users.List(new Context(), new Filter<User>().Id(idsToLoad));

				if (loadedUsers != null && loadedUsers.Count > 0)
				{
					// Create lookup:
					var userLookup = new Dictionary<int, User>();
					foreach (var user in loadedUsers)
					{
						userLookup[user.Id] = user;
					}

					// Apply to recipients:
					foreach (var recipient in recipients)
					{
						if (recipient == null || recipient.User != null)
						{
							continue;
						}

						if (recipient.UserId != 0)
						{
							// Try setting the user object now:
							if (userLookup.TryGetValue(recipient.UserId, out recipient.User))
							{
								// User exists and is now set in the recipient.
								// Set into CustomData too:
								recipient.CustomData["user"] = recipient.User;
							}
						}
					}
				}

			}

		}

		/// <summary>
		/// Sends emails to the given recipients without waiting for it to complete.
		/// </summary>
		/// <param name="recipients"></param>
		/// <param name="key"></param>
		public void Send(IList<Recipient> recipients, string key)
		{
			Task.Run(async () =>
			{
				await SendAsync(recipients, key);
			});
		}

		/// <summary>
		/// Sends the given email to the given list of recipients.
		/// </summary>
		/// <param name="recipients"></param>
		/// <param name="key"></param>
		/// <returns></returns>
		public async Task<bool> SendAsync(IList<Recipient> recipients, string key)
		{
			// First, make sure we have users loaded for all recipients.
			await LoadUsers(recipients);

			// Next, group recipients by locale:
			// This is because each locale effectively has a different template object.
			var recipientsByLocale = new Dictionary<int, TemplateAndRecipientSet>();

			foreach (var recipient in recipients)
			{
				if (recipient == null || recipient.User == null || recipient.User.Email == null || recipient.Context == null)
				{
					continue;
				}

				// Locale is either from the Context, or the user's last locale.
				var localeId = recipient.Context.LocaleId;

				if (localeId <= 0)
				{
					localeId = recipient.User.LocaleId.HasValue ? recipient.User.LocaleId.Value : 1;
				}

				if (localeId <= 0)
				{
					// Site default:
					localeId = 1;
				}

				if (!recipientsByLocale.TryGetValue(localeId, out TemplateAndRecipientSet set))
				{
					// Create the new set now:
					set = new TemplateAndRecipientSet();
					
					// Load the template:
					var template = await GetByKey(new Context() {
						LocaleId = localeId
					}, key);

					set.Template = template;

					if (template != null)
					{
						set.BodyJson = template.BodyJson;
					}

					// Add it:
					recipientsByLocale[localeId] = set;
				}

				set.Recipients.Add(recipient);
				set.Contexts.Add(recipient.CustomData.Context);
			}

			// For each locale block, render the emails:
			foreach (var localeKvp in recipientsByLocale)
			{
				// Get the set of canvas + all the contexts:
				var set = localeKvp.Value;

				// Fallback email subject:
				var fallbackSubject = set.Template == null ? null : set.Template.Subject;

				// Render all. The results are in the exact same order as the recipients set.
				List<RenderedCanvas> renderedCanvases = await _canvasRendererService.Render(set);

				/*
				 new List<RenderedCanvas>() {
					new RenderedCanvas(){
						Body = "<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" id=\"body\" style=\"text-align: center; min-width: 640px; width: 100%; margin: 0; padding: 0;\" bgcolor=\"#f0f3f7\"><tbody><tr class=\"line\"><td style=\"font-family: &quot;Helvetica Neue&quot;, Helvetica, Arial, sans-serif; height: 4px; font-size: 4px; line-height: 4px;\" bgcolor=\"#7068d6\"></td></tr><tr class=\"header\"><td style=\"font-family: &quot;Helvetica Neue&quot;, Helvetica, Arial, sans-serif; font-size: 13px; line-height: 1.6; color: #5c5c5c; padding: 25px 0;\"><img alt src=\"https://ivip.cf/email_logo.png\" width=\"55\" height=\"50\" /></td></tr><tr><td style=\"font-family: &quot;Helvetica Neue&quot;, Helvetica, Arial, sans-serif;\"><table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" class=\"wrapper\" style=\"width: 640px; border-collapse: separate; border-spacing: 0; margin: 0 auto;\"><tbody><tr><td class=\"wrapper-cell\" style=\"font-family: &quot;Helvetica Neue&quot;, Helvetica, Arial, sans-serif; border-radius: 3px; overflow: hidden; padding: 18px 25px; border: 1px solid #ededed;\" align=\"left\" bgcolor=\"#ffffff\"><p>Hi ,</p><table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" class=\"content\" style=\"width: 100%; border-collapse: separate; border-spacing: 0;\"><tbody><tr><td class=\"text-content\" style=\"font-family: &quot;Helvetica Neue&quot;, Helvetica, Arial, sans-serif; color: #333333; font-size: 15px; font-weight: 400; line-height: 1.4; padding: 15px 5px;\" align=\"center\"><span>We received a request to reset your password. If this was you, click the following link to proceed:</span><table role=\"presentation\" style=\"margin: auto;\" cellspacing=\"0\" cellpadding=\"0\" border=\"0\" align=\"center\"><tbody><tr><td class=\"button-td button-td-primary\" style=\"border-radius: 4px; background: #222222;\"><a class=\"button-a button-a-primary\" href=\"https://ivip.cf/\" style=\"background: #222222; border: 1px solid #000000; font-family: sans-serif; font-size: 15px; line-height: 15px; text-decoration: none; padding: 13px 17px; color: #ffffff; display: block; border-radius: 4px;\">Reset my password</a></td></tr></tbody></table></td></tr></tbody></table></td></tr></tbody></table></td></tr><tr class=\"footer\"><td style=\"font-family: &quot;Helvetica Neue&quot;, Helvetica, Arial, sans-serif; font-size: 13px; line-height: 1.6; color: #5c5c5c; padding: 25px 0;\"><div>You're receiving this email because of your account with us. <a class=\"mng-notif-link\" href=\"https://source.socialstack.cf/profile/notifications\" style=\"color: #3777b0; text-decoration: none;\">Manage all notifications</a> · <a class=\"help-link\" href=\"https://source.socialstack.cf/help\" style=\"color: #3777b0; text-decoration: none;\">Help</a></div></td></tr><tr><td class=\"footer-message\" style=\"font-family: &quot;Helvetica Neue&quot;, Helvetica, Arial, sans-serif; font-size: 13px; line-height: 1.6; color: #5c5c5c; padding: 25px 0;\"></td></tr></tbody></table>"
					}
				}; //
				 */

				// Actually send each one next.
				for (var i=0;i<set.Recipients.Count;i++)
				{
					var recipient = set.Recipients[i];

					var renderedCanvas = renderedCanvases[i];

					// Email to send to:
					var targetEmail = recipient.User.Email;

					// Email subject:
					var subject = string.IsNullOrEmpty(renderedCanvas.Title) ? fallbackSubject : renderedCanvas.Title;

					// Send now:
					await Send(targetEmail, subject, renderedCanvas.Body);
				}
			}

			return true;
		}

		/// <summary>
		/// Direct sends an email to the given address. Doesn't block this thread.
		/// </summary>
		/// <param name="toAddress">Target email address.</param>
		/// <param name="subject">Email subject.</param>
		/// <param name="body">Email body (HTML).</param>
		public async Task Send(string toAddress, string subject, string body, string messageId = null, EmailAccount fromAccount = null)
		{
			if (fromAccount == null)
			{
				fromAccount = _configuration.Accounts["default"];
			}

			SmtpClient client = new SmtpClient(fromAccount.Server);
			client.UseDefaultCredentials = false;
			client.Credentials = new NetworkCredential(fromAccount.User, fromAccount.Password);
			client.Port = fromAccount.Port;
			client.EnableSsl = fromAccount.Encrypted;

			MailMessage mailMessage = new MailMessage();
			mailMessage.IsBodyHtml = true;

			if (!string.IsNullOrEmpty(messageId))
			{
				// Got a message ID:
				mailMessage.Headers.Add("Message-Id", messageId);
			}

			mailMessage.DeliveryNotificationOptions = DeliveryNotificationOptions.OnSuccess | DeliveryNotificationOptions.OnFailure | DeliveryNotificationOptions.Delay;
			mailMessage.From = new MailAddress(fromAccount.FromAddress);
			mailMessage.To.Add(toAddress);
			mailMessage.Body = body;

			if (!string.IsNullOrEmpty(fromAccount.ReplyTo))
			{
				mailMessage.ReplyToList.Add(fromAccount.ReplyTo);
			}

			mailMessage.Subject = subject;

			await client.SendMailAsync(mailMessage);
		}
		
	}

	/// <summary>
	/// A pairing of a template and a block of recipients to send it to.
	/// </summary>
	public class TemplateAndRecipientSet : CanvasAndContextSet
	{
		/// <summary>
		/// The email template to receive.
		/// </summary>
		public EmailTemplate Template;

		/// <summary>
		/// The set of recipients that'll receive this template.
		/// </summary>
		public List<Recipient> Recipients = new List<Recipient>();
	}

}
