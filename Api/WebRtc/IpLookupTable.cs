namespace Api.WebRTC;

/// <summary>
/// Used for routing a 2 byte value -> a client object.
/// </summary>
public struct IpLookupTable<T> where T : RtpClient, new()
{
	/// <summary>
	/// The client at this node. Always null at the root node.
	/// </summary>
	public T Client;

	/// <summary>
	/// The sub-tables of this lookup table.
	/// </summary>
	public IpLookupTable<T>[] SubTables;


	/// <summary>
	/// Creates a leaf node for the given client
	/// </summary>
	/// <param name="client"></param>
	public IpLookupTable(T client)
	{
		Client = client;
		SubTables = null;
	}

	/// <summary>
	/// Creates root/ intermediate node.
	/// </summary>
	public IpLookupTable()
	{
		Client = null;
		SubTables = new IpLookupTable<T>[ushort.MaxValue];
	}

	/// <summary>
	/// Removes the given client from this node.
	/// </summary>
	/// <param name="client"></param>
	/// <param name="depth"></param>
	public void RemoveClient(RtpClient client, int depth)
	{
		if (Client == null && depth != -2)
		{
			// Can't directly add a client to root node, thus depth check.
			return;
		}

		if (Client == client)
		{
			Client = null;
			return;
		}

		if (SubTables != null)
		{
			// Remove client from subtable:
			int fragment = client.GetAddressFragment(depth);
			SubTables[fragment].RemoveClient(client, depth + 2);
		}
	}

	/// <summary>
	/// Adds the given client to this node.
	/// </summary>
	/// <param name="client"></param>
	/// <param name="depth"></param>
	public void AddClient(T client, int depth)
	{
		if (Client == null && depth != -2)
		{
			// Can't directly add a client to root node, thus depth check.
			Client = client;
			return;
		}

		if (Client != null)
		{
			if (Client == client)
			{
				return;
			}

			// Check if their addresses are the same.
			// This avoids a race to the bottom of the tree in an instant port reuse scenario.
			if (Client.RemoteAddress.Port == client.RemoteAddress.Port && Client.RemoteAddress.Address == client.RemoteAddress.Address)
			{
				// They're the same. The new client will replace the existing one.
				Client = client;
				return;
			}
		}

		// Must add to subtables if there aren't any.
		if (SubTables == null)
		{
			SubTables = new IpLookupTable<T>[ushort.MaxValue];
		}

		int fragment;

		if (Client != null)
		{
			// This will no longer be a leaf node.
			// Relocate the leaf nodes client to its relevant subtable.
			fragment = Client.GetAddressFragment(depth);
			SubTables[fragment].AddClient(Client, depth + 2);
			Client = null;
		}

		// Add client to subtable:
		fragment = client.GetAddressFragment(depth);
		SubTables[fragment].AddClient(client, depth + 2);
	}

	/// <summary>
	/// Sets up subtables
	/// </summary>
	public IpLookupTable<T>[] AddUser()
	{
		SubTables = new IpLookupTable<T>[ushort.MaxValue];
		return SubTables;
	}

}