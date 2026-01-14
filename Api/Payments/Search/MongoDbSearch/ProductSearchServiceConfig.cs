using Api.Configuration;
using Api.Translate;
using System.Collections.Generic;

namespace Api.Payments;

/// <summary>
/// Configurations used by the product search service.
/// </summary>
public partial class ProductSearchServiceConfig : Config
{
	
	/// <summary>
	/// The name of the index on atlas to use.
	/// </summary>
	public string AtlasIndex {get; set;} = "default";
	
}