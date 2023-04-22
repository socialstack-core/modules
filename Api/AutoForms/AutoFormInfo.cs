using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.AutoForms
{
	/// <summary>
	/// Defines a particular autoform - the endpoint it occurs at and the fields it has.
	/// </summary>
	public class AutoFormInfo
	{
		/// <summary>
		/// True if it supports revisions.
		/// </summary>
		public bool SupportsRevisions;
		
		/// <summary>
		/// The endpoint that this autoform is for.
		/// e.g. "v1/forumreply"
		/// </summary>
		public string Endpoint;

		/// <summary>
		/// The fields in the form.
		/// </summary>
		public List<AutoFormField> Fields;

	}
	
	/// <summary>
	/// Follows the same JSON structure as canvas. module maps directly to a component, data to props.
	/// </summary>
	public class AutoFormField {

		/// <summary>
		/// True if this is a virtual list field and needs to be included if you want its value.
		/// </summary>
		public bool Includable;

		/// <summary>
		/// The fields value type.
		/// </summary>
		public string ValueType;

		/// <summary>
		/// The component to use.
		/// </summary>
		public string Module;

		/// <summary>
		/// The fields order
		/// </summary>
		public uint Order = uint.MaxValue;

		/// <summary>
		/// Props for the module.
		/// </summary>
		public Dictionary<string, object> Data;

		/// <summary>
		/// Can this field be represented by tokens?
		/// </summary>
		public bool Tokeniseable = true;


	}

}
