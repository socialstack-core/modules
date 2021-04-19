using System;
using System.Reflection;


namespace Api.Contexts
{
	/// <summary>
	/// Stores info about publicly settable context fields.
	/// </summary>
	public class ContextFieldInfo
	{
		/// <summary>
		/// The source prop.
		/// </summary>
		public PropertyInfo Property;

		/// <summary>
		/// The set method.
		/// </summary>
		public MethodInfo Set;

		/// <summary>
		/// The get method.
		/// </summary>
		public MethodInfo Get;

		/// <summary>
		/// Full name.
		/// </summary>
		public string Name;

		/// <summary>
		/// The name pre lowercased with a dash at the end.
		/// </summary>
		public string LowercaseNameWithDash;

		/// <summary>
		/// Default field value.
		/// </summary>
		public uint DefaultValue;

		/// <summary>
		/// The content type ID of the content of this field.
		/// </summary>
		public int ContentTypeId;

		/// <summary>
		/// The service for this field. Set on demand internally inside ContextService.
		/// </summary>
		public AutoService Service;

		/// <summary>
		/// E.g. "user":
		/// </summary>
		public byte[] JsonFieldHeader;
	}
}