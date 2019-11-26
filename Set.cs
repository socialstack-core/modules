using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Results
{
	/// <summary>
	/// Used to return a list of results of the given type.
	/// Use this instead of returning a raw list to avoid a classic JSON attack vector
	/// and so you can also optionally provide a total number of rows.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class Set<T>
	{
		/// <summary>
		/// The current results set.
		/// </summary>
		public List<T> Results;
		/// <summary>
		/// The total number of records, if available.
		/// </summary>
		public int? Total;
	}
}
