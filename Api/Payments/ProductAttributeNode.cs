using System.Collections.Generic;

namespace Api.Payments;


/// <summary>
/// A product attribute in the cache tree.
/// </summary>
public class ProductAttributeGroupNode
{
	/// <summary>
	/// The attribute group, if this node is one.
	/// </summary>
	public ProductAttributeGroup Group;
	
	/// <summary>
	/// Local field used for building structure in memory
	/// </summary>
	public List<ProductAttributeGroupNode> Children { get; set; } = new();
	
	/// <summary>
	/// Local field used for building structure in memory
	/// </summary>
	public List<ProductAttributeNode> Attributes { get; set; } = new();
	
	/// <summary>
	/// Local fields used for building category structure in memory
	/// </summary>
	public ProductAttributeGroupNode Parent { get; set; }

	/// <summary>
	/// Local field exposing fully expanded slug for the category (based on category structure)
	/// </summary>
	public string FullPathSlug { get; set; }
}

/// <summary>
/// A product attribute in the cache tree.
/// </summary>
public class ProductAttributeNode
{
	/// <summary>
	/// The attribute, if this node is one.
	/// </summary>
	public ProductAttribute Attribute;
	
	/// <summary>
	/// Local fields used for building category structure in memory
	/// </summary>
	public ProductAttributeGroupNode Parent { get; set; }
	
	// values probably!
}