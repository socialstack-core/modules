using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Results
{
	/// <summary>
	/// Used to return just an ID.
	/// </summary>
	public class IdResult : Success
	{
		/// <summary>
		/// The ID to return.
		/// </summary>
		public int Id;
	}
}
