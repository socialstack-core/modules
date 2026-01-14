namespace Api.Startup.Routing;

/// <summary>
/// Generalised information about a node in the routing tree.
/// </summary>
public struct RouterNodeMetadata
{
	/// <summary>
	/// "Page", "Redirect", "Rewrite", "Method", "Group"
	/// </summary>
	public string Type;
	
	/// <summary>
	/// True if this node has further children (and  therefore appears as a directory).
	/// </summary>
	public bool HasChildren;

	/// <summary>
	/// The full route to this particular node.
	/// </summary>
	public string FullRoute;
	
	/// <summary>
	/// The child key for this specific child. It's null if it is a wildcard token, representing "anything else".
	/// </summary>
	public string ChildKey;

	/// <summary>
	/// A display name.
	/// </summary>
	public string Name;

	/// <summary>
	/// An ID for an associated content object, if there is one.
	/// </summary>
	public ulong ContentId;

	/// <summary>
	/// An admin edit URL if this object has one.
	/// </summary>
	public string EditUrl;

	/// <summary>
	/// An admin create URL if this object has one.
	/// </summary>
	public string CreateUrl;
}
