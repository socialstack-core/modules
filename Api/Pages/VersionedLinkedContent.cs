using Api.AutoForms;

namespace Api.Users
{	
	/// <summary>
	/// Use this to get a regular versioned content but also with a Slug for auto permalinks.
	/// </summary>
	public abstract partial class VersionLinkedContent<T> : VersionedContent<T> where T:struct
	{
		/// <summary>
		/// The slug for the content.
		/// </summary>
		[Data("readonly", "true")]
		public string Slug;
	}
}
