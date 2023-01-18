using Api.CustomContentTypes;
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
		/// Set of events for a customContentType.
		/// </summary>
		public static EventGroup<CustomContentType> CustomContentType;
		
		/// <summary>
		/// Set of events for a customContentTypeField.
		/// </summary>
		public static EventGroup<CustomContentTypeField> CustomContentTypeField;

		/// <summary>
		/// Set of events for a customContentTypeSelectOption.
		/// </summary>
		public static EventGroup<CustomContentTypeSelectOption> CustomContentTypeSelectOption;
	}
}