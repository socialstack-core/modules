using System.Collections.Generic;

namespace Api.Startup;


/// <summary>
/// A filter description.
/// </summary>
public class ListFilter
{
	/// <summary>
	/// Page size. If zero, there is no limit.
	/// </summary>
	public int PageSize;
	
	/// <summary>
	/// Page number if paginated.
	/// </summary>
	public int PageIndex;
	
	/// <summary>
	/// A filter query.
	/// </summary>
	public string Query;
	
	/// <summary>
	/// Optional sort config.
	/// </summary>
	public FilterSortConfig? Sort;

	/// <summary>
	/// Indicates if the total result count should be included (on paginated queries).
	/// </summary>
	public bool? IncludeTotal;

	/// <summary>
	/// The arguments set for the query string.
	/// </summary>
	public List<object> Args;
}

/// <summary>
/// Sort config.
/// </summary>
public struct FilterSortConfig
{
	/// <summary>
	/// Field to sort by.
	/// </summary>
	public string Field;
	
	/// <summary>
	/// Sort direction, either 'asc' or 'desc'. 'asc' assumed if not specified.
	/// </summary>
	public string Direction;
}