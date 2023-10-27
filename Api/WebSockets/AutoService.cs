using Api.Database;
using Api.WebSockets;


public partial class AutoService<T, ID>
{
	/// <summary>
	/// The network room set for the given svc.
	/// Note that this is null for mapping services - use MappingNetworkRooms on those instead, as they are indexed by SourceId.
	/// </summary>
	public NetworkRoomSet<T, ID, ID> StandardNetworkRooms;
}



namespace Api.Startup
{
	/// <summary>
	/// The AutoService for mapping types.
	/// </summary>
	public partial class MappingService<SRC_ID, TARG_ID>
	{

		/// <summary>
		/// The network room set specifically for mappings.
		/// This is indexed by SourceId.
		/// </summary>
		public NetworkRoomSet<Mapping<SRC_ID, TARG_ID>, uint, SRC_ID> MappingNetworkRooms;

	}
}