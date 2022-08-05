using Api.AutoForms;
using Api.Database;
using Api.Translate;
using Api.Users;
using Newtonsoft.Json;

namespace Api.Captchas
{
	
	/// <summary>
	/// A Captcha
	/// </summary>
	public partial class Captcha : VersionedContent<uint>
	{

        /// <summary>
        /// The description of the captcha test.
        /// </summary>
        [Data("hint", "Captcha description")]
        public string Description;

        /// <summary>
        /// The prompt for the user explaining the captcha test.
        /// </summary>
        [Data("hint", "The prompt displayed to the user, asking them to click on the required section/object")]
        [Data("type", "canvas")]
        [Localized]
        public string Prompt;

        /// <summary>
        /// The expected id/tag passed back to pass the captcha test.
        /// </summary>
        [Data("hint", "The id/tag of the path in the svg file expected to pass the test")]
        public string ExpectedTag;

        /// <summary>
        /// The captcha background image. The filetype comes from this.
        /// </summary>
        [Data("hint", "The background image for the captcha test.")]
        public string BackgroundRef;

        /// <summary>
        /// The captcha foreground svg image. The filetype comes from this.
        /// </summary>
        [Data("hint", "The foreground svg image containing the 'paths' to check for")]
        public string ForegroundRef;

        /// <summary>
        /// Is this captcha active (not pending/draft)
        /// </summary>
        [Data("hint", "Is this an active captcha.")]
        public bool IsActive;

        /// <summary>
        /// Show the current id/region for debugging
        /// </summary>
        [Data("hint", "Show the current id/region for debugging.")]
        public bool ShowDebug;

    }

}