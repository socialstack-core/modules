using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;

namespace Api.HubSpot

{
	/// <summary>
	/// Paging block returned by HubSpot list/search endpoints.
	/// </summary>
	public class HubSpotPaging
	{
		/// <summary>
		/// Pointer for the next page (if present).
		/// </summary>
		[JsonProperty("next")]
		public HubSpotNextPage Next { get; set; }
	}

	/// <summary>
	/// Information required to fetch the next page.
	/// </summary>
	public class HubSpotNextPage
	{
		/// <summary>
		/// Opaque cursor to pass as the <c>after</c> query parameter.
		/// </summary>
		[JsonProperty("after")]
		public string After { get; set; }

		/// <summary>
		/// Optional link HubSpot may include for the next page.
		/// </summary>
		[JsonProperty("link")]
		public string Link { get; set; }
	}

	/// <summary>
	/// Wrapper for list/search responses with typed results and optional paging.
	/// </summary>
	/// <typeparam name="T">The result item type (e.g., <see cref="HubSpotEntity"/>).</typeparam>
	public class HubSpotListResponse<T>
	{
		/// <summary>
		/// The items returned by the query.
		/// </summary>
		[JsonProperty("results")]
		public List<T> Results { get; set; } = new();

		/// <summary>
		/// Pagination info for fetching subsequent pages.
		/// </summary>
		[JsonProperty("paging")]
		public HubSpotPaging Paging { get; set; }
	}

	/// <summary>
	/// Standard HubSpot error payload for non-2xx responses.
	/// </summary>
	public class HubSpotProblem
	{
		/// <summary>
		/// The status code reported by HubSpot.
		/// </summary>
		[JsonProperty("status")]
		public string Status { get; set; }

		/// <summary>
		/// Human-readable error message.
		/// </summary>
		[JsonProperty("message")]
		public string Message { get; set; }

		/// <summary>
		/// Error category (e.g., <c>VALIDATION_ERROR</c>).
		/// </summary>
		[JsonProperty("category")]
		public string Category { get; set; }

		/// <summary>
		/// Correlation ID useful for HubSpot support.
		/// </summary>
		[JsonProperty("correlationId")]
		public string CorrelationId { get; set; }

		/// <summary>
		/// Additional field-level context, if any.
		/// </summary>
		[JsonProperty("context")]
		public Dictionary<string, string[]> Context { get; set; }

		/// <summary>
		/// Helpful documentation links, if any.
		/// </summary>
		[JsonProperty("links")]
		public Dictionary<string, string> Links { get; set; }

		/// <summary>
		/// Summary of problem used in exception
		/// </summary>
		/// <returns></returns>
		public override string ToString() => $"{Status} {Category}: {Message} (corrId={CorrelationId})";
	}

	/// <summary>
	/// Exception thrown for HubSpot API errors with attached context.
	/// </summary>
	public class HubSpotApiException : Exception
	{
		/// <summary>
		/// The HTTP status code of the response.
		/// </summary>
		public HttpStatusCode StatusCode { get; }

		/// <summary>
		/// Parsed HubSpot problem detail, if available.
		/// </summary>
		public HubSpotProblem Problem { get; }

		/// <summary>
		/// HubSpot correlation id from response headers, if present.
		/// </summary>
		public string HubSpotCorrelationId { get; }

		/// <summary>
		/// Creates a new <see cref="HubSpotApiException"/>.
		/// </summary>
		public HubSpotApiException(HttpStatusCode statusCode, string message, HubSpotProblem problem = null, string corrId = null)
			: base(message) => (StatusCode, Problem, HubSpotCorrelationId) = (statusCode, problem, corrId);
	}

	/// <summary>
	/// A HubSpot entity with a dynamic property bag so custom fields require no code changes.
	/// </summary>
	public class HubSpotEntity
	{
		/// <summary>
		/// HubSpot internal object ID.
		/// </summary>
		[JsonProperty("id")]
		public string Id { get; set; }

		/// <summary>
		/// Record creation time.
		/// </summary>
		[JsonProperty("createdAt")]
		public DateTime? CreatedAt { get; set; }

		/// <summary>
		/// Last update time.
		/// </summary>
		[JsonProperty("updatedAt")]
		public DateTime? UpdatedAt { get; set; }

		/// <summary>
		/// True if the record is archived.
		/// </summary>
		[JsonProperty("archived")]
		public bool Archived { get; set; }

		/// <summary>
		/// Dynamic property bag (all configured fields land here).
		/// </summary>
		[JsonProperty("properties")]
		public Dictionary<string, JToken> Properties { get; set; } = new();

		/// <summary>
		/// Optional dynamic associations payload (kept as raw JSON).
		/// </summary>
		[JsonProperty("associations")]
		public Dictionary<string, JToken> Associations { get; set; }

		/// <summary>
		/// Convenience accessor for string properties.
		/// </summary>
		public string GetString(string name)
			=> Properties.TryGetValue(name, out var tok) && tok.Type != JTokenType.Null ? tok.ToString() : null;
	}

	/// <summary>
	/// Search request payload for <c>/crm/v3/objects/?????/search</c>.
	/// </summary>
	public class SearchRequest
	{
		/// <summary>
		/// Groups of filters. Filters are ANDed within a group, groups are ORed.
		/// </summary>
		[JsonProperty("filterGroups")]
		public List<FilterGroup> FilterGroups { get; set; } = new();

		/// <summary>
		/// Max number of results to return (default 10).
		/// </summary>
		[JsonProperty("limit")]
		public int Limit { get; set; } = 10;

		/// <summary>
		/// Optional list of properties to return for each contact.
		/// </summary>
		[JsonProperty("properties")]
		public List<string> Properties { get; set; }
	}

	/// <summary>
	/// A group of filters combined with logical AND.
	/// </summary>
	public class FilterGroup
	{
		/// <summary>
		/// Filters in this group (ANDed together).
		/// </summary>
		[JsonProperty("filters")]
		public List<Filter> Filters { get; set; } = new();
	}

	/// <summary>
	/// A single filter (e.g., propertyName=email, operator=EQ, value=jane@acme.com).
	/// </summary>
	public class Filter
	{
		/// <summary>
		/// The property to filter on (e.g., "email").
		/// </summary>
		[JsonProperty("propertyName")]
		public string PropertyName { get; set; } = default!;

		/// <summary>
		/// The operator to apply (e.g., "EQ", "CONTAINS_TOKEN").
		/// </summary>
		[JsonProperty("operator")]
		public string Operator { get; set; } = default!;

		/// <summary>
		/// The value to compare against.
		/// </summary>
		[JsonProperty("value")]
		public string Value { get; set; } = default!;
	}

}
