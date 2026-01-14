using Api.Payments;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;

namespace Api.Eventing
{

	/// <summary>
	/// Specialised event group for the Product search filtering event type.
	/// </summary>
	public partial class ProductEventGroup : EventGroup<Product>
	{
		/// <summary>
		/// Called when running an atlas search for products to allow for filters to be amended.
		/// </summary>
		public EventHandler<List<BsonDocument>, ProductSearch> SearchFilter;

		/// <summary>
		/// Called when running an atlas search for products to allow for custom SHOULD clauses to be added.
		/// </summary>
		public EventHandler<BsonArray, ProductSearch> SearchShould;

		/// <summary>
		/// Called when running an atlas search for products to allow for custom MUST clauses to be added.
		/// </summary>
		public EventHandler<BsonArray, ProductSearch> SearchMust;

		/// <summary>
		/// Called when running an non atlas (basic) search for products to allow for filters to be amended.
		/// </summary>
		public EventHandler<List<FilterDefinition<Product>>, ProductSearch> BasicSearchFilter;
	}

}