using Api.AutoForms;
using Api.Permissions;
using System.Collections.Generic;

namespace Api.Eventing
{

	/// <summary>
	/// Events are instanced automatically. 
	/// You can however specify a custom type or instance them yourself if you'd like to do so.
	/// </summary>
	public partial class Events
	{
		/// <summary>
		/// Set of events for an AutoForm.
		/// </summary>
		public static AutoFormEventGroup AutoForm;
	}

	/// <summary>
	/// Set of events for AutoForms.
	/// </summary>
	public class AutoFormEventGroup : EventGroup
	{
		
		/// <summary>
		/// Called when the metadata for an autoform is being constructed.
		/// </summary>
		public EventHandler<AutoFormInfo, AutoService> BuildMeta;

		/// <summary>
		/// Called when building an autoform field. Use this to override the module used for a field based on its contentType.
		/// </summary>
		public EventHandler<AutoFormField, AutoService> GetFieldModule;
		
	}

}
