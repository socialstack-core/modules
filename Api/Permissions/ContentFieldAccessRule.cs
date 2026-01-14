using Api.Users;


namespace Api.Permissions
{
	
	/// <summary>
	/// A ContentFieldAccessRule
	/// </summary>
	public partial class ContentFieldAccessRule : VersionedContent<uint>
	{
		/// <summary>
		/// The full name of the entity. 
		/// </summary>
		public string EntityName;

		/// <summary>
		/// Is the entity a virtual entity, or a real C# one.
		/// </summary>
		public bool IsVirtualType;

		/// <summary>
		/// The field on the entity.
		/// </summary>
		public string FieldName;

		/// <summary>
		/// Can read filter string, can be true, false, IsSelf(), etc... 
		/// leave null to rely on the parent roles inheritence.
		/// </summary>
		public string CanRead;

		/// <summary>
		/// Can write filter string, can be true, false, IsSelf(), etc...
		/// leave null to rely on the parent roles inheritence.
		/// </summary>
		public string CanWrite;

		/// <summary>
		/// What role does this rule belong to
		/// </summary>
		public uint RoleId;
		
	}

}