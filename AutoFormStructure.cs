using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.AutoForms
{
	/// <summary>
	/// Defines what autoforms are available from this API.
	/// </summary>
	public class AutoFormStructure
	{
		/// <summary>
		/// The forms in this API.
		/// </summary>
		public List<AutoFormInfo> Forms;

		/// <summary>
		/// The content types in this API.
		/// </summary>
		public List<ContentType> ContentTypes;
	}
}
