using System.Text;
using System.Drawing;
using System;
using System.Linq;
using Api.Configuration;
using Api.Eventing;
using Api.Contexts;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Api.Themes
{
	
	/// <summary>
	/// This service manages and generates (for devs) the frontend code.
	/// It does it by using either precompiled (as much as possible) bundles with metadata, or by compiling in-memory for devs using V8.
	/// </summary>
	public class ThemeService : AutoService
	{
		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public ThemeService()
		{
			_current = GetAllConfig<ThemeConfig>();
		}

		/// <summary>
		/// Gets all the available theme config.
		/// </summary>
		/// <returns></returns>
		public ConfigSet<ThemeConfig> GetAllConfig()
		{
			return _current;
		}

		/// <summary>
		/// Current config.
		/// </summary>
		private ConfigSet<ThemeConfig> _current;

		private void OutputObject(StringBuilder builder, string prefix, object varSet)
		{
			var properties = varSet.GetType().GetProperties();
			string[] bootstrapVariants = { 
				"primary", "secondary", "success", "info", "warning", "danger", "light", "dark"
				};

			foreach (var property in properties)
			{
				var lcName = property.Name.ToLower();
				var value = property.GetValue(varSet);
				Color fgColor, hoverColor, hoverBorderColor, activeColor, activeBorderColor;
				string focusShadow;

				if (lcName == "customcss")
				{
					// Output its contents as is:
					if (value == null)
					{
						continue;
					}

					builder.Append((string)value);

				}else if (property.PropertyType == typeof(string) && value != null)
				{
					var str = (string)value;

					if (string.IsNullOrWhiteSpace(str))
					{
						continue;
					}

					builder.Append("--");

					if (prefix != null)
					{
						builder.Append(prefix);
						builder.Append('-');
					}

					builder.Append(lcName);

					// Very rough!
					builder.Append(':');
					builder.Append(value);
					builder.Append(';');

					// produce hover/active/focus supporting colours based on each Bootrap variant
					if (bootstrapVariants.Contains(prefix) && lcName == "background") {
						UpdateContrastVariant(value.ToString(), out fgColor, out hoverColor, out hoverBorderColor, out activeColor, out activeBorderColor, out focusShadow);
						AddGeneratedColor(ref builder, prefix, "auto-color", fgColor);
						AddGeneratedColor(ref builder, prefix, "auto-hover-background", hoverColor);
						AddGeneratedColor(ref builder, prefix, "auto-hover-border", hoverBorderColor);
						AddGeneratedColor(ref builder, prefix, "auto-active-background", activeColor);
						AddGeneratedColor(ref builder, prefix, "auto-active-border", activeBorderColor);
						AddGeneratedString(ref builder, prefix, "auto-focus-shadow", focusShadow);
					}
				}
				else if(value != null)
				{
					OutputObject(builder, prefix == null ? lcName : prefix + "-" + lcName, value);
				}
			}
		}

		/// <summary>
		/// Builds the given set of configs as a collection of css variables.
		/// </summary>
		/// <param name="set"></param>
		/// <returns></returns>
		public string OutputCssVariables(ConfigSet<ThemeConfig> set)
		{
			var builder = new StringBuilder();

			foreach (var config in set.Configurations)
			{
				builder.Append("*[data-theme=\"");
				builder.Append(config.Id);
				builder.Append("\"]{");

				OutputObject(builder, null, config);

				builder.Append('}');
			}
			var result = builder.ToString();
			return result;
		}

		/// <summary>
		/// Builds the given config as a collection of css variables.
		/// </summary>
		/// <param name="config"></param>
		/// <returns></returns>
		public string OutputCssVariables(ThemeConfig config)
		{
			var builder = new StringBuilder();

			// TODO:
			// add support for high-contrast themes
			// - high contrast mode on Windows can be detected via "@media(forced-colors:active)" *
			// - longer-term, we should aim to use "@media(prefers-contrast:more)", but as of July 2020, this has zero support
			// 
			// * note that this mode disables box-shadow effects

			if (config.DarkModeOfThemeId != 0)
			{
				builder.Append("@media(prefers-color-scheme:dark){*[data-theme=\"");
				builder.Append(config.DarkModeOfThemeId);
				builder.Append("\"]{");
			}
			else
			{
				builder.Append("*[data-theme=\"");
				builder.Append(config.Id);
				builder.Append("\"]{");
			}

			OutputObject(builder, null, config);

			if (config.DarkModeOfThemeId != 0)
			{
				builder.Append("}}");
			}
			else
			{
				builder.Append('}');
			}

			var result = builder.ToString();
			return result;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="builder"></param>
		/// <param name="prefix"></param>
		/// <param name="name"></param>
		/// <param name="color"></param>
		public void AddGeneratedColor(ref StringBuilder builder, string prefix, string name, Color color)
        {
			builder.Append("--");

			if (prefix != null)
			{
				builder.Append(prefix);
				builder.Append('-');
			}

			builder.Append(name);

			builder.Append(':');
			builder.Append(ColorToHex(color));
			builder.Append(';');
        }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="builder"></param>
		/// <param name="prefix"></param>
		/// <param name="name"></param>
		/// <param name="value"></param>
		public void AddGeneratedString(ref StringBuilder builder, string prefix, string name, string value)
        {
			builder.Append("--");

			if (prefix != null)
			{
				builder.Append(prefix);
				builder.Append('-');
			}

			builder.Append(name);

			builder.Append(':');
			builder.Append(value);
			builder.Append(';');
        }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="hex"></param>
		/// <param name="fgColor"></param>
		/// <param name="hoverColor"></param>
		/// <param name="hoverBorderColor"></param>
		/// <param name="activeColor"></param>
		/// <param name="activeBorderColor"></param>
		/// <param name="focusShadow"></param>
		public void UpdateContrastVariant(string hex, 
			out Color fgColor, 
			out Color hoverColor, out Color hoverBorderColor,
			out Color activeColor, out Color activeBorderColor,
			out String focusShadow)
        {
			var bgColor = HexToColor(hex);
			fgColor = GetContrastColor(bgColor);

			// based on bootstrap button-variant() mixin
			hoverColor = (fgColor == Color.White) ? ShadeColor(bgColor, 15) : TintColor(bgColor, 15);
			hoverBorderColor = (fgColor == Color.White) ? ShadeColor(bgColor, 20) : TintColor(bgColor, 10);
			activeColor = (fgColor == Color.White) ? ShadeColor(bgColor, 20) : TintColor(bgColor, 20);
			activeBorderColor = (fgColor == Color.White) ? ShadeColor(bgColor, 25) : TintColor(bgColor, 10);

			Color focusShadowColor = MixColor(fgColor, bgColor, 15);
			focusShadow = "0 0 0 .25rem " + ColorToHex(focusShadowColor, 50);
        }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="hex"></param>
		/// <returns></returns>
		public Color HexToColor(string hex)
        {
			var colorConverter = new ColorConverter();
			var alpha = 255;

			// NB: check for #RRGGBBAA format colours - 
			// ConvertFromString() returns the last 6 chars
			if (hex.Length > 7)
            {
				var alphaString = hex.Substring(hex.Length - 2);
				alpha = int.Parse(alphaString, System.Globalization.NumberStyles.HexNumber);
				hex = hex.TrimEnd(hex[hex.Length - 2]);
            }

			var color = (Color)colorConverter.ConvertFromString(hex);
			return Color.FromArgb(alpha, color);
        }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="color"></param>
		/// <param name="opacity"></param>
		/// <returns></returns>
		public string ColorToHex(Color color, int opacity = 100)
        {
			// optionally include opacity - this has the added benefit of always returning colours in #RRGGBB format
			// (possible to get named HTML colours without this, e.g. #FFF returns as "White")
			int alpha = (int)Math.Round((opacity / 100d) * 255d);
			Color colWithAlpha = Color.FromArgb(alpha, color);
			var hex = ColorTranslator.ToHtml(colWithAlpha);

			// NB: ColorTranslator.ToHtml() doesn't include opacity info
			if (alpha < 255)
            {
				hex += alpha.ToString("X2");
            }

			return hex;
        }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="color"></param>
		/// <returns></returns>
		public Color GetContrastColor(Color color)
        {
			// http://www.w3.org/TR/AERT#color-contrast
			var brightness = Math.Round(
				(
					(color.R * 299d) +
					(color.G * 587d) +
					(color.B * 114d)
				) / 1000d);
			
			return (brightness > 125) ? Color.Black : Color.White;
        }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="color"></param>
		/// <param name="amount"></param>
		/// <returns></returns>
		public Color LightenColor(Color color, int amount)
        {
			double h, s, l;
			ColorToHsl(color, out h, out s, out l);
			l = Math.Min(l + amount, 100d);

			return HslToColor(h, s, l);
        }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="color"></param>
		/// <param name="amount"></param>
		/// <returns></returns>
		public Color DarkenColor(Color color, int amount)
        {
			double h, s, l;
			ColorToHsl(color, out h, out s, out l);
			l = Math.Max(l - amount, 0d);

			return HslToColor(h, s, l);
        }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="color1"></param>
		/// <param name="color2"></param>
		/// <param name="amount"></param>
		/// <returns></returns>
		public Color MixColor(Color color1, Color color2, int amount)
        {
			// ref: https://fossies.org/linux/dart-sass/lib/src/functions/color.dart
			double weightScale = amount / 100d;
			double normalizedWeight = weightScale * 2 - 1;
			//var alphaDistance = color1.A - color2.A;
			var alphaDistance = 0;
			
			var combinedWeight1 = normalizedWeight * alphaDistance == -1
				? normalizedWeight
				: (normalizedWeight + alphaDistance) /
				(1 + normalizedWeight * alphaDistance);
			
			double weight1 = (combinedWeight1 + 1) / 2;
			double weight2 = 1 - weight1;
			
			var r = Math.Round(color1.R * weight1 + color2.R * weight2);
			var g = Math.Round(color1.G * weight1 + color2.G * weight2);
			var b = Math.Round(color1.B * weight1 + color2.B * weight2);
			//var a = Math.Round(color1.A * weightScale + color2.A * (1 - weightScale));
			var a = 255;

			return Color.FromArgb(a, (int)r, (int)g, (int)b);
        }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="color"></param>
		/// <param name="weight"></param>
		/// <returns></returns>
		public Color TintColor(Color color, int weight) 
		{ 
			return MixColor(Color.White, color, weight);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="color"></param>
		/// <param name="weight"></param>
		/// <returns></returns>
		public Color ShadeColor(Color color, int weight) 
		{ 
			return MixColor(Color.Black, color, weight);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="color"></param>
		/// <param name="h"></param>
		/// <param name="s"></param>
		/// <param name="l"></param>
		public void ColorToHsl(Color color, out double h, out double s, out double l)
        {
            double r = color.R / 255.0;
            double g = color.G / 255.0;
            double b = color.B / 255.0;
            double v;
            double m;
            double vm;
            double r2, g2, b2;

            h = 0; // default to black
            s = 0;
            l = 0;

            v = Math.Max(r,g);
            v = Math.Max(v,b);
            m = Math.Min(r,g);
            m = Math.Min(m,b);

            l = (m + v) / 2.0;

            if (l <= 0.0) 
			{ 
				return; 
			}

            vm = v - m;
            s = vm;

            if (s > 0.0) 
			{
                  s /= (l <= 0.5) ? (v + m ) : (2.0 - v - m);
            } else {
				return;
            }

            r2 = (v - r) / vm;
            g2 = (v - g) / vm;
            b2 = (v - b) / vm;

            if (r == v)
            {
				h = (g == m ? 5.0 + b2 : 1.0 - g2);
            }
            else if (g == v)
            {
				h = (b == m ? 1.0 + r2 : 3.0 - b2);
            }
            else
            {
				h = (r == m ? 3.0 + g2 : 5.0 - r2);
            }

            h /= 6.0;
        }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="h"></param>
		/// <param name="s"></param>
		/// <param name="l"></param>
		/// <param name="a"></param>
		/// <returns></returns>
		public Color HslToColor(double h, double s, double l, int a = 255)
        {
            h = Math.Max(0D, Math.Min(360D, h));
            s = Math.Max(0D, Math.Min(1D, s));
            l = Math.Max(0D, Math.Min(1D, l));
            a = Math.Max(0, Math.Min(255, a));

            // achromatic argb (gray scale)
            if (Math.Abs(s) < 0.000000000000001) {
                return Color.FromArgb(
                        a,
                        Math.Max(0, Math.Min(255, Convert.ToInt32(double.Parse($"{l * 255D:0.00}")))),
                        Math.Max(0, Math.Min(255, Convert.ToInt32(double.Parse($"{l * 255D:0.00}")))),
                        Math.Max(0, Math.Min(255, Convert.ToInt32(double.Parse($"{l * 255D:0.00}")))));
            }

            double q = l < .5D
                    ? l * (1D + s)
                    : (l + s) - (l * s);
            double p = (2D * l) - q;

            double hk = h / 360D;
            double[] T = new double[3];
            T[0] = hk + (1D / 3D); // Tr
            T[1] = hk; // Tb
            T[2] = hk - (1D / 3D); // Tg

            for (int i = 0; i < 3; i++) 
			{
                if (T[i] < 0D) 
				{ 
                    T[i] += 1D;
				}

                if (T[i] > 1D)
				{ 
                    T[i] -= 1D;
				}

                if ((T[i] * 6D) < 1D)
                {
                    T[i] = p + ((q - p) * 6D * T[i]);
                }
                else if ((T[i] * 2D) < 1)
				{ 
                    T[i] = q;
				}
                else if ((T[i] * 3D) < 2)
				{ 
                    T[i] = p + ((q - p) * ((2D / 3D) - T[i]) * 6D);
				}
                else
                {
                    T[i] = p;
                }
            }

            return Color.FromArgb(
                    a,
                    Math.Max(0, Math.Min(255, Convert.ToInt32(double.Parse($"{T[0] * 255D:0.00}")))),
                    Math.Max(0, Math.Min(255, Convert.ToInt32(double.Parse($"{T[1] * 255D:0.00}")))),
                    Math.Max(0, Math.Min(255, Convert.ToInt32(double.Parse($"{T[2] * 255D:0.00}")))));
        }

	}

}