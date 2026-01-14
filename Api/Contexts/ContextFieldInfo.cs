using System;
using System.Reflection;


namespace Api.Contexts
{
	/// <summary>
	/// Stores info about publicly settable context fields.
	/// </summary>
	public partial class ContextFieldInfo
	{
		/// <summary>
		/// The source prop.
		/// </summary>
		public PropertyInfo Property;

		/// <summary>
		/// True if this field should be omitted from the token. 
		/// RoleId is always obtained through the user row so it is skipped for example.
		/// </summary>
		public bool OmitFromToken;
		
		/// <summary>
		/// Private backing field. Name must be of the form _propertyName e.g. _userId.
		/// </summary>
		public FieldInfo PrivateFieldInfo;

		/// <summary>
		/// Full name.
		/// </summary>
		public string Name;
		
		/// <summary>
		/// The field name with 'Id' truncated from the end, e.g. "User". Not the same as ContentType.Name.
		/// This is the uppercased name of the field as it appears in the JSON.
		/// </summary>
		public string ContentFieldName;

		/// <summary>
		/// The name as it appears in the JSON. It is the ContentFieldName but lowercased.
		/// </summary>
		public string JsonFieldName => ContentFieldName.ToLower();

		/// <summary>
		/// Shortcode of this field.
		/// </summary>
		public char Shortcode;

		/// <summary>
		/// Default field value.
		/// </summary>
		public uint DefaultValue;

		/// <summary>
		/// The content type of the content of this field.
		/// </summary>
		public Type ContentType;

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