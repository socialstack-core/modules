using Api.Configuration;
using System;

namespace Api.Themes
{
	
	/// <summary>
	/// Config for themes.
	/// The properties on this object directly influences the available fields in the admin panel. So, if you'd like additional theme config options, 
	/// just extend this class with additional properties - either via a partial class in a separate module, or right here.
	/// </summary>
	public partial class ThemeConfig : Config
	{
		/// <summary>
		/// Primary palette
		/// </summary>
		public ThemePalette Primary { get; set; }

		/// <summary>
		/// Secondary palette
		/// </summary>
		public ThemePalette Secondary { get; set; }

		/// <summary>
		/// Tertiary palette
		/// </summary>
		public ThemePalette Tertiary { get; set; }

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