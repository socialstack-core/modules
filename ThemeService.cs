using System.Text;

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
		}

		/// <summary>
		/// Current config. Whenever the config is saved, this object ref remains the same (it never goes stale).
		/// </summary>
		private ThemeConfig _current;

		/// <summary>
		/// Gets the theme config.
		/// </summary>
		/// <returns></returns>
		public ThemeConfig GetConfig()
		{
			if (_current == null)
			{
				_current = GetConfig<ThemeConfig>();
			}

			return _current;
		}

		private void OutputObject(StringBuilder builder, string prefix, object varSet)
		{
			var properties = varSet.GetType().GetProperties();

			foreach (var property in properties)
			{
				var lcName = property.Name.ToLower();
				var value = property.GetValue(varSet);

				if (property.PropertyType == typeof(string) && value != null)
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
				}
				else if(value != null)
				{
					OutputObject(builder, prefix == null ? lcName : prefix + "-" + lcName, value);
				}
			}
		}

		/// <summary>
		/// Builds the given config as a collection of css variables.
		/// </summary>
		/// <param name="config"></param>
		/// <returns></returns>
		public string OutputCssVariables(ThemeConfig config)
		{
			var builder = new StringBuilder();
			builder.Append("*{");
			
			OutputObject(builder, null, config);

			builder.Append('}');
			var result = builder.ToString();
			return result;
		}

	}

}