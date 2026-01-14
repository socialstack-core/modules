using Newtonsoft.Json;

namespace Api.Payments
{
    /// <summary>
    /// Browser information collected from the client to support Strong Customer Authentication (3DS2).
    /// Matches the shape returned by the frontend JS helper (navigator, screen, timezone).
    /// </summary>
    public class BrowserInfo
    {
        /// <summary>
        /// Indicates if JavaScript is enabled in the browser.
        /// </summary>
        [JsonProperty("browserJavascriptEnabled")]
        public bool BrowserJavascriptEnabled { get; set; }

        /// <summary>
        /// The full browser user agent string as reported by the client (e.g., navigator.userAgent).
        /// </summary>
        [JsonProperty("browserUserAgent")]
        public string BrowserUserAgent { get; set; }

        /// <summary>
        /// The browser language in BCP 47 format (e.g., en-GB), typically from navigator.language.
        /// </summary>
        [JsonProperty("browserLanguage")]
        public string BrowserLanguage { get; set; }

        /// <summary>
        /// Indicates if Java is enabled in the browser.
        /// </summary>
        [JsonProperty("browserJavaEnabled")]
        public bool BrowserJavaEnabled { get; set; }

        /// <summary>
        /// The browser color depth in bits per pixel (commonly 8, 16, 24, or 32).
        /// Represented as a string to align with the Opayo API request model.
        /// </summary>
        [JsonProperty("browserColorDepth")]
        public string BrowserColorDepth { get; set; }

        /// <summary>
        /// The browser (screen) height in pixels.
        /// Represented as a string to align with the Opayo API request model.
        /// </summary>
        [JsonProperty("browserScreenHeight")]
        public string BrowserScreenHeight { get; set; }

        /// <summary>
        /// The browser (screen) width in pixels.
        /// Represented as a string to align with the Opayo API request model.
        /// </summary>
        [JsonProperty("browserScreenWidth")]
        public string BrowserScreenWidth { get; set; }

        /// <summary>
        /// The browser timezone offset from UTC (minutes or formatted string), e.g., "+300".
        /// Represented as a string to align with the Opayo API request model.
        /// </summary>
        [JsonProperty("browserTZ")]
        public string BrowserTZ { get; set; }
    }
}
