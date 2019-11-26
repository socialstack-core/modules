using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Results
{
	/// <summary>
	/// Used to indicate a generic success.
	/// </summary>
	public class Success : Result
	{
		/// <summary>
		/// New success result
		/// </summary>
		public Success()
		{
			Type = "success";
		}
	}
}
