using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Api.Configuration;


namespace Api.DefaultAvatar
{
	/// <summary>
	/// The config block for default avatars.
	/// </summary>
    public class DefaultAvatarConfig: Config
	{
		/// <summary>
		/// Refs for a set of random profile images to choose from.
		/// </summary>
		public string[] Images {get; set;}
	}
	
}
