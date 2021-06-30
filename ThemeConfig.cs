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
		/// The base light palette. This is usually used as the source of colours for other variables.
		/// </summary>
		public ThemePalette LightPalette { get; set; } = new ThemePalette(
			background: "#ffffff",
			foreground: "#000000"
			);

		/// <summary>
		/// The base dark palette. This is usually used as the source of colours for other variables.
		/// </summary>
		public ThemePalette DarkPalette { get; set; } = new ThemePalette(
			background: "#000000",
			foreground: "#ffffff"
			);

	}

	/// <summary>
	/// Palette within the theme config. Very closely based on Bootstrap 5 color palette options.
	/// </summary>
	public partial class ThemePalette
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="background"></param>
		/// <param name="foreground"></param>
		/// <param name="primary"></param>
		/// <param name="secondary"></param>
		/// <param name="tertiary"></param>
		/// <param name="success"></param>
		/// <param name="info"></param>
		/// <param name="warning"></param>
		/// <param name="danger"></param>
		/// <param name="light"></param>
		/// <param name="dark"></param>
		public ThemePalette(
			string background, 
			string foreground, 
			string primary = "#0d6efd",
			string secondary = "#6c757d",
			string tertiary = "#6c757d",
			string success = "#198754",
			string info = "#0dcaf0",
			string warning = "#ffc107",
			string danger = "#dc3545",
			string light = "#f8f9fa",
			string dark = "#212529"
			) {
			Background = background;
			Foreground = foreground;

			Primary = primary;
			Secondary = secondary;
			Tertiary = tertiary;
			Success = success;
			Info = info;
			Warning = warning;
			Danger = danger;
			Light = light;
			Dark = dark;
		}

		/// <summary>
		/// Background col
		/// </summary>
		public string Background { get; set; } = "#fff";

		/// <summary>
		/// Foreground col
		/// </summary>
		public string Foreground { get; set; } = "#000";

		/// <summary>
		/// Primary col
		/// </summary>
		public string Primary { get; set; } = "#0d6efd";

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
		public string Success { get; set; } = "#198754";

		/// <summary>
		/// Info col
		/// </summary>
		public string Info { get; set; } = "#0dcaf0";

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
		public string Dark { get; set; } = "#212529";

	}

}