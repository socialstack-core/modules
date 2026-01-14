using Api.Contexts;
using Api.Startup;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Api.AuditEvents;

/// <summary>Handles auditEvent endpoints.</summary>
[Route("v1/auditEvent")]
public partial class AuditEventController : AutoController<AuditEvent>
{


	/// <summary>
	/// Detailed audit log search. Note that MongoDB is 
	/// currently required for this otherwise it will return limited results.
	/// </summary>
	/// <returns></returns>
	[HttpPost("detailed")]
	public async ValueTask<ContentStream<AuditEvent, uint>?> Detailed(Context context, [FromBody] AuditLogSearchRequest request)
	{
		if (context.Role == null || !context.Role.CanViewAdmin)
		{
			return null;
		}

		var resultSet = await (_service as AuditEventService).DetailedSearch(
			context,
			request
		);

		if (resultSet == null)
		{
			return null;
		}

		return new ContentStream<AuditEvent, uint>(resultSet.Events, _service, resultSet.TotalResults) {
			IncludeTotal = true
		};
	}

	/// <summary>
	/// The available content types that can be used for filtering.
	/// </summary>
	/// <returns></returns>
	[HttpGet("available-types")]
	public async ValueTask<AvailableAuditTypes?> AvailableTypes(Context context)
	{
		if (context.Role == null || !context.Role.CanViewAdmin)
		{
			// 404
			return null;
		}

		var types = await (_service as AuditEventService).GetTypesToInclude(context);

		return new AvailableAuditTypes()
		{
			Results = types
		};
	}

}

/// <summary>
/// The available content types for filtering the audit log.
/// </summary>
public struct AvailableAuditTypes
{
	/// <summary>
	/// The set of content types.
	/// </summary>
	public List<AuditEventType> Results;
}

/// <summary>
/// Fields that can be used when submitting a request to search the audit log.
/// </summary>
public struct AuditLogSearchRequest
{
	/// <summary>
	/// Restricting to specific content types.
	/// </summary>
	public List<string> ContentTypes;

	/// <summary>
	/// Filtering by specific user optionally.
	/// </summary>
	public uint? UserId;

	/// <summary>
	/// Starting date. If not specified it's Now-Range.
	/// </summary>
	public DateTime? StartUtc;

	/// <summary>
	/// Search range in minutes relative to startUtc, max 1d (1440). If not specified it's 1d.
	/// </summary>
	public int? Range;

	/// <summary>
	/// Page offset.
	/// </summary>
	public int PageIndex;

	/// <summary>
	/// The page size.
	/// </summary>
	public int PageSize;

	/// <summary>
	/// The sort order.
	/// </summary>
	public LogSortOrder SortOrder;
}