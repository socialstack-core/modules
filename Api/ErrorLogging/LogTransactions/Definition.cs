using Api.SocketServerLibrary;
using System.Collections.Generic;

namespace Api.ErrorLogging;

/// <summary>
/// A definition of something. Can inherit other definitions.
/// </summary>
public partial class Definition
{
	/// <summary>
	/// ID of the definition. Simply the incremental number, starting from 1, of the definition as seen in the blockchain defined order.
	/// </summary>
	public ulong Id;

	/// <summary>
	/// Parent schema
	/// </summary>
	public Schema Schema;

	/// <summary>
	/// Inherited definition ID.
	/// </summary>
	public ulong InheritedId;
	
	/// <summary>
	/// Standard name for the name of a content type (field 1 on the ContentType type)
	/// </summary>
	public string Name;
}