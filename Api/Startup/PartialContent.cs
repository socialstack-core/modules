using Api.Database;
using System.Collections.Generic;

namespace Api.Startup;


/// <summary>
/// General base class for PartialContent.
/// </summary>
public class PartialContent {
	
}

/// <summary>
/// Content which is both partially set and can have virtual fields.
/// </summary>
public class PartialContent<T, ID> : PartialContent
	where ID : struct 
	where T:Content<ID>, new()
{
	/// <summary>
	/// The content itself.
	/// </summary>
	public T Content;

	/// <summary>
	/// Virtual fields set on this piece of content.
	/// </summary>
	public List<VirtualFieldValue> VirtualFields;
}

/// <summary>
/// Content of a virtual field.
/// </summary>
public class VirtualFieldValue{
	
	/// <summary>
	/// The field name.
	/// </summary>
	public string Name;
	
	/// <summary>
	/// The field value.
	/// </summary>
	public object Value;
	
}