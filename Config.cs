using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Api.Configuration
{
	
	/// <summary>
	/// Base class of every configuration block.
	/// To create config for your service, create a class that inherits this called {ServiceName}Config.
	/// For example, if your config is for ThingService, then you would 
    /// * Make	public partial class ThingServiceConfig : Config
	/// * GetConfig&lt;ThingServiceConfig&gt;() in e.g. the constructor of ThingService.
	/// Use the BeforeConfigure event to track when it's about to change.
	/// Config is JSON serialised so it can contain lists and sub classes as well.
	/// </summary>
	public partial class Config
	{
		/// <summary>
		/// The config ID, if applicable.
		/// </summary>
		public uint Id;
		
		/// <summary>
		/// Triggered when this config object is updated.
		/// </summary>
		public event Func<ValueTask> OnChange;

		/// <summary>
		/// The contents of this config converted to the frontend only JSON. Null if no fields are frontend marked.
		/// </summary>
		public string FrontendJson;

		/// <summary>
		/// True if this is a set of configs.
		/// </summary>
		public virtual void AddFrontendJson(StringBuilder sb)
		{
			if (FrontendJson == null)
			{
				sb.Append("null");
			}
			else
			{
				sb.Append(FrontendJson);
			}
		}

		/// <summary>
		/// Invoke this to indicate a change has happened.
		/// </summary>
		public async ValueTask Changed()
		{
			if (OnChange == null)
			{
				return;
			}
			await OnChange();
		}
	}

	/// <summary>
	/// A set of configs.
	/// </summary>
	public partial class ConfigSet<T> : Config
		where T:Config
	{
		/// <summary>
		/// The underlying list of configs.
		/// </summary>
		public List<T> Configurations;

		/// <summary>
		/// True if this is a set of configs.
		/// </summary>
		public override void AddFrontendJson(StringBuilder sb)
		{
			sb.Append('[');

			if (Configurations != null)
			{
				for (var i=0;i<Configurations.Count;i++)
				{
					if (i != 0)
					{
						sb.Append(',');
					}

					var config = Configurations[i];
					config.AddFrontendJson(sb);
				}
			}

			sb.Append(']');

		}

	}

}