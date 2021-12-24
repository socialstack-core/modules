using Api.Translate;
using System.Threading.Tasks;
using Api.Startup;


namespace Api.Contexts
{
	public partial class Context
	{
		
		private static LocaleService _locales;
		
		/// <summary>
		/// Underlying locale ID.
		/// </summary>
		private uint _localeId = 1;
		
		/// <summary>
		/// The current locale or the site default.
		/// </summary>
		public uint LocaleId
		{
			get
			{
				return _localeId;
			}
			set
			{
				if (value == 0)
				{
					value = 1;
				}

				_locale = null;
				_localeId = value;
			}
		}

		/// <summary>
		/// The full locale object, if it has been requested.
		/// </summary>
		private Locale _locale;

		/// <summary>
		/// Gets the locale for this context.
		/// </summary>
		/// <returns></returns>
		public async ValueTask<Locale> GetLocale()
		{
			if (_locale != null)
			{
				return _locale;
			}

			if (_locales == null)
			{
				_locales = Services.Get<LocaleService>();
			}

			// Get the user now:
			_locale = await _locales.Get(this, LocaleId, DataOptions.IgnorePermissions);

			if (_locale == null)
			{
				// Dodgy locale in the cookie. Locale #1 always exists.
				return await _locales.Get(this, 1, DataOptions.IgnorePermissions);
			}

			return _locale;
		}
		
	}
}