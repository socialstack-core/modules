using Api.CanvasRenderer;
using Api.Configuration;
using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Pages;
using Api.Permissions;
using Api.SocketServerLibrary;
using Api.Startup;
using Api.Templates;
using Api.Translate;
using Api.Users;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

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

		private readonly ConfigurationService _configurationService;

		private readonly UserService _users;

		private readonly PageService _pages;

		private readonly HtmlService _html;

		private readonly ConcurrentDictionary<uint, EmailCanvasGenerator> _generators = new ConcurrentDictionary<uint, EmailCanvasGenerator>();

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public EmailTemplateService(HtmlService html, CanvasRendererService canvasRendererService, UserService users, RoleService roles, PageService pages, TemplateService templates, ConfigurationService configService) : base(Events.EmailTemplate)
		{
			_users = users;
			_pages = pages;
			_html = html;
			_configurationService = configService;
			_canvasRendererService = canvasRendererService;
			_configuration = GetConfig<EmailConfig>();

			InstallAdminPages(new AdminPageOptions()
			{
				NavMenuLabel = new Localized<string>("Email Templates"),
				NavMenuIcon = "fa:fa-paper-plane",
				ListFields = ["id", "name", "key"],
				NavMenuParentKey = "content_management",
				Tabs = [
					new AdminTab("Design", "design"),
					new AdminTab("Details", "details")
				]
			});

			// Install the base email template.
			templates.InstallContent(new TemplateBuilder()
			{
				Title = "Email default",
				Key = "email_default",
				TemplateType = TemplateType.Email,
				BuildBody = (TemplateBuilder builder) => {
					return new CanvasNode("Email/Templates/BaseEmailTemplate")
						.AddRoot("children",
							new CanvasNode("Admin/Template/Slot")
								.With("name", "body")
						);
				}
			});

			Events.Page.BeforePageInstall.AddEventListener((ctx, pageBuilder) =>
			{
				if (
					pageBuilder.IsAdmin &&
					pageBuilder.ContentType == typeof(EmailTemplate) &&
					pageBuilder.PageType == CommonPageType.AdminAdd
				)
				{
					pageBuilder.Body.Roots["body"] = new CanvasNode()
					{
						Module = "Admin/Email/Create"
					};
				}

				return ValueTask.FromResult(pageBuilder);
			});

			Events.User.BeforeSettable.AddEventListener((Context context, JsonField<User, uint> field) =>
			{
				if (field == null)
				{
					return new ValueTask<JsonField<User, uint>>(field);
				}

				if (field.Name == "EmailOptOutFlags")
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

			Events.EmailTemplate.Send.AddEventListener(async (Context context, EmailToSend toSend) =>
			{

				if (toSend.Handled)
				{
					return toSend;
				}

				if (toSend.FromAccount == null || string.IsNullOrEmpty(toSend.FromAccount.Server) || toSend.FromAccount.Port == 0)
				{
					throw new PublicException("This site is currently unable to send emails as it has not been configured", "email/not-configured");
				}

				SmtpClient client = new SmtpClient(toSend.FromAccount.Server)
				{
					UseDefaultCredentials = false,
					Credentials = new NetworkCredential(toSend.FromAccount.User, toSend.FromAccount.Password),
					Port = toSend.FromAccount.Port,
					EnableSsl = toSend.FromAccount.Encrypted
				};

				MailMessage mailMessage = new MailMessage
				{
					IsBodyHtml = true
				};

				if (!string.IsNullOrEmpty(toSend.MessageId))
				{
					// Got a message ID:
					mailMessage.Headers.Add("Message-Id", toSend.MessageId);
				}

				if (toSend.AdditionalHeaders != null)
				{
					foreach (var header in toSend.AdditionalHeaders)
					{
						if (header.Key == "Reply-To")
						{
							mailMessage.ReplyToList.Add(header.Value);
						}
						mailMessage.Headers.Add(header.Key, header.Value);
					}
				}

				mailMessage.DeliveryNotificationOptions = DeliveryNotificationOptions.OnSuccess | DeliveryNotificationOptions.OnFailure | DeliveryNotificationOptions.Delay;
				mailMessage.From = new MailAddress(toSend.FromAccount.FromAddress);
				mailMessage.To.Add(toSend.ToAddress);
				mailMessage.Body = toSend.Body;

				if (toSend.Attachments != null)
				{
					foreach (var attachment in toSend.Attachments)
					{
						// Add attachment:
						mailMessage.Attachments.Add(attachment);
					}
				}

				if (!string.IsNullOrEmpty(toSend.FromAccount.ReplyTo))
				{
					mailMessage.ReplyToList.Add(toSend.FromAccount.ReplyTo);
				}

				mailMessage.Subject = toSend.Subject;

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

				toSend.Handled = true;
				return toSend;
			});

			_pages.Install(new PageBuilder()
			{
				Url = "/en-admin/email/test",
				Key = "admin_email_test",
				Title = "Send a test email",
				BuildBody = (PageBuilder builder) =>
				{
					return builder.AddTemplate(
						new CanvasNode("Admin/Tile")
							.With("className", "email-test")
							.With("title", "Email Test")
							.AppendChild(
								new CanvasNode("Admin/Email/EmailTest")
							)
					);
				}
			});

			InstallEmails(
				new EmailBuilder()
				{
					Name = "Verify email address",
					Subject = "Verify your email address",
					Key = "verify_email",
					BuildBody = (EmailBuilder builder) =>
					{
						return builder.AddTemplate(
							new CanvasNode("Email/Centered")
							.AppendChild(
								"An account was recently created with us. If this was you, click the following link to proceed:"
							)
							.AppendChild(
								new CanvasNode("Email/PrimaryButton")
								.With("label", "Verify my email address")
								.With("target", "/email-verify/${customData.userId}/${customData.token}")
							)
						);
					}
				},
				new EmailBuilder()
				{
					Name = "Password reset",
					Subject = "Password reset",
					Key = "forgot_password",
					PrimaryContentType = "PasswordResetRequest",
					BuildBody = (EmailBuilder builder) =>
					{
						return builder.AddTemplate(
							new CanvasNode("Email/Centered")
							.AppendChild(
								"A password reset request was recently created with us for this email. If this was you, click the following link to proceed:"
							)
							.AppendChild(
								new CanvasNode("Email/PrimaryButton")
								.With("label", "Reset my password")
								.With("target", "/password/reset/${customData.token}")
							)
						);
					}
				},
				new EmailBuilder()
				{
					Name = "Welcome",
					Subject = "Welcome aboard!",
					Key = "welcome_member_email",
					BuildBody = (EmailBuilder builder) =>
					{
						return builder.AddTemplate(
							new CanvasNode("Email/Centered")
							.AppendChild(
								"Thanks for joining! If you have any questions please reach out."
							)
						);
					}
				}
			);

			var userConfig = _users.GetConfig<UserServiceConfig>();

			Events.User.AfterCreate.AddEventListener(async (Context ctx, User user) =>
			{

				if (user == null)
				{
					return user;
				}

				var role = await roles.Get(ctx, user.Role); // (returns instantly)

				if ((user.Role == Roles.Guest.Id || (role != null && role.InheritedRoleId == Roles.Guest.Id)) && userConfig.VerifyEmails)
				{
					var token = await _users.SendVerificationEmail(ctx, user);
					user.EmailVerifyToken = token;
				}
				//sending to regular members only
				else if (user.Role == Roles.Member.Id && userConfig.SendWelcomeEmail)
				{
					Send(user, "welcome_member_email");
				}

				return user;
			}, 100);

			Events.User.BeforeCreate.AddEventListener(async (Context ctx, User user) =>
			{
				if (user == null)
				{
					return user;
				}

				if (userConfig.UniqueEmails && !string.IsNullOrEmpty(user.Email))
				{
					// Let's make sure the email address is not in use.
					var usersWithEmail = await _users.Where("Email=?", DataOptions.IgnorePermissions).Bind(user.Email).Any(ctx);

					if (usersWithEmail)
					{
						throw new PublicException(userConfig.UniqueEmailMessage, "email_used");
					}
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

				return user;
			});

#if !DEBUG
			Cache();
#endif
		}

		/// <summary>
		/// Generate the canvas JSON for the given template, factoring in locales and caching of the generator.
		/// </summary>
		/// <returns>The email generator itself for convenience.</returns>
		private async ValueTask<EmailCanvasGenerator> GenerateCanvas(Context context, EmailTemplate template, Writer writer, object primaryObject)
		{
			if (!_generators.TryGetValue(template.Id, out EmailCanvasGenerator generator))
			{
				generator = new EmailCanvasGenerator(template);
				if (!_generators.TryAdd(template.Id, generator))
				{
					_generators.TryGetValue(template.Id, out generator);
				}
			}

			await generator.Generate(context, template, writer, primaryObject);
			return generator;
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
			return await Render(template, recipient);
		}

		/// <summary>
		/// Renders an email with the given key using the given recipient info.
		/// </summary>
		/// <param name="template">Template to use.</param>
		/// <param name="recipient">Mainly used for localisation. The end user's context.</param>
		/// <returns></returns>
		public async ValueTask<RenderedCanvas> Render(EmailTemplate template, Recipient recipient)
		{
			if (template == null || recipient == null)
			{
				return new RenderedCanvas()
				{
					Body = null
				};
			}

			var po = recipient.CustomData;
			var context = recipient.Context;

			// Construct the state now:
			var writer = Writer.GetPooled();
			writer.Start(null);
			writer.WriteASCII("{");
			writer.WriteS(GetAvailableDomains());
			writer.WriteASCII("\"page\":{\"bodyJson\":");
			var bodyGenerator = await GenerateCanvas(context, template, writer, po);
			writer.Write((byte)'}');

			var cfgBytes = _configurationService.GetLatestFrontendConfigBytesJson();

			if (cfgBytes != null)
			{
				writer.WriteASCII(",\"config\":");
				writer.WriteNoLength(cfgBytes);
			}

			if (po != null)
			{
				writer.WriteASCII(",\"po\":");

				if (bodyGenerator != null && bodyGenerator.PrimaryContentService != null)
				{
					// It's a content type - use the proper serialiser
					await bodyGenerator.PrimaryContentService.ObjectToJson(
						context,
						po,
						writer,
						null,
						template.PrimaryContentIncludes,
						ContextFlags.IsPrimary | ContextFlags.IsEmail
					);
				}
				else
				{
					// Newtonsoft
					var poJsonStr = Newtonsoft.Json.JsonConvert.SerializeObject(po, jsonSettings);
					writer.WriteS(poJsonStr);
				}
			}

			writer.Write((byte)'}');
			var stateForSSR = writer.ToUTF8String();
			writer.Release();

			return await _canvasRendererService.Render(
				recipient.Context,
				null,
				stateForSSR
			);
		}

		private string _siteDomains;

		/// <summary>
		/// Get all the site domains for use in tokeniser and url links
		/// </summary>
		/// <returns></returns>
		private string GetAvailableDomains()
		{
			if (_siteDomains != null)
			{
				return _siteDomains;
			}

			_siteDomains = "";

			var domainService = Services.Get("SiteDomainService");
			if (domainService != null)
			{
				var getSiteDomains = domainService.GetType().GetMethod("GetSiteDomains");

				_siteDomains = getSiteDomains.Invoke(domainService, null).ToString();

				if (!string.IsNullOrWhiteSpace(_siteDomains))
				{
					_siteDomains = _siteDomains + ",";
				}
			}

			return _siteDomains;
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
		public async ValueTask InstallNow(EmailBuilder builder)
		{
			var context = new Context();

			// Match by target URL of the item.
			var existingEntry = await Where("Key=?", DataOptions.NoCacheIgnorePermissions).Bind(builder.Key).ListAll(context);

			if (existingEntry.Count != 0)
			{
				return;
			}

			// Start building:
			builder.Build();

			await Events.EmailTemplate.BeforeInstall.Dispatch(context, builder);
			builder.EmailTemplate.BodyJson = new Localized<JsonString>(new JsonString(builder.Body.ToJson()));

			await Create(context, builder.EmailTemplate, DataOptions.IgnorePermissions);
		}

		/// <summary>
		/// Ensures each recipient instance has a User loaded, and that it's also set into the CustomData.
		/// Note that we don't support sending to emails only, as a user is required to be able to track opt-out state.
		/// </summary>
		/// <param name="recipients"></param>
		private async ValueTask LoadUsers(IEnumerable<Recipient> recipients)
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

								if (recipient.User != null && recipient.Context.LocaleId == 0 && recipient.User.LocaleId.HasValue)
								{
									recipient.Context.LocaleId = recipient.User.LocaleId.GetValueOrDefault();

									// It can still be zero after this - the email generator handles that.
								}
							}
						}
					}
				}

			}

		}

		/// <summary>
		/// Sends emails to the given user without waiting for it to complete.
		/// </summary>
		/// <param name="recipient"></param>
		/// <param name="key"></param>
		/// <param name="messageId"></param>
		/// <param name="attachments"></param>
		public void Send(User recipient, string key, string messageId = null, IEnumerable<Attachment> attachments = null)
		{
			Send(new Recipient(recipient), key, messageId, attachments);
		}

		/// <summary>
		/// Sends emails to the given recipient without waiting for it to complete.
		/// </summary>
		/// <param name="recipient"></param>
		/// <param name="key"></param>
		/// <param name="messageId"></param>
		/// <param name="attachments"></param>
		public void Send(Recipient recipient, string key, string messageId = null, IEnumerable<Attachment> attachments = null)
		{
			Task.Run(async () =>
			{
				try
				{
					await SendAndWaitForSuccess(recipient, key, messageId, attachments);
				}
				catch (Exception e)
				{
					Log.Error(LogTag, e, "Failed sending an email.");
					throw;
				}
			});
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
					await SendAndWaitForSuccess(recipients, key, messageId, attachments);
				}
				catch (Exception e)
				{
					Log.Error(LogTag, e, "Failed sending an email.");
					throw;
				}
			});
		}

		/// <summary>
		/// Sends the given email to the given recipient. Generally avoid this: use send instead.
		/// </summary>
		/// <param name="recipient"></param>
		/// <param name="key"></param>
		/// <param name="messageId"></param>
		/// <param name="attachments">Optional attachments.</param>
		/// <returns></returns>
		public async Task<bool> SendAndWaitForSuccess(
			Recipient recipient,
			string key,
			string messageId = null,
			IEnumerable<Attachment> attachments = null
		)
		{
			return await SendAndWaitForSuccess(
				new List<Recipient>() { recipient },
				key,
				messageId,
				attachments
			);
		}

		/// <summary>
		/// Sends the given email to the given list of recipients, waiting for it to succeed. 
		/// Generally avoid this: use fire and forget Send instead.
		/// </summary>
		/// <param name="recipients"></param>
		/// <param name="key"></param>
		/// <param name="messageId"></param>
		/// <param name="attachments">Optional attachments.</param>
		/// <returns></returns>
		public async Task<bool> SendAndWaitForSuccess(
			IList<Recipient> recipients,
			string key,
			string messageId = null,
			IEnumerable<Attachment> attachments = null
		)
		{
			// Load the template itself:
			var template = await GetByKey(new Context(1, 0, 1), key);

			if (template == null)
			{
				throw new Exception("Invalid email template key provided: " + key);
			}


			// Make sure we have users loaded for all recipients.
			await LoadUsers(recipients);

			// For each one..
			foreach (var recipient in recipients)
			{
				if (recipient == null || (recipient.EmailAddress == null && (recipient.User == null || recipient.User.Email == null)) || recipient.Context == null)
				{
					continue;
				}

				var renderedResult = await Render(template, recipient);

				// Email to send to:
				var targetEmail = recipient.EmailAddress == null ? recipient.User.Email : recipient.EmailAddress;

				// Email subject:
				var resolvedSubject = template.Subject.Get(recipient.Context);

				// resolve any PrimaryContent tokens in the subject
				// CustomData must be a primary object type
				// e.g. For a purchase, "New order :: Reference - {Purchase.Reference}"
				if (recipient.CustomData != null && ContentTypes.IsContentType(recipient.CustomData.GetType()))
				{
					try
					{
						resolvedSubject = await _html.ReplaceTokens(recipient.Context, resolvedSubject, recipient.CustomData);
					}
					catch (Exception ex)
					{
						Log.Error(LogTag, ex, $"Failed to replace tokens in subject '{resolvedSubject}'");
					}
				}

				// Send now:
				await Send(targetEmail, resolvedSubject, renderedResult.Body, messageId, null, attachments);
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

			var toSend = new EmailToSend()
			{
				ToAddress = toAddress,
				Subject = subject,
				Body = body,
				MessageId = messageId,
				FromAccount = fromAccount,
				Attachments = attachments,
				AdditionalHeaders = additionalHeaders
			};

			await Events.EmailTemplate.Send.Dispatch(new Context(), toSend);
		}

	}

}
