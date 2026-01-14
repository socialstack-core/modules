using Api.Configuration;
using System.Collections.Generic;

namespace Api.Themes
{

	/// <summary>
	/// Global theme configuration values, such as the site logo. Use themeService.GetConfig to access the current instance of this.
	/// </summary>
	[Frontend]
	public partial class GlobalThemeConfig : Config
	{
		/// <summary>
		/// Site logo ref.
		/// </summary>
		public string LogoRef { get; set; }

		/// <summary>
		/// Site logo ref (small version).
		/// </summary>
		public string SmallLogoRef { get; set; }

		/// <summary>
		/// Site logo ref (admin area). Typically a white version of the logo.
		/// </summary>
		public string AdminLogoRef { get; set; }
		
		/// <summary>
		/// Default theme key. This is applied to the body if it is not null.
		/// </summary>
		public string DefaultThemeId { get; set; } = "main";

		/// <summary>
		/// Default theme ID. This is applied to the body if it is non-zero.
		/// </summary>
		public string DefaultAdminThemeId { get; set; } = "admin";

		/// <summary>
		/// Site contact number.
		/// </summary>
		public string ContactNumber { get; set; } = "0808 189 2044";

		/// <summary>
		/// Site contact email address.
		/// </summary>
		public string ContactEmail { get; set; } = "info@4-roads.com";

		/// <summary>
		/// Site postal address.
		/// </summary>
		public string Address { get; set; } = "48 Priory Road<br />Kenilworth<br />Warwickshire<br />CV8 1LQ";

		/// <summary>
		/// Full site name.
		/// </summary>
		public string SiteName { get; set; } = "4 Roads (UK) Limited";

		/// <summary>
		/// Shortened site name.
		/// </summary>
		public string SiteNameShort { get; set; } = "4 Roads";

		/// <summary>
		/// Copyright statement. Supports 'year' and 'company' as templating variables.
		/// </summary>
		public string Copyright { get; set; } = "&copy; ${year} ${company}";

		/// <summary>
		/// Placeholder text for main site search field.
		/// </summary>
		public string SearchPlaceholder { get; set; } = "Search site";

		/// <summary>
		/// Localised theme config options.
		/// </summary>
		public GlobalLocalisedThemeConfig Localised { get; set; }
	}

	/// <summary>
	/// Global localised theme configuration values, such as the contact information.
	/// </summary>
	public partial class GlobalLocalisedThemeConfig : Config
	{
		/// <summary>
		/// Site logo ref.
		/// </summary>
		public Dictionary<string, string> LogoRef { get; set; }

		/// <summary>
		/// Site logo ref (small version).
		/// </summary>
		public Dictionary<string, string> SmallLogoRef { get; set; }

		/// <summary>
		/// Site logo ref (admin area). Typically a white version of the logo.
		/// </summary>
		public Dictionary<string, string> AdminLogoRef { get; set; }

		/// <summary>
		/// Site contact number.
		/// </summary>
		public Dictionary<string, string> ContactNumber { get; set; }

		/// <summary>
		/// Site postal address.
		/// </summary>
		public Dictionary<string, string> Address { get; set; }

		/// <summary>
		/// Full site name.
		/// </summary>
		public Dictionary<string, string> SiteName { get; set; }

		/// <summary>
		/// Shortened site name.
		/// </summary>
		public Dictionary<string, string> SiteNameShort { get; set; }

		/// <summary>
		/// Copyright statement. Supports 'year' and 'company' as templating variables.
		/// </summary>
		public Dictionary<string, string> Copyright { get; set; }

		/// <summary>
		/// Placeholder text for main site search field.
		/// </summary>
		public Dictionary<string, string> SearchPlaceholder { get; set; }
	}

	/// <summary>
	/// Config for themes.
	/// The properties on this object directly influences the available fields in the admin panel. So, if you'd like additional theme config options, 
	/// just extend this class with additional properties - either via a partial class in a separate module, or right here.
	/// </summary>
	public partial class ThemeConfig : Config
	{
		/// <summary>
		/// A key used to reference this particular theme config.
		/// </summary>
		public string Key {get; set;}
		
		/// <summary>
		/// Set this if the current theme config is the dark mode variant of another theme config with the given ID/ key.
		/// For example, let's say theme #1 is "Admin (light)" and theme #2 is "Admin (dark)"
		/// Theme #2 would have this field set to 1, indicating that it is the dark mode version of theme #1.
		/// </summary>
		public string DarkModeOfThemeId { get; set; }

		/// <summary>
		/// List of variables and their values.
		/// </summary>
		public Dictionary<string, string> Variables { get; set; }

		/// <summary>
		/// The CSS in this theme. Typically uses the variable values.
		/// </summary>
		public string Css { get; set; } = "";
	}

}