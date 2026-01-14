


using Api.Database;
using Api.Startup;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Api.Contexts;

/// <summary>
/// Holds useful information about Context objects.
/// </summary>
public static class ContextFields
{

	/// <summary>
	/// Fields by the shortcode, which is usually the first character of a context field name.
	/// </summary>
	public static readonly ContextFieldInfo[] FieldsByShortcode = new ContextFieldInfo[64];

	/// <summary>
	/// Maps lowercase field names to the info about them.
	/// </summary>
	public static readonly Dictionary<string, ContextFieldInfo> Fields = new Dictionary<string, ContextFieldInfo>();

	/// <summary>
	/// The raw list of fields.
	/// </summary>
	public static readonly List<ContextFieldInfo> FieldList = new List<ContextFieldInfo>();

	/// <summary>
	/// Maps a content type ID to the context field info. Your context property must end with 'Id' to get an entry here.
	/// </summary>
	public static readonly Dictionary<int, ContextFieldInfo> ContentTypeToFieldInfo = new Dictionary<int, ContextFieldInfo>();

}
