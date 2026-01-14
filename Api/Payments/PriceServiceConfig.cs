using Api.Configuration;
using Api.Translate;
using System.Collections.Generic;

namespace Api.Payments;

/// <summary>
/// Configurations used by the purchase service.
/// </summary>
public class PriceServiceConfig : Config
{
	/// <summary>
	/// Tax configuration. The key is the ISO-1366 code identifying a particular region. You may use any ISO-1366 extension, such as 
	/// ISO-1366-2:US for USA states as well ("US-CA" etc). Always uppercase.
	/// </summary>
	public Dictionary<string, TaxConfiguration> Tax { get; set; }
}

/// <summary>
/// A particular tax config for a jurisdiction.
/// </summary>
public class TaxConfiguration
{
	
	/// <summary>
	/// The name of the tax.
	/// </summary>
	public string TaxName {get; set;} = "VAT";
	
	/// <summary>
	/// 0-100% rate for tax in this jurisdiction.
	/// </summary>
	public double Multiplier {get;set;}
	
}

