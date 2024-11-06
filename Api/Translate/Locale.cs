using Api.AutoForms;
using Api.Database;
using Api.Users;
using Newtonsoft.Json;

namespace Api.Translate
{
	/// <summary>
	/// </summary>
	public partial class Locale : VersionedContent<uint>
	{
		/// <summary>
		/// The name.
		/// </summary>
		[Localized]
		[Data("hint", "The name of the locale")]
		public string Name;

		/// <summary>
		/// Usually a 5 letter locale code e.g. "en_GB". May also be just 2 e.g. "fr".
		/// </summary>
		[Data("hint", "The primary locale code, usually a 5 letter locale code e.g. 'en_GB'. May also be just 2 e.g. 'fr' or client specific such as 'en-en'")]
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
		/// Indicates if the locale is not currently available
		/// </summary>
		[Data("hint", "Indicates if the locale is not currently available")]
		public bool isDisabled;

		/// <summary>
		/// Indicates if the locale should be redirected to root (e.g. /en-us/abc > /abc)
		/// </summary>
		[Data("hint", "Indicates if the locale should be redirected to root (e.g. /en-us/abc > /abc)")]
		public bool isRedirected;

		/// <summary>
		/// Indicates this locale goes primarily right to left, such as Hebrew or Arabic.
		/// </summary>
		[Data("hint", "Set this if the locale goes right to left, such as Hebrew or Arabic")]
		public bool RightToLeft;

		/// <summary>
		/// If this locale is in use, the page URL to lookup will be prefixed with this value, except on the admin panel.
		/// Should not contain any /. For example, can be "jersey". The frontend will never actually see this path; it is purely for creating different page sets per locale.
		/// Different page sets is generally tidier than translating pages.
		/// </summary>
		[Data("hint", "Path in the page tree to use for this locale. Not seen by the frontend; it simply creates a collection of pages for this locale specifically.")]
		public string PagePath;

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
	}

}
