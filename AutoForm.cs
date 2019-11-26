using Microsoft.AspNetCore.Mvc.Formatters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;


namespace Api.AutoForms
{
	/// <summary>
	/// A model which inherits this gets automated, extendible update/ create endpoints.
	/// The given type is of the target object being updated.
	/// So for example, if your model is for updating user objects name it e.g. UserAutoForm&lt;User&gt;.
	/// 
	/// Fields in your endpoint model which have the same name and type as fields in your target object will be automatically
	/// applied, unless you use the [DontCopy] attribute.
	/// 
	/// Note that providing an ID is the difference between an update and create.
	/// </summary>
	public class AutoForm<T> : AutoForm
	{
		/// <summary>
		/// The object being built by this form.
		/// </summary>
		public T Result;
		
		/// <summary>
		/// Create a new AutoForm.
		/// </summary>
		public AutoForm()
		{
			
		}
		
	}

	/// <summary>
	/// A common base class for all generic auto form types so they can be detected as needed.
	/// </summary>
	public class AutoForm {

		/// <summary>
		/// The raw model fields. Generally use the WasDefined method instead.
		/// </summary>
		public ModelStateDictionary RawModelFields;

		/// <summary>
		/// Returns true if the named field was in the source JSON data at all.
		/// </summary>
		/// <param name="fieldName"></param>
		/// <returns></returns>
		public bool WasDefined(string fieldName)
		{
			return RawModelFields.ContainsKey(fieldName);
		}

	}

}
