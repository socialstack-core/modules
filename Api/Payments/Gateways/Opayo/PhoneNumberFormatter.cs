using Api.Startup;
using System.Text.RegularExpressions;

namespace Api.Payments.Opayo
{
    /// <summary>
    /// Helper to format telephone numbers to the format required by Opayo:
    /// "+" + country code + national significant number, no spaces or punctuation.
    /// Example (UK): input "07234 567891" -> "+447234567891".
    /// </summary>
    public static class PhoneNumberFormatter
    {
        private static readonly Regex NonDigit = new Regex("[^0-9]", RegexOptions.Compiled);

		/// <summary>
		/// Formats a phone number into "+{countryCode}{nsn}" as required by Opayo.
		/// <param name="raw">User-entered phone number (may include spaces, dashes, parentheses, etc.).</param>
		/// <param name="countryCode">Numeric country code</param>
		/// <returns>Formatted phone number string (e.g., "+447234567891").</returns>
		/// <remarks>
		/// Rules:
		/// - Strip all non-digits.
		/// - If the string starts with the country code already, ensure it is prefixed by "+".
		/// - If the string starts with a leading 0 (national trunk prefix), remove the leading 0 and prefix the country code.
		/// - Otherwise, if it doesn't start with the country code, prefix the country code.
		/// - No spaces or punctuation in the output.
		/// </remarks>
		/// </summary>
		public static string Format(string raw, string countryCode)
        {
			if (string.IsNullOrWhiteSpace(raw))
			{
				throw new PublicException("Phone number is required", "purchase_phone_missing");
			}

			if (string.IsNullOrWhiteSpace(countryCode) || !Regex.IsMatch(countryCode, "^\\d+$"))
			{
				throw new PublicException("Country code must be numeric (e.g., '44')", "purchase_phone_countrycode");
			}

            // Remove all non-digits
            var digits = NonDigit.Replace(raw, string.Empty);
			if (string.IsNullOrEmpty(digits))
			{
				throw new PublicException("Phone number must contain digits", "purchase_phone_digits");
			}

            // If digits already start with the country code
            if (digits.StartsWith(countryCode))
            {
                return "+" + digits;
            }

			// If local format with leading trunk '0', drop it and add country code
			if (digits.StartsWith("0"))
			{
				var nsn = digits.TrimStart('0');
				if (string.IsNullOrEmpty(nsn)) { 
					throw new PublicException("Invalid local phone number after removing trunk prefix", "purchase_phone_prefix");
				}
                return "+" + countryCode + nsn;
            }

            // Otherwise, just prefix the country code
            return "+" + countryCode + digits;
        }

        /// <summary>
        /// Convenience method for UK numbers (country code 44).
        /// Example: "07234 567891" -> "+447234567891".
        /// </summary>
        public static string FormatUk(string raw) => Format(raw, "44");
    }
}
