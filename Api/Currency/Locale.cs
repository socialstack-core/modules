using Api.AutoForms;
using Api.Users;


namespace Api.Translate
{
    /// <summary>
    /// </summary>
    public partial class Locale
	{
		/// <summary>
		/// Usually a 1 character symbol e.g. "£" or "$"
		/// </summary>
		[Data("hint", "The currency character for this locale, e.g. £ or $")]
		public string CurrencySymbol;
	}

}
