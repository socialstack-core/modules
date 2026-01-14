using Api.Contexts;
using Api.Database;
using Api.Startup;
using System;

namespace Api.Permissions;


/// <summary>
/// A field grant rule (either read or write, not both) for a particular field.
/// </summary>
public class FieldGrantRule<T, ID>
	where T : Content<ID>, new()
	where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
{	
	/// <summary>
	/// The field this rule is for.
	/// </summary>
	public JsonField<T, ID> Field;
	
	/// <summary>
	/// A full filter to check.  Check if this is not null.
	/// </summary>
	public Filter<T, ID> Filter;
	
	/// <summary>
	/// If inherit is false and filter is null, use this.
	/// </summary>
	public bool ConstGrant;


	/// <summary>
	/// Creates a new field grant rule for the given grant text.
	/// </summary>
	/// <param name="field"></param>
	/// <param name="text"></param>
	/// <param name="fallback"></param>
	public FieldGrantRule(JsonField<T, ID> field, string text, string fallback)
	{
		Field = field;

		if (string.IsNullOrEmpty(text))
		{
			// Inherit (fallback).
			text = fallback;
		}
		
		if (string.IsNullOrEmpty(text) || text == "true")
		{
			ConstGrant = true;
		}
		else if (text == "false")
		{
			ConstGrant = false;
		}
		else
		{
			// will be a full filter obj
			Filter = field.Structure.Service.GetFilterFor(text);
		}
	}

	/// <summary>
	/// True if the rule is granted.
	/// </summary>
	public bool IsGranted(Context context, T obj, ContextFlags flags = ContextFlags.None)
	{
		if(Filter != null)
		{
			return Filter.Match(context, obj, flags);
		}
		
		return ConstGrant;
	}
}