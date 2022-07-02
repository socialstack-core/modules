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
		/// True if this field should be skipped. RoleId is always obtained through the user row so it is skipped for example.
		/// </summary>
		public bool SkipOutput;
		
		/// <summary>
		/// Private backing field. Name must be of the form _propertyName.
		/// </summary>
		public FieldInfo PrivateFieldInfo;

		/// <summary>
		/// Full name.
		/// </summary>
		public string Name;

		/// <summary>
		/// Shortcode of this field.
		/// </summary>
		public char Shortcode;

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