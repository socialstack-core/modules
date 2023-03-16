using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using Newtonsoft.Json;
using Api.Users;
using Newtonsoft.Json.Serialization;
using Api.CanvasRenderer;
using System;
using Twilio;
using Twilio.Rest.Api.V2010.Account;


namespace Api.SmsMessages
{
	/// <summary>
	/// Handles smsMessages.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get. Uses Twilio to send content via SMS.
	/// </summary>
	public partial class SmsTemplateService : AutoService<SmsTemplate>
    {
		private UserService _users;
		private CanvasRendererService _canvasRendererService;
		private SmsConfig _configuration;


		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public SmsTemplateService(UserService users, CanvasRendererService canvasRenderer) : base(Events.SmsTemplate)
        {
			_users = users;
			_canvasRendererService = canvasRenderer;

			_configuration = GetConfig<SmsConfig>();

			InstallAdminPages("SMS Messages", "fa:fa-mobile", new string[] { "id", "subject", "key" });
		}
		
		/// <summary>
		/// Gets an SMS template by its key.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="key"></param>
		/// <returns></returns>
		public async ValueTask<SmsTemplate> GetByKey(Context context, string key)
		{
			return await Where("Key=?", DataOptions.IgnorePermissions).Bind(key).First(context);
		}

		/// <summary>
		/// Renders an SMS with the given key using the given recipient info.
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
			return await _canvasRendererService.Render(recipient.Context, template.BodyJson, new PageState() {
				Tokens = null,
				TokenNames = null,
				PrimaryObject = Newtonsoft.Json.JsonConvert.SerializeObject(recipient.CustomData, jsonSettings)
			});
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
		public async ValueTask InstallNow(SmsTemplate template)
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
		/// Note that we don't support sending to phone numbers only as a user is required to be able to track opt-out state.
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
		/// Sends SMS to the given recipients without waiting for it to complete.
		/// </summary>
		/// <param name="recipients"></param>
		/// <param name="key"></param>
		public void Send(IList<Recipient> recipients, string key)
		{
			Task.Run(async () =>
			{
				try
				{
					await SendAsync(recipients, key);
				}
				catch (Exception e)
				{
					Console.WriteLine(e.ToString());
					throw;
				}
			});
		}

		/// <summary>
		/// Sends the given SMS to the given list of recipients.
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
			var recipientsByLocale = new Dictionary<uint, TemplateAndRecipientSet>();

			foreach (var recipient in recipients)
			{
				if (recipient == null || (recipient.ContactNumber == null && (recipient.User == null || recipient.User.ContactNumber == null)) || recipient.Context == null)
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

					if (template == null)
					{
						throw new Exception("SMS template with key '" + key + "' doesn't exist.");
					}

					set.Template = template;

					// Add it:
					recipientsByLocale[localeId] = set;
				}

				set.Recipients.Add(recipient);
			}

			// For each locale block, render the SMS:
			foreach (var localeKvp in recipientsByLocale)
			{
				// Get the set of canvas + all the contexts:
				var set = localeKvp.Value;

				// For each recipient, render it.
				for (var i = 0; i < set.Recipients.Count; i++)
				{
					var recipient = set.Recipients[i];

					// Render all. The results are in the exact same order as the recipients set.
					var renderedResult = await _canvasRendererService.Render(recipient.Context, set.Template.BodyJson, new PageState()
					{
						PrimaryObject = Newtonsoft.Json.JsonConvert.SerializeObject(recipient.CustomData, jsonSettings)
					}, null, false, RenderMode.Text);

					// SMS number to send to:
					var targetNumber = recipient.ContactNumber == null ? recipient.User.ContactNumber : recipient.ContactNumber;

					// Send now:
					await Send(targetNumber, renderedResult.Body, null);
				}
			}

			return true;
		}

		private bool _init;
		private Twilio.Types.PhoneNumber _fromNumber;

		/// <summary>
		/// Direct sends an SMS to the given number. Doesn't block this thread.
		/// </summary>
		/// <param name="toNumber">Target phone number.</param>
		/// <param name="body">Sms body (text).</param>
		/// <param name="fromAccount">Optionally select a particular from account. The default (in your appsettings.json) is used otherwise.</param>
		public async Task Send(string toNumber, string body, SmsAccount fromAccount = null)
		{
			if (fromAccount == null)
			{
				fromAccount = _configuration.Accounts["default"];
			}

			if (!_init)
			{
				_init = true;
				_fromNumber = new Twilio.Types.PhoneNumber(fromAccount.PhoneNumber);
				TwilioClient.Init(fromAccount.TwilioSid, fromAccount.TwilioAuthToken);
			}

			await MessageResource.CreateAsync(new CreateMessageOptions(new Twilio.Types.PhoneNumber(toNumber)) {
				Body = body,
				From = _fromNumber
			});
			
		}

	}

	/// <summary>
	/// A pairing of a template and a block of recipients to send it to.
	/// </summary>
	public class TemplateAndRecipientSet
	{
		/// <summary>
		/// The SMS template to send.
		/// </summary>
		public SmsTemplate Template;

		/// <summary>
		/// The set of recipients that'll receive this template.
		/// </summary>
		public List<Recipient> Recipients = new List<Recipient>();
	}
}
