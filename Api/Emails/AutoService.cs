using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Permissions;
using Api.SocketServerLibrary;
using Api.Startup;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;


/// <summary>
/// The base class of all AutoService instances.
/// </summary>
public partial class AutoService
{
	/// <summary>
	/// Installs one or more email templates.
	/// Schedules the install to happen either immediately if services have not yet started (async) or after services have started.
	/// </summary>
	/// <param name="templates"></param>
	public void InstallEmails(params Api.Emails.EmailTemplate[] templates)
	{
		if (Services.Started)
		{
			InstallEmailsInternal(templates);
		}
		else
		{
			// Must happen after services start otherwise the email template service isn't necessarily ready yet.
			Events.Service.AfterStart.AddEventListener((Context ctx, object src) =>
			{
				InstallEmailsInternal(templates);
				return new ValueTask<object>(src);
			});
		}
	}

	private static void InstallEmailsInternal(Api.Emails.EmailTemplate[] templates)
	{
		var emailService = Services.Get<Api.Emails.EmailTemplateService>();

		if (emailService == null)
		{
			return;
		}

		Task.Run(async () =>
		{
			foreach (var template in templates)
			{
				await emailService.InstallNow(template);
			}
		});

	}
}
