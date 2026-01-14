using Api.Addresses;
using Api.Permissions;
using System.Collections.Generic;

namespace Api.Eventing;

/// <summary>
/// Events are instanced automatically. 
/// You can however specify a custom type or instance them yourself if you'd like to do so.
/// </summary>
public partial class Events
{

	/// <summary>
	/// Set of events for an address.
	/// </summary>
	public static AddressEventGroup Address;
	
}

/// <summary>
/// Event group for Addresses.
/// </summary>
public partial class AddressEventGroup : EventGroup<Address>
{

}