using System.Text;
using System.Drawing;
using System;
using System.Linq;
using Api.Configuration;
using Api.Eventing;
using Api.Contexts;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Startup;

namespace Api.Themes
{

    /// <summary>
    /// This service manages and generates (for devs) the frontend code.
    /// It does it by using either precompiled (as much as possible) bundles with metadata, or by compiling in-memory for devs using V8.
    /// </summary>
    public class ThemeService : AutoService
    {
        /// <summary>
        /// 
        /// </summary>
        public Dictionary<string, string> bootstrapVariants = new Dictionary<string, string>()
        {
            { "primary",    "#0d6efd" },
            { "secondary",  "#6c757d" },
            { "success",    "#198754" },
            { "danger",     "#dc3545" },
            { "warning",    "#ffc107" },
            { "info",       "#0dcaf0" },
            { "light",      "#f8f9fa" },
            { "dark",       "#212529" }
        };

        /// <summary>
        /// Instanced automatically.
        /// </summary>
        public ThemeService(ConfigurationService configService)
        {
            _globalCfg = GetConfig<GlobalThemeConfig>();
            _current = GetAllConfig<ThemeConfig>();

            // If the theme list doesn't contain anything, two are created - admin and main.
            if (_current.Configurations == null || _current.Configurations.Count == 0)
            {
                var defaultVars = new Dictionary<string, string>(){
                    { "primary", "#0d6efd" },
                    { "primary-fg", "" },
                    { "primary-shadow", "" },
                    { "primary-hover", "" },
                    { "primary-hover-border", "" },
                    { "primary-active", "" },
                    { "primary-active-border", "" },
                    { "secondary", "#6c757d" },
                    { "secondary-shadow", "" },
                    { "secondary-fg", "" },
                    { "secondary-hover", "" },
                    { "secondary-hover-border", "" },
                    { "secondary-active", "" },
                    { "secondary-active-border", "" },
                    { "success", "#198754" },
                    { "success-fg", "" },
                    { "success-shadow", "" },
                    { "success-hover", "" },
                    { "success-hover-border", "" },
                    { "success-active", "" },
                    { "success-active-border", "" },
                    { "info", "#0dcaf0" },
                    { "info-shadow", "" },
                    { "info-fg", "" },
                    { "info-hover", "" },
                    { "info-hover-border", "" },
                    { "info-active", "" },
                    { "info-active-border", "" },
                    { "warning", "#ffc107" },
                    { "warning-shadow", "" },
                    { "warning-fg", "" },
                    { "warning-hover", "" },
                    { "warning-hover-border", "" },
                    { "warning-active", "" },
                    { "warning-active-border", "" },
                    { "danger", "#dc3545" },
                    { "danger-shadow", "" },
                    { "danger-fg", "" },
                    { "danger-hover", "" },
                    { "danger-hover-border", "" },
                    { "danger-active", "" },
                    { "danger-active-border", "" },
                    { "light", "#f8f9fa" },
                    { "light-shadow", "" },
                    { "light-fg", "" },
                    { "light-hover", "" },
                    { "light-hover-border", "" },
                    { "light-active", "" },
                    { "light-active-border", "" },
                    { "dark", "#212529" },
                    { "dark-shadow", "" },
                    { "dark-fg", "" },
                    { "dark-hover", "" },
                    { "dark-hover-border", "" },
                    { "dark-active", "" },
                    { "dark-active-border", "" },
                    { "font-mono", "SFMono-Regular,Menlo,Monaco,Consolas,\"Liberation Mono\",\"Courier New\",monospace" },
                    { "font", "\"OpenSans\",\"Helvetica Neue\",Arial,\"Noto Sans\",\"Liberation Sans\",sans-serif,\"Apple Color Emoji\",\"Segoe UI Emoji\",\"Segoe UI Symbol\",\"Noto Color Emoji\"" }
                };

                var admin = new ThemeConfig()
                {
                    Key = "admin",
                    Variables = defaultVars
                };

                var main = new ThemeConfig()
                {
                    Key = "main",
                    Variables = defaultVars
                };

                _ = configService.InstallConfig(admin, "Admin Theme", "Theme", _current);
                _ = configService.InstallConfig(main, "Main Site Theme", "Theme", _current);
            }
        }

