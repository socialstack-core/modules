using Api.AutoForms;

namespace Api.Translate
{
    /// <summary>
    /// </summary>
    public partial class Locale
    {
        /// <summary>
        /// Usually a 3 letter locale code e.g. "GBP".
        /// </summary>
        [Data("hint", "The currency code, usually a 3 letter locale code e.g. 'GBP'.")]
        public string CurrencyCode;
    }

}
