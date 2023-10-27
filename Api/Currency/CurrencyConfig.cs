using Api.Configuration;

namespace Api.Currency
{
    /// <summary>
    /// Configuration for the currency mechanism
    /// </summary>
    public class CurrencyConfig : Config
    {
        /// <summary>
        /// Currency config to round to
        /// </summary>
        public int RoundTo {get; set;}
    }
}