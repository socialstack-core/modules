using Api.Configuration;
using System.Collections.Generic;


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
		/// The base palette. This is usually used as the source of colours for other variables.
		/// </summary>
		public ThemePalette Palette { get; set; } = new ThemePalette();

	}

	/// <summary>
	/// Palette within the theme config. Very closely based on Bootstrap 5 color palette options.
	/// </summary>
	public partial class ThemePalette
	{
		/// <summary>
		/// Primary col
		/// </summary>
		public string Primary { get; set; } = "#007bff";

		/// <summary>
		/// Secondary col
		/// </summary>
		public string Secondary { get; set; } = "#6c757d";

		/// <summary>
		/// Tertiary col
		/// </summary>
		public string Tertiary { get; set; } = "#6c757d";

		/// <summary>
		/// Success col
		/// </summary>
		public string Success { get; set; } = "#28a745";

		/// <summary>
		/// Info col
		/// </summary>
		public string Info { get; set; } = "#17a2b8";

		/// <summary>
		/// Warning col
		/// </summary>
		public string Warning { get; set; } = "#ffc107";

		/// <summary>
		/// Danger col
		/// </summary>
		public string Danger { get; set; } = "#dc3545";

		/// <summary>
		/// Light col
		/// </summary>
		public string Light { get; set; } = "#f8f9fa";

		/// <summary>
		/// Dark col
		/// </summary>
		public string Dark { get; set; } = "#343a40";

	}

}