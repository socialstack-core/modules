using Api.Database;

namespace Api.ContentSync
{
	/// <summary>
	/// Network room type.
	/// </summary>
	[DatabaseField(Group = "host")]
	public class NetworkRoomType : Content<uint>
	{
		
		/// <summary>The unique name of the network room. Always lowercased.</summary>
		public string TypeName;
		
	}

}