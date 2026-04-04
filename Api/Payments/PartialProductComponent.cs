using Newtonsoft.Json.Linq;

namespace Api.Payments;


/// <summary>
/// Exists to hold a partial product as seen in the Admin/Payments/ProductComponents/ValueEditor.
/// It's a special class as it can also contain the Delete flag.
/// </summary>
public class PartialProductComponent
{
	/// <summary>
	/// The ID of the product if one already exists and this is an edit. It's blank if this is a creation.
	/// </summary>
	public uint Id;

	/// <summary>
	/// The (partial) product info. May contain no fields at all aside from e.g. Id.
	/// </summary>
	public JObject ProductQuantity;
	
	/// <summary>
	/// True if this component should be deleted. Id is required otherwise the entry will just be ignored. 
	/// </summary>
	public bool DeleteComponent;

	/// <summary>
	/// True if this component should be updated (new qty). Id is required otherwise the entry will just be ignored. 
	/// </summary>
	public bool UpdateComponent;

}