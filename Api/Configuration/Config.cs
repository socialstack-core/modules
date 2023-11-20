using Newtonsoft.Json;
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
		/// All global sets. When a new config entry is added, these are 
		/// tested to see if the config should also be added to an active set.
		/// </summary>
		public static Dictionary<string, ConfigSet> SetMap = new Dictionary<string, ConfigSet>();
		
		/// <summary>
		/// The config ID, if applicable.
		/// </summary>
		[JsonIgnore]
		public uint Id;

		/// <summary>
		/// Triggered when this config object is updated.
		/// </summary>
		public event Func<ValueTask> OnChange;

		/// <summary>
		/// The contents of this config converted to the frontend only JSON. Null if no fields are frontend marked.
		/// </summary>
		[JsonIgnore]
		public string FrontendJson;

		/// <summary>
		/// Adds to this set (if it is a ConfigSet).
		/// </summary>
		/// <param name="cfg"></param>
		public virtual ValueTask UpdateInSet(Config cfg)
		{
			return new ValueTask();
		}
		
		/// <summary>
		/// Removes an entry by its ID if it is in this set. Runs OnChange if a removal occurred.
		/// </summary>
		/// <param name="id"></param>
		public virtual ValueTask RemoveById(uint id)
		{
			return new ValueTask();
		}

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

        /// <summary>
        /// Gets the name of this config type.
        /// </summary>
        public virtual string GetName()
        {
            return GetType().Name;
        }

    }

    /// <summary>
    /// A set of configs.
    /// </summary>
    public partial class ConfigSet : Config
	{

		/// <summary>
		/// The key this set uses. Configurations must be for this environment and with this exact key.
		/// </summary>
		public string Name;

		/// <summary>
		/// The type of entry in this set of configs.
		/// </summary>
		public Type EntryType;

        /// <summary>
        /// Gets the index of the config with the given ID.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public virtual int GetIndex(uint id)
		{
			return -1;
		}
	}

	/// <summary>
	/// A set of configs.
	/// </summary>
	public partial class ConfigSet<T> : ConfigSet
		where T:Config,new()
	{
		/// <summary>
		/// The underlying list of configs.
		/// </summary>
		public List<T> Configurations;

		/// <summary>
		/// Primary config.
		/// </summary>
		public T Primary;

		/// <summary>
		/// Creates an empty config set.
		/// </summary>
		public ConfigSet() {
			EntryType = typeof(T);
		}

		/// <summary>
		/// Updates the primary config.
		/// This runs when the set is notably modified: the set goes from length 0 to 1, 1 to 0, or the 0th object was updated.
		/// </summary>
		public async ValueTask UpdatePrimaryConfig()
		{
			T src;

			if (Configurations == null || Configurations.Count == 0)
			{
				// Primary object fields must now be set to that of a default object.
				src = new T();
			}
			else
			{
				if (Primary == Configurations[0])
				{
					// Run the primary update event:
					await Primary.Changed();
					return;
				}
					
				// Primary object fields must now be set to that of a default object.
				src = Configurations[0];
			}

			var type = typeof(T);

			foreach (var property in type.GetProperties())
			{
				property.SetValue(Primary, property.GetValue(src));
			}

			foreach (var field in type.GetFields())
			{
				if (field.Name == "OnChange")
				{
					// Retain original event set for this field.
					continue;
				}

				field.SetValue(Primary, field.GetValue(src));
			}

			await Primary.Changed();
		}

		/// <summary>
		/// Removes an entry by its ID if it is in this set. Runs OnChange if a removal occurred.
		/// </summary>
		/// <param name="id"></param>
		public override async ValueTask RemoveById(uint id)
		{
			if (Configurations == null || Configurations.Count == 0)
			{
				return;
			}

			var removeAt = GetIndex(id);

			if (removeAt != -1)
			{
				Configurations.RemoveAt(removeAt);

				if (Configurations.Count == 0)
				{
					// Primary changed:
					await UpdatePrimaryConfig();
				}

				// Indicate set changed:
				await Changed();
			}
		}

		/// <summary>
		/// Gets the index of the config with the given ID.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public override int GetIndex(uint id)
		{

			var index = -1;

			for (var i = 0; i < Configurations.Count; i++)
			{
				if (Configurations[i].Id == id)
				{
					// Found it!
					index = i;
					break;
				}
			}

			return index;
		}

		/// <summary>
		/// Adds to this set (if it is a ConfigSet), or updates it if it's already in here.
		/// </summary>
		/// <param name="cfg"></param>
		public override async ValueTask UpdateInSet(Config cfg)
		{
			var cleanCfg = cfg as T;

			if (cleanCfg == null)
			{
				// Wrong object!
				return;
			}

			if (Configurations == null)
			{
				Configurations = new List<T>();
			}

			// Is it already in the set?
			var index = GetIndex(cleanCfg.Id);

			if (index != -1)
			{
				// Replace the object:
				Configurations[index] = cleanCfg;

				if (index == 0)
				{
					// Primary changed
					await UpdatePrimaryConfig();
				}
			}
			else
			{
				Configurations.Add((T)cfg);

				if (Configurations.Count == 1)
				{
					// Primary changed
					await UpdatePrimaryConfig();
				}
			}

			// Rebuild any frontend configs:
			Startup.Services.Get<ConfigurationService>().UpdateFrontendConfig(cfg, this);

			// Tell the set it just changed:
			await Changed();
		}

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

        /// <summary>
        /// Gets the name of this config type.
        /// </summary>
        public override string GetName()
        {
            return typeof(T).Name;
        }
    }

}