using Api.Contexts;
using Api.Eventing;
using Api.Pages;
using Api.Startup;
using Api.Translate;
using System.Threading.Tasks;

namespace Api.UrlLocale;

/// <summary>
/// Sets up locale load from URLs.
/// </summary>
[EventListener]
public class InitUrlLocales
{
	
	/// <summary>
	/// Constructed automatically by startup system.
	/// </summary>
	public InitUrlLocales()
	{
		LocaleService localeService = null;

		Events.Page.BeforeParseUrl.AddEventListener(async (Context context, UrlInfo urlInfo, Microsoft.AspNetCore.Http.QueryString qs) =>
		{
			// The URL given here has been partially tidied up, meaning the first/last character is never a / or spaces.

			// Assuming 4 char ISO e.g. /gb-en/ at start of URL. Length must therefore be at least 5

			if (urlInfo.Length < 5)
			{
				// Homepage etc
				return urlInfo;
			}

			// First, check if 6th char is a fwd slash or end of string, and the 3rd is a dash:
			if (!(urlInfo.Length == 5 || urlInfo.Url[urlInfo.Start + 5] == '/') || urlInfo.Url[urlInfo.Start+2] != '-')
			{
				return urlInfo;
			}

			// The URL starts with ??-?? and we can therefore assume with very high probability that it is a locale code that we're interested in.
			// let's grab it:
			var code = urlInfo.LowercaseSubstring(0, 5);

			// Get locale:
			if (localeService == null)
			{
				localeService = Services.Get<LocaleService>();
			}
			
			var localeId = await localeService.GetId(code);

			if (localeId.HasValue)
			{
				// This URL starts with a locale code!
				// Chop it off the front and update the context too.
				if(urlInfo.Length == 5)
				{
					// Homepage - the URL is literally only the code
					urlInfo.Start += 5;
					urlInfo.Length -= 5;
				}
				else
				{
					// Code and a fwdslash - chop off the slash too
					urlInfo.Start += 6;
					urlInfo.Length -= 6;
				}

				context.LocaleId = localeId.Value;
			}

			return urlInfo;
		});

	}
	
}