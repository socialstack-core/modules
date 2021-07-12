using Api.Configuration;
using System;

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
		/// Default theme ID. This is applied to the body if it is non-zero.
		/// </summary>
		public uint DefaultThemeId { get; set; }

		/// <summary>
		/// Default theme ID. This is applied to the body if it is non-zero.
		/// </summary>
		public uint DefaultAdminThemeId { get; set; }
	}

	/// <summary>
	/// Config for themes.
	/// The properties on this object directly influences the available fields in the admin panel. So, if you'd like additional theme config options, 
	/// just extend this class with additional properties - either via a partial class in a separate module, or right here.
	/// </summary>
	public partial class ThemeConfig : Config
	{
		/// <summary>
		/// Set this if the current theme config is the dark mode variant of another theme config with the given ID.
		/// For example, let's say theme #1 is "Admin (light)" and theme #2 is "Admin (dark)"
		/// Theme #2 would have this field set to 1, indicating that it is the dark mode version of theme #1.
		/// </summary>
		public uint DarkModeOfThemeId { get; set; }

		/// <summary>
		/// Default palette, describing e.g. the main text/ bg colour of a component.
		/// </summary>
		public ThemePalette Default { get; set; }

		/// <summary>
		/// Primary palette, typically describing things that should stand out the most to the user.
		/// </summary>
		public ThemePalette Primary { get; set; }

		/// <summary>
		/// Secondary palette, typically describing things that are interactive but not as important as the primary ones.
		/// </summary>
		public ThemePalette Secondary { get; set; }

		/// <summary>
		/// Success palette
		/// </summary>
		public ThemePalette Success { get; set; }

		/// <summary>
		/// Info palette
		/// </summary>
		public ThemePalette Info { get; set; }

		/// <summary>
		/// Warning palette
		/// </summary>
		public ThemePalette Warning { get; set; }

		/// <summary>
		/// Danger palette
		/// </summary>
		public ThemePalette Danger { get; set; }

		/// <summary>
		/// Escape hatch custom CSS to apply when this theme is active. 
		/// You should always aim to keep this CSS minimal and generic - i.e. don't target specific components with this.
		/// </summary>
		public string CustomCss { get; set; }
	}

	/// <summary>
	/// A particular palette within a theme. 
	/// Note that the default (rest) styles are directly on the palette, because it is also a ThemeUnit.
	/// </summary>
	public partial class ThemePalette : ThemeUnit
	{
		/// <summary>
		/// Hover mode
		/// </summary>
		public ThemeUnit Hover { get; set; }

		/// <summary>
		/// Focus mode
		/// </summary>
		public ThemeUnit Focus { get; set; }

		/// <summary>
		/// Active mode
		/// </summary>
		public ThemeUnit Active { get; set; }
	}

	/// <summary>
	/// A particular unit within the theme config. This declares a set of common css properties that can be edited within other theme segments.
	/// </summary>
	public partial class ThemeUnit
	{
		/// <summary>
		/// Bg color
		/// </summary>
		public string Background { get; set; }
		/// <summary>
		/// Text color
		/// </summary>
		public string Color { get; set; }
		/// <summary>
		/// Border color
		/// </summary>
		public string Border { get; set; }
		/// <summary>
		/// Shadow color
		/// </summary>
		public string Shadow { get; set; }
	}
}