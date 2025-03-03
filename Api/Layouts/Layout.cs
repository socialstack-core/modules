using Api.Users;


namespace Api.Layouts
{
	
	/// <summary>
	/// A Layout
	/// </summary>
	public partial class Layout : VersionedContent<uint>
	{
		/// <summary>
		/// The layout name
		/// </summary>
		public string Name;

		/// <summary>
		/// The layout key
		/// </summary>
		public string Key;

		/// <summary>
		/// Layout JSON
		/// </summary>
		public string LayoutJson;		
	}

}