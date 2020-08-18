using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.ProfanityFilter
{
	/// <summary>
	/// The appsettings.json config block for the profanity filter.
	/// </summary>
    public class ProfanityFilterConfig
    {
		/// <summary>
		/// The raw patterns that are word-word matched.
		/// They can contain * at either the start, end or both. A literal * is escaped with backslash, and a literal backslash is a double backslash.
		/// Start with ! to mean exclude. I.e. if a word matches another pattern, but then also matches an exclusion, it will not trigger the filter.
		/// </summary>
		public string[] Patterns {get; set;}
	}
	
}
