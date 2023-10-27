using Api.Permissions;

namespace Api.WebSockets;

/// <summary>
/// Stores sync meta for a given type.
/// </summary>
public partial class NetworkRoomTypeMeta
{
	/// <summary>
	/// Gets the load capability from the host service.
	/// </summary>
	public virtual Capability LoadCapability { get; }

	/// <summary>
	/// True if this is a mapping type.
	/// </summary>
	public virtual bool IsMapping {
		get {
			return false;
		}
	}
	
	/// <summary>
	/// Gets or creates the network room of the given ID.
	/// </summary>
	/// <param name="roomId"></param>
	public virtual NetworkRoom GetOrCreateRoom(ulong roomId)
	{
		return null;
	}
}