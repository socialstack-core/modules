using System;
using System.Collections;
using System.Collections.Generic;
namespace Api.CanvasRenderer;


/// <summary>
/// A datamap entry in a canvas generator.
/// </summary>
public class CanvasGeneratorMapEntry
{
	/// <summary>
	/// The ID of this entry. Used by pointers.
	/// </summary>
	public uint Id;
	/// <summary>
	/// The graph node that will ultimately generate this data.
	/// </summary>
	public Executor GraphNode;
	/// <summary>
	/// Output field from the graph node.
	/// </summary>
	public string Field;
	
}