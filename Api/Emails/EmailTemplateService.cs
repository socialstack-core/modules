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
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Api.Translate;

namespace Api.Emails
{
	/// <summary>
	/// Handles emailTemplates.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class EmailTemplateService : AutoService<EmailTemplate>
	{
		/// <summary>
		/// The priority value used when adding an email event handler automatically.
		/// A high value essentially means everything else happens, then the email is sent.
		/// </summary>
		public const int EmailHandlerPriority = 1000;

		private readonly EmailConfig _configuration;

		private readonly CanvasRendererService _canvasRendererService;

		private readonly UserService _users;

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public EmailTemplateService(CanvasRendererService canvasRendererService, UserService users) : base(Events.EmailTemplate)
		{
			_users = users;
			_canvasRendererService = canvasRendererService;
			_configuration = GetConfig<EmailConfig>();
			
			InstallAdminPages("Emails", "fa:fa-paper-plane", new string[] { "id", "name", "key" });

			Events.User.BeforeSettable.AddEventListener((Context context, JsonField<User, uint> field) =>
			{
				if (field == null)
				{
					return new ValueTask<JsonField<User, uint>>(field);
				}
				
				if(field.Name == "EmailOptOutFlags")
				{
					// This field isn't settable
					field = null;
				}

				return new ValueTask<JsonField<User, uint>>(field);
			});

			Events.EmailTemplate.BeforeCreate.AddEventListener((Context context, EmailTemplate template) =>
			{
				if (string.IsNullOrEmpty(template.Key))
				{
					throw new PublicException("A key is required", "email_key_required");
				}

				return new ValueTask<EmailTemplate>(template);
			});

			Events.EmailTemplate.BeforeUpdate.AddEventListener((Context context, EmailTemplate template, EmailTemplate original) =>
			{
				if (string.IsNullOrEmpty(template.Key))
				{
					throw new PublicException("A key is required", "email_key_required");
				}

				return new ValueTask<EmailTemplate>(template);
			});

			InstallEmails(
				new EmailTemplate(){
					Name = "Verify email address",
					Subject = "Verify your email address",
					Key = "verify_email",
					BodyJson = "{\"module\":\"Email/Default\",\"content\":[{\"module\":\"Email/Centered\",\"data\":{}," +
					"\"content\":\"An account was recently created with us. If this was you, click the following link to proceed:\"},"+
					"{\"module\":\"Email/PrimaryButton\",\"data\":{\"label\":\"Verify my email address\",\"target\":\"/email-verify/${customData.userId}/${customData.token}\"}}]}"
				},
				new EmailTemplate()
                {
					Name = "Password reset",
					Subject = "Password reset",
					Key = "forgot_password",
					BodyJson = "{\"module\":\"Email/Default\",\"content\":[{\"module\":\"Email/Centered\",\"data\":{}," +
					"\"content\":\"A password reset request was recently created with us for this email. If this was you, click the following link to proceed:\"}," +
					"{\"module\":\"Email/PrimaryButton\",\"data\":{\"label\":\"Verify my email address\",\"target\":\"/password/reset/${customData.token}\"}}]}"
				},
				new EmailTemplate()
				{
					Name = "Welcome",
					Subject = "Welcome aboard!",
					Key = "welcome_member_email",
					BodyJson = "{\"module\":\"Email/Default\",\"content\":[{\"module\":\"Email/Centered\",\"data\":{}," +
					"\"content\":\"Thanks for joining! If you have any questions please reach out.\"}]}"
				}
			);
			
			var userConfig = _users.GetConfig<UserServiceConfig>();
			
			Events.User.AfterCreate.AddEventListener(async (Context ctx, User user) => {
				
				if(user == null){
					return user;
				}
				
				if(user.Role == Roles.Guest.Id && userConfig.VerifyEmails)
				{
					var token = await _users.SendVerificationEmail(ctx, user);
					user.EmailVerifyToken = token;
				} 
				//sending to regular members only
				else if(user.Role == Roles.Member.Id && userConfig.SendWelcomeEmail)
				{
					Send(new List<Recipient>()
					{
						new(user)
					}, "welcome_member_email");
				}
				
				return user;
			});
			
			Events.User.BeforeCreate.AddEventListener(async (Context ctx, User user) =>
			{
				if (user == null)
				{
					return user;
				}
				
				if (userConfig.UniqueEmails && !string.IsNullOrEmpty(user.Email))
				{
					// Let's make sure the username is not in use.
					var usersWithEmail = await _users.Where("Email=?", DataOptions.IgnorePermissions).Bind(user.Email).Any(ctx);

					if (usersWithEmail)
					{
						throw new PublicException(userConfig.UniqueEmailMessage, "email_used");
					}
				}

				if (!string.IsNullOrEmpty(user.Email))
                {
					user.LowerCaseEmail = user.Email.ToLower();
				}

				return user;
			});
			
			Events.User.BeforeUpdate.AddEventListener(async (Context ctx, User user, User orig) =>
			{
				if (user == null)
                {
					return user;
                }
				
				if (userConfig.UniqueEmails && !string.IsNullOrEmpty(user.Email) && user.Email != orig.Email)
				{		
					// Let's make sure the username is not in use by anyone besides this user (in case they didn't change it!).
					var usersWithEmail = await _users.Where("Email=? and Id!=?", DataOptions.IgnorePermissions).Bind(user.Email).Bind(user.Id).Any(ctx);

					if (usersWithEmail)
					{
						throw new PublicException(userConfig.UniqueEmailMessage, "email_used");
					}
				}

				if (user.Email != orig.Email)
				{
					user.LowerCaseEmail = user.Email == null ? null : user.Email.ToLower();
				}

				return user;
			});
			
			// Cache all in memory:
			Cache();
		}

		/// <summary>
		/// Gets an email template by its key.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="key"></param>
		/// <returns></returns>
		public async ValueTask<EmailTemplate> GetByKey(Context context, string key)
		{
			return await Where("Key=?", DataOptions.IgnorePermissions).Bind(key).First(context);
		}

		/// <summary>
		/// Renders an email with the given key using the given recipient info.
		/// </summary>
		/// <param name="key">Template key to use.</param>
		/// <param name="recipient">Mainly used for localisation. The end user's context.</param>
		/// <returns></returns>
		public async ValueTask<RenderedCanvas> Render(string key, Recipient recipient)
		{
			var template = await GetByKey(recipient.Context, key);

			if (template == null || recipient == null)
			{
				return new RenderedCanvas() {
					Body = null
				};
			}

			// Render the template now:
			var state = "{\"po\": " + Newtonsoft.Json.JsonConvert.SerializeObject(recipient.CustomData, jsonSettings) + "}";

			return await _canvasRendererService.Render(recipient.Context, template.BodyJson, state);
		}

		private readonly JsonSerializerSettings jsonSettings = new JsonSerializerSettings
		{
			ContractResolver = new DefaultContractResolver
			{
				NamingStrategy = new CamelCaseNamingStrategy()
			},
			Formatting = Formatting.None
		};

		/// <summary>
		/// Installs a template (Creates it if it doesn't already exist).
		/// </summary>
		public async ValueTask InstallNow(EmailTemplate template)
		{
			var context = new Context();

			// Match by target URL of the item.
			var existingEntry = await Where("Key=?", DataOptions.NoCacheIgnorePermissions).Bind(template.Key).ListAll(context);

			if (existingEntry.Count == 0)
			{
				await Create(context, template, DataOptions.IgnorePermissions);
			}
		}
		
		/// <summary>
		/// Ensures each recipient instance has a User loaded, and that it's also set into the CustomData.
		/// Note that we don't support sending to emails only, as a user is required to be able to track opt-out state.
		/// </summary>
		/// <param name="recipients"></param>
		private async ValueTask LoadUsers(IList<Recipient> recipients)
		{
			List<uint> idsToLoad = null;

			foreach (var recipient in recipients)
			{
				if (recipient == null)
				{
					continue;
				}

				if (recipient.UserId != 0 && recipient.User == null)
				{
					// Load this user:
					if (idsToLoad == null)
					{
						idsToLoad = new List<uint>();
					}

					idsToLoad.Add(recipient.UserId);
				}
			}

			if (idsToLoad != null)
			{
				var loadedUsers = await _users.Where("Id=[?]", DataOptions.IgnorePermissions).Bind(idsToLoad).ListAll(new Context());

				if (loadedUsers != null && loadedUsers.Count > 0)
				{
					// Create lookup:
					var userLookup = new Dictionary<uint, User>();
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
								recipient.Context.User = recipient.User;
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
		/// <param name="messageId"></param>
		/// <param name="attachments"></param>
		public void Send(IList<Recipient> recipients, string key, string messageId = null, IEnumerable<Attachment> attachments = null)
		{
			Task.Run(async () =>
			{
				try
				{
					await SendAsync(recipients, key, messageId, attachments);
				}
				catch (Exception e)
				{
					Log.Error(LogTag, e, "Failed sending an email.");
					throw;
				}
			});
		}

		/// <summary>
		/// Sends the given email to the given list of recipients.
		/// </summary>
		/// <param name="recipients"></param>
		/// <param name="key"></param>
		/// <param name="messageId"></param>
		/// <param name="attachments">Optional attachments.</param>
		/// <returns></returns>
		public async Task<bool> SendAsync(IList<Recipient> recipients, string key, string messageId = null, IEnumerable<Attachment> attachments = null)
		{
			// First, make sure we have users loaded for all recipients.
			await LoadUsers(recipients);

			// Next, group recipients by locale:
			// This is because each locale effectively has a different template object.
			var recipientsByLocale = new Dictionary<uint, TemplateAndRecipientSet>();

			foreach (var recipient in recipients)
			{
				if (recipient == null || (recipient.EmailAddress == null && (recipient.User == null || recipient.User.Email == null)) || recipient.Context == null)
				{
					continue;
				}

				// Locale is either from the Context, or the user's last locale.
				var localeId = recipient.Context.LocaleId;

				if (localeId <= 0)
				{
					localeId = recipient.User.LocaleId ?? 1;
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
					var template = await GetByKey(new Context(localeId, 0, Roles.Developer.Id), key);
					
					if(template == null){
						throw new Exception("Email template with key '" + key + "' doesn't exist.");
					}
					
					set.Template = template;

					// Add it:
					recipientsByLocale[localeId] = set;
				}

				set.Recipients.Add(recipient);
			}

			// For each locale block, render the emails:
			foreach (var localeKvp in recipientsByLocale)
			{
				// Get the set of canvas + all the contexts:
				var set = localeKvp.Value;

				// Email subject:
				var subject = set.Template?.Subject;

				// For each recipient, render it.
				for (var i=0;i<set.Recipients.Count;i++)
				{
					var recipient = set.Recipients[i];

					// Render all. The results are in the exact same order as the recipients set.
					var state = "{\"po\": " + Newtonsoft.Json.JsonConvert.SerializeObject(recipient.CustomData, jsonSettings) + "}";

					var renderedResult = await _canvasRendererService.Render(recipient.Context, set.Template.BodyJson, state);

					// Email to send to:
					var targetEmail = recipient.EmailAddress == null ? recipient.User.Email : recipient.EmailAddress;

					// Send now:
					await Send(targetEmail, subject, renderedResult.Body, messageId, null, attachments);
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
		/// <param name="messageId">Optional message ID.</param>
		/// <param name="fromAccount">Optionally select a particular from account. The default (in your appsettings.json) is used otherwise.</param>
		/// <param name="attachments">Optional attachments.</param>
		/// <param name="additionalHeaders">Optional headers. E.g. put Reply-To in here.</param>
		public async Task Send(string toAddress, string subject, string body, string messageId = null, EmailAccount fromAccount = null, IEnumerable<Attachment> attachments = null, Dictionary<string, string> additionalHeaders = null)
		{
			if (fromAccount == null)
			{
				fromAccount = _configuration.Accounts["default"];
			}

			SmtpClient client = new SmtpClient(fromAccount.Server)
			{
				UseDefaultCredentials = false,
				Credentials = new NetworkCredential(fromAccount.User, fromAccount.Password),
				Port = fromAccount.Port,
				EnableSsl = fromAccount.Encrypted
			};

			MailMessage mailMessage = new MailMessage
			{
				IsBodyHtml = true
			};

			if (!string.IsNullOrEmpty(messageId))
			{
				// Got a message ID:
				mailMessage.Headers.Add("Message-Id", messageId);
			}

			if (additionalHeaders != null)
			{
				foreach (var header in additionalHeaders)
				{
					if (header.Key == "Reply-To")
					{
						mailMessage.ReplyToList.Add(header.Value);
					}
					mailMessage.Headers.Add(header.Key, header.Value);
				}
			}

			mailMessage.DeliveryNotificationOptions = DeliveryNotificationOptions.OnSuccess | DeliveryNotificationOptions.OnFailure | DeliveryNotificationOptions.Delay;
			mailMessage.From = new MailAddress(fromAccount.FromAddress);
			mailMessage.To.Add(toAddress);
			mailMessage.Body = body;

			if (attachments != null)
			{
				foreach (var attachment in attachments)
				{
					// Add attachment:
					mailMessage.Attachments.Add(attachment);
				}
			}

			if (!string.IsNullOrEmpty(fromAccount.ReplyTo))
			{
				mailMessage.ReplyToList.Add(fromAccount.ReplyTo);
			}

			mailMessage.Subject = subject;

			client.SendCompleted += (object sender, AsyncCompletedEventArgs e) =>
			{
				if (e.Cancelled)
				{
					Log.Info(LogTag, "Email send cancelled.");
				}
				if (e.Error != null)
				{
					Log.Error(LogTag, e.Error, "Failed sending");
				}
				else
				{
					Log.Info(LogTag, "Email sent successfully");
				}
			};

			await client.SendMailAsync(mailMessage);

		}

	}

	/// <summary>
	/// A pairing of a template and a block of recipients to send it to.
	/// </summary>
	public class TemplateAndRecipientSet
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
