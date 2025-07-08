using Api.AutoForms;
using Api.Translate;
using Api.Users;

namespace Api.SiteDomains
{
	
	/// <summary>
	/// A SiteDomain
	/// </summary>
	public partial class SiteDomain : VersionedContent<uint>
	{
        /// <summary>
        /// The name.
        /// </summary>
        [Localized]
        [Order(1)]
        [Data("hint", "The name of the site domain")]
        public string Name;

        /// <summary>
        /// Short code to identify the domain and prefix internal page urls (may be duplicates so also need to consider IsPrimary)
        /// </summary>
        [Order(2)]
        [Data("hint", "The domain prefix, a short value used when setting cross domain links within the site")]
        public string Code;

        /// <summary>
        /// Indicates if the domain is not currently available
        /// </summary>
        [Order(4)]
        [Data("hint", "Indicates if the domain is not currently available")]
        public bool IsDisabled;

        /// <summary>
        /// Indicates if this is the primary entry for the domain
        /// </summary>
        [Order(4)]
        [Data("hint", "Indicates if the domain is the primary one for mapping via prefix")]
        public bool IsPrimary;

        /// <summary>
        /// Indicates if this is the core/root domain for the site
        /// </summary>
        [Data("hint", "Indicates if the domain is the core/root one for the site")]
        [Order(4)]
        public bool IsRoot;

        /// <summary>
        /// Used by sites with localised domains. A comma separated list of domain names with optional ports.
        /// </summary>
        [Data("hint", "The domain name with optional ports e.g. 'www.mysite.com,www.mysite.co.uk'.")]
        [Order(3)]
        public string Domain;

    }

}