        /// <summary>
        /// Gets the current latest global config. Use this for e.g. the site logo.
        /// </summary>
        /// <returns></returns>
        public GlobalThemeConfig GetConfig()
        {
            return _globalCfg;
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
        /// Current configured themes.
        /// </summary>
        private ConfigSet<ThemeConfig> _current;

        /// <summary>
        /// Current global config.
        /// </summary>
        private GlobalThemeConfig _globalCfg;

        /*
		private void OutputObject(StringBuilder builder, string prefix, object varSet, bool isDefault = false)
		{
			var properties = varSet.GetType().GetProperties();

			foreach (var property in properties)
			{
				var lcName = property.Name.ToLower();
				var value = property.GetValue(varSet);
				Color fgColor, hoverColor, hoverBorderColor, activeColor, activeBorderColor;
				string focusShadow;

				var currentIsDefault = isDefault;

				if (lcName == "default")
				{
					currentIsDefault = true;
				}

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

					if (currentIsDefault)
					{
						builder.Append(lcName);

						// Also very rough!
						builder.Append(':');
						builder.Append(value);
						builder.Append(';');
					}

					// produce hover/active/focus supporting colours based on each Bootrap variant
					if (bootstrapVariants.ContainsKey(prefix) && lcName == "background") {
						UpdateContrastVariant(str, out fgColor, out hoverColor, out hoverBorderColor, out activeColor, out activeBorderColor, out focusShadow);
						AddGeneratedColor(ref builder, prefix, "auto-border", HexToColor(str));
						AddGeneratedColor(ref builder, prefix, "auto-color", fgColor);
						AddGeneratedColor(ref builder, prefix, "auto-hover-background", hoverColor);
						AddGeneratedColor(ref builder, prefix, "auto-hover-border", hoverBorderColor);
						AddGeneratedColor(ref builder, prefix, "auto-hover-color", GetContrastColor(hoverColor));
						AddGeneratedColor(ref builder, prefix, "auto-active-background", activeColor);
						AddGeneratedColor(ref builder, prefix, "auto-active-border", activeBorderColor);
						AddGeneratedColor(ref builder, prefix, "auto-active-color", GetContrastColor(activeColor));
						AddGeneratedString(ref builder, prefix, "auto-focus-shadow", focusShadow);
					}

					var prefixSegments = prefix.Split("-");

					// ensure text contrast is sufficient for hover / active states
					if (bootstrapVariants.ContainsKey(prefixSegments[0]) && prefixSegments.Length > 1 && lcName == "background") {

						switch (prefixSegments[1])
                        {
							case "hover":
								AddGeneratedColor(ref builder, prefix, "color", GetContrastColor(HexToColor(str)));
								break;

							case "active":
								AddGeneratedColor(ref builder, prefix, "color", GetContrastColor(HexToColor(str)));
								break;

                        }

					}
				}
				else if(value != null)
				{
					OutputObject(builder, prefix == null ? lcName : prefix + "-" + lcName, value, currentIsDefault);
				}
			}
		}
		*/

        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        /// <param name="selector"></param>
        /// <param name="id"></param>
        /// <param name="builder"></param>
        /// <returns></returns>
        public void OutputCssBlock(ThemeConfig config, string selector, string id, bool html, ref StringBuilder builder)
        {
            bool hasSelector = selector != null && selector.Length > 0;
            bool isMediaQuery = hasSelector && selector.StartsWith("@media");

            if (hasSelector)
            {
                builder.Append(selector);
            }

            if (isMediaQuery)
            {
                builder.Append("{");
            }

            if (!html)
            {
                builder.Append("*");
            }

            builder.Append("[data-theme=\"");
            builder.Append(id);
            builder.Append("\"]{");

            if (config.Variables != null)
            {
                // Output each one:
                foreach (var kvp in config.Variables)
                {
                    var lcName = kvp.Key.ToLower();
                    var value = kvp.Value;

                    if (string.IsNullOrWhiteSpace(value))
                    {
                        // Generate a value now.
                        // We'll use the name to identify the "parent" variable.
                        // Note that this can only be generated if the parent is also defined/ re-defined on this current theme; it can't use an inherited theme value.

                        var firstDash = kvp.Key.IndexOf('-');

                        if (firstDash == -1)
                        {
                            continue;
                        }

                        var colorName = kvp.Key.Substring(0, firstDash);
                        Color targetColor = Color.White;

                        if (config.Variables.TryGetValue(colorName, out string parentValue))
                        {
                            // Got the parent one!
                            // Attempt to generate the value next.
                            var bgColor = ColorMap.FromCss(parentValue);
                            var fgColor = bgColor.GetContrastColor();

                            // based on bootstrap button-variant() mixin
                            if (kvp.Key.EndsWith("hover"))
                            {
                                targetColor = (fgColor.IsWhite) ? bgColor.ShadeColor(0.15f) : bgColor.TintColor(0.15f);
                            }
                            else if (kvp.Key.EndsWith("fg"))
                            {
                                targetColor = fgColor;
                            }
                            else if (kvp.Key.EndsWith("hover-border"))
                            {
                                targetColor = (fgColor.IsWhite) ? bgColor.ShadeColor(0.20f) : bgColor.TintColor(0.1f);
                            }
                            else if (kvp.Key.EndsWith("active"))
                            {
                                targetColor = (fgColor.IsWhite) ? bgColor.ShadeColor(0.20f) : bgColor.TintColor(0.2f);
                            }
                            else if (kvp.Key.EndsWith("active-border"))
                            {
                                targetColor = (fgColor.IsWhite) ? bgColor.ShadeColor(0.25f) : bgColor.TintColor(0.1f);
                            }
                            else if (kvp.Key.EndsWith("shadow"))
                            {
                                targetColor = fgColor.Mix(bgColor, 0.15f);
                                targetColor.A = 0.5f;
                            }

                        }

                        builder.Append("--");
                        builder.Append(lcName);
                        builder.Append(':');
                        targetColor.ToCss(builder);
                    }
                    else
                    {
                        builder.Append("--");
                        builder.Append(lcName);
                        builder.Append(':');
                        builder.Append(value);
                    }
                    builder.Append(';');
                }
            }

            // And the CSS block.
            if (!string.IsNullOrWhiteSpace(config.Css))
            {
                // Internally closes the block (as it might add some properties to it):

                var blockRoot = "*[data-theme=\"";
                if (string.IsNullOrEmpty(config.Key))
                {
                    blockRoot += config.Id.ToString();
                }
                else
                {
                    blockRoot += config.Key;
                }
                blockRoot += "\"]";

                ReconstructCss(builder, config.Css, blockRoot);
            }
            else
            {
                // Close the block:
                builder.Append('}');
            }

            if (isMediaQuery)
            {
                builder.Append("}");
            }

        }

        /// <summary>
        /// Builds the given set of configs out to the CSS rules.
        /// </summary>
        /// <param name="set"></param>
        /// <returns></returns>
        public string OutputCss(ConfigSet<ThemeConfig> set)
        {
            var builder = new StringBuilder();

            // OutputCssDefaults(builder);

            foreach (var config in set.Configurations)
            {

                if (!string.IsNullOrEmpty(config.DarkModeOfThemeId))
                {
                    OutputCssBlock(config, "@media(prefers-color-scheme:dark)", config.DarkModeOfThemeId, false, ref builder);
                    OutputCssBlock(config, "html[data-theme-variant=\"dark\"]", config.DarkModeOfThemeId, false, ref builder);
                    OutputCssBlock(config, "html[data-theme-variant=\"dark\"]", config.DarkModeOfThemeId, true, ref builder);
                }
                else
                {
                    string id = string.IsNullOrEmpty(config.Key) ? config.Id.ToString() : config.Key;
                    OutputCssBlock(config, "", id, false, ref builder);
                }

            }

            var result = builder.ToString();
            return result;
        }

        private void ReconstructCss(StringBuilder builder, string css, string blockRoot)
        {
            // Parse the CSS. Any :root properties are emitted immediately into the builder. Any selectors have the theme prepended to it.
            var lexer = new CssLexer(css);
            var state = 0;
            char strDelim = '\0';
            lexer.SkipJunk(true);

            string selector = null;
            StringBuilder tokenBuilder = new StringBuilder();
            StringBuilder additionalSelectors = null;

            while (lexer.More())
            {
                var current = lexer.Current;

                if (state == 0)
                {
                    if (current == '/' && lexer.Peek() == '*')
                    {
                        // comment state
                        state = 1;
                    }
                    else if (current == '{')
                    {
                        // Finished reading a selector.
                        selector = tokenBuilder.ToString();
                        tokenBuilder.Clear();
                        state = 2;
                    }
                    else
                    {
                        tokenBuilder.Append(current);
                        lexer.SkipJunk(false);
                    }
                }
                else if (state == 1)
                {
                    // Skip until */
                    if (current == '*' && lexer.Peek() == '/')
                    {
                        // Skip the /
                        lexer.Skip();
                        state = 0;
                    }
                }
                else if (state == 2)
                {
                    // Read property block until }

                    if (current == '"')
                    {
                        // Entering string mode.
                        state = 3;
                        strDelim = current;
                        tokenBuilder.Append(current);
                    }
                    else if (current == '\'')
                    {
                        // Entering string mode.
                        state = 3;
                        strDelim = current;
                        tokenBuilder.Append(current);
                    }
                    else if (current == '}')
                    {
                        // Done!
                        var propertyBlock = tokenBuilder.ToString();
                        tokenBuilder.Clear();
                        state = 0;

                        // Add the selector + propertyBlock now.
                        selector = selector.Trim();

                        if (selector == ":root")
                        {
                            // property block goes direct into the builder.
                            builder.Append(propertyBlock);
                        }
                        else
                        {
                            if (additionalSelectors == null)
                            {
                                additionalSelectors = new StringBuilder();
                            }

                            if (selector.IndexOf(',') != -1)
                            {
                                // Multiple selectors - each needs the prefix. Might even have :root in here.
                                var subSelectors = selector.Split(',');
                                for (var i = 0; i < subSelectors.Length; i++)
                                {
                                    var subSelector = subSelectors[i].Trim();

                                    if (i != 0)
                                    {
                                        additionalSelectors.Append(',');
                                    }

                                    additionalSelectors.Append(blockRoot);

                                    if (subSelector != ":root")
                                    {
                                        additionalSelectors.Append(' ');
                                        additionalSelectors.Append(subSelector);
                                    }

                                }
                            }
                            else
                            {
                                // Prefix the selector:
                                additionalSelectors.Append(blockRoot);

                                if (!(selector.Length > 2 && selector[0] == ':' && selector[1] == ':'))
                                {
                                    // ::before otherwise, no space
                                    additionalSelectors.Append(' ');
                                }

                                additionalSelectors.Append(selector);
                            }

                            additionalSelectors.Append('{');
                            additionalSelectors.Append(propertyBlock);
                            additionalSelectors.Append('}');
                        }
                    }
                    else
                    {
                        tokenBuilder.Append(current);
                        lexer.SkipJunk(false);
                    }
                }
                else if (state == 3)
                {
                    // String reading.
                    if (current == '\\')
                    {
                        var literal = lexer.Peek();
                        lexer.Skip();
                        if (literal != '\0')
                        {
                            tokenBuilder.Append(literal);
                        }
                    }
                    else if (current == strDelim)
                    {
                        // Done!
                        tokenBuilder.Append(current);
                        state = 2;
                    }
                    else
                    {
                        tokenBuilder.Append(current);
                    }
                }

            }

            // Close data theme block:
            builder.Append('}');

            if (additionalSelectors != null)
            {
                // Got extra selectors.

                // Output them:
                builder.Append(additionalSelectors.ToString());
            }
        }

        /*
		/// <summary>
		/// 
		/// </summary>
		/// <param name="builder"></param>
		public void OutputCssDefaults(StringBuilder builder)
        {
			builder.Append(":root {");

			// ensure we have fallback defaults
			foreach (KeyValuePair<string, string> variant in bootstrapVariants)
			{
				Color fgColor, hoverColor, hoverBorderColor, activeColor, activeBorderColor;
				string focusShadow;

				UpdateContrastVariant(variant.Value, out fgColor, out hoverColor, out hoverBorderColor, out activeColor, out activeBorderColor, out focusShadow);
				AddGeneratedColor(ref builder, variant.Key, "auto-border", HexToColor(variant.Value));
				AddGeneratedColor(ref builder, variant.Key, "auto-color", fgColor);
				AddGeneratedColor(ref builder, variant.Key, "auto-hover-background", hoverColor);
				AddGeneratedColor(ref builder, variant.Key, "auto-hover-border", hoverBorderColor);
				AddGeneratedColor(ref builder, variant.Key, "auto-hover-color", GetContrastColor(hoverColor));
				AddGeneratedColor(ref builder, variant.Key, "auto-active-background", activeColor);
				AddGeneratedColor(ref builder, variant.Key, "auto-active-border", activeBorderColor);
				AddGeneratedColor(ref builder, variant.Key, "auto-active-color", GetContrastColor(activeColor));
				AddGeneratedString(ref builder, variant.Key, "auto-focus-shadow", focusShadow);
			}

			builder.Append('}');
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

		*/
    }

}