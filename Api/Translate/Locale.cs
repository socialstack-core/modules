using Api.AutoForms;
using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Users;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Translate
{
	/// <summary>
	/// </summary>
	public partial class Locale : VersionedContent<uint>
	{
		/// <summary>
		/// The name.
		/// </summary>
		[Data("hint", "The name of the locale")]
		[Data("required", true)]
		[Data("validate", "Required")]
		public Localized<string> Name;

		/// <summary>
		/// Usually a 5 letter locale code e.g. "en_GB". May also be just 2 e.g. "fr".
		/// </summary>
		[Data("hint", "The primary locale code, usually a 5 letter locale code e.g. 'en_GB'. May also be just 2 e.g. 'fr' or client specific such as 'en-en'")]
		[Data("required", true)]
		[Data("validate", "Required")]
		public string Code;

		/// <summary>
		/// Associated flag image representing the locale.
		/// </summary>
		[Data("hint", "Associated flag image representing the locale")]
		public string FlagIconRef;

		/// <summary>
		/// List of comma seperated aliases for mapping request headers or custom client codes
		/// </summary>
		[Data("hint", "List of comma seperated aliases for mapping request headers or custom client codes")]
		public string Aliases;

		/// <summary>
		/// Indicates this locale goes primarily right to left, such as Hebrew or Arabic.
		/// </summary>
		[Data("hint", "Set this if the locale goes right to left, such as Hebrew or Arabic")]
		public bool RightToLeft;

		/// <summary>
		/// If this locale is in use, the page URL to lookup will be prefixed with this value, except on the admin panel.
		/// Must start with /. For example, "/en-us". This causes a regional variant of the entire website to exist; 
		/// do note that it does not clone permalinks etc however.
		/// Different page sets is generally tidier than translating pages.
		/// </summary>
		[Data("hint", "Lowercase . A path in the page tree to use for this locale, e.g. 'en-us'. Creates a regional variant of the entire website.")]
		public string UrlPrefix;

		/// <summary>
		/// Used by sites with localised domains. A comma separated list of domain names with optional ports.
		/// </summary>
		[Data("hint", "List of comma seperated domain names with optional ports e.g. 'www.mysite.com,www.mysite.co.uk'. Overrides all other locale indicators when used")]
		public string Domains;


		[DatabaseField(Ignore = true)]
		private string _shortCode;

		/// <summary>
		/// If the code is e.g. en-GB, this is just en. It is internally cached for speed as well.
		/// </summary>
		[JsonIgnore]
		public string ShortCode {
			get{
				if (_shortCode == null)
				{
					if (string.IsNullOrEmpty(Code))
					{
						return Code;
					}

					// handle underscore or dash.
					var index = Code.IndexOf('-');
					if (index != -1)
					{
						_shortCode = Code.Substring(0, index);
					}
					else
					{
						index = Code.IndexOf('_');

						if (index != -1)
						{
							_shortCode = Code.Substring(0, index);
						}
						else
						{
							_shortCode = Code;
						}
					}
				}

				return _shortCode;
			}
		}

		/// <summary>
		/// Initialises the essential locales set used by the database engine for the determination of locale codes.
		/// Database engines call this during their startup routine.
		/// </summary>
		/// <param name="context"></param>
		/// <returns></returns>
		public static async ValueTask InitialiseLocaleList(Context context)
		{
			List<Locale> locales = null;
			locales = await Events.Locale.InitialList.Dispatch(context, locales);

			// Find the max ID:
			var maxId = locales.Max(locale => locale.Id);

			var localeLookup = new Locale[maxId];

			foreach (var locale in locales)
			{
				localeLookup[locale.Id - 1] = locale;
			}

			// Set the available locales:
			ContentTypes.Locales = localeLookup;
		}
	}

}
