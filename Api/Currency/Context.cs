using Api.Translate;
using System.Threading.Tasks;
using Api.Startup;


namespace Api.Contexts
{
	public partial class Context
	{
		/// <summary>
		/// Underlying currency locale ID.
		/// </summary>
		private uint _currencylocaleId = 0;
		
		/// <summary>
		/// The current locale or the site default.
		/// </summary>
		public uint CurrencyLocaleId
		{
			get
			{
				return _currencylocaleId;
			}
			set
			{
				_currencylocale = null;
				_currencylocaleId = value;
			}
		}

		/// <summary>
		/// The full locale object, if it has been requested.
		/// </summary>
		private Locale _currencylocale;

		/// <summary>
		/// Gets the locale for this context.
		/// </summary>
		/// <returns></returns>
		public async ValueTask<Locale> GetCurrencyLocale()
		{
			if (_currencylocale != null)
			{
				return _currencylocale;
			}

			if (_locales == null)
			{
				_locales = Services.Get<LocaleService>();
			}

			// Get the user now:
			_currencylocale = await _locales.Get(this, CurrencyLocaleId, DataOptions.IgnorePermissions);

			return _currencylocale;
		}

		/// <summary>
		/// Set this to true to prevent conversion of prices when loading or listing an entity
		/// </summary>
		public bool DoNotConvertPrice = false;
		
	}
}