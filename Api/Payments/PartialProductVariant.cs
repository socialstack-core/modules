using Newtonsoft.Json.Linq;

namespace Api.Payments;


/// <summary>
/// Exists to hold a partial product as seen in the Admin/Payments/Variants/ValueEditor.
/// It's a special class as it can also contain the Delete flag.
/// </summary>
public class PartialProductVariant
{
	/// <summary>
	/// The ID of the product if one already exists and this is an edit. It's blank if this is a creation.
	/// </summary>
	public uint Id;

	/// <summary>
	/// The (partial) product info. May contain no fields at all aside from e.g. Id.
	/// </summary>
	public JObject Product;
	
	/// <summary>
	/// True if this variant should be deleted. Id is required otherwise the entry will just be ignored. 
	/// This is such that saving the parent product is what triggers variants to update in one block. 
	/// </summary>
	public bool DeleteVariant;
	
}