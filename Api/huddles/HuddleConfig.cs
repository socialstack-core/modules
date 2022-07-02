using Api.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Huddles
{
	/// <summary>
	/// Huddle configuration.
	/// </summary>
    public class HuddleConfig : Config
    {
		/// <summary>
		/// Set this if huddle should perform transcodes.
		/// </summary>
		public bool TranscodeUploads { get; set; }
	}
	
}
