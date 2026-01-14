using System;
using System.Collections.Generic;

namespace Api.AuditEvents;

/// <summary>
/// Searches for audit log entries.
/// </summary>
public class AuditLogSearch
{
	/// <summary>
	/// The config for the search.
	/// </summary>
	public AuditLogSearchRequest Config;

	/// <summary>
	/// The audit events which can be mapped from revisions ("virtual"). These mapped ones are identified by having a 0 ID.
	/// </summary>
	public List<AuditEvent> Events;

	/// <summary>
	/// True if the search was handled.
	/// </summary>
	public bool Handled;

	/// <summary>
	/// All available content types.
	/// </summary>
	public List<AuditEventType> ContentTypes;

	/// <summary>
	/// Total result count.
	/// </summary>
	public int TotalResults;
}

/// <summary>
/// A struct representing the required payload for ordering a collection
/// </summary>
public struct LogSortOrder
{
	/// <summary>
	/// The field to order by
	/// </summary>
	public string Field;

	/// <summary>
	/// The sort direction
	/// </summary>
	public LogSortDirection Direction;
}

/// <summary>
/// The sort direction as an enum.
/// </summary>
public enum LogSortDirection
{
	/// <summary>
	/// Ascending
	/// </summary>
	ASC,
	/// <summary>
	/// Descending
	/// </summary>
	DESC
}
