using Api.Configuration;
using System;
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
		/// Site logo ref (admin area). Typically a white version of the logo.
		/// </summary>
		public string AdminLogoRef { get; set; }
		
		/// <summary>
		/// Default theme key. This is applied to the body if it is not null.
		/// </summary>
		public string DefaultThemeId { get; set; }

		/// <summary>
		/// Default theme ID. This is applied to the body if it is non-zero.
		/// </summary>
		public string DefaultAdminThemeId { get; set; }
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
		public string Css { get; set; }
	}

}