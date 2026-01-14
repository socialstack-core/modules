using System;
using Api.AutoForms;
using Api.Database;
using Api.Startup;
using Api.Translate;
using Api.Users;
using Newtonsoft.Json;


namespace Api.Payments
{

	/// <summary>
	/// A ProductAttribute
	/// </summary>
	[ListAs("RequiredAttributes", Explicit = true)]
	[ImplicitFor("RequiredAttributes", typeof(ProductCategory))]
	[ImplicitFor("RequiredAttributes", typeof(ProductTemplate))]
	[HasVirtualField("attributeGroup", typeof(ProductAttributeGroup), "ProductAttributeGroupId")]
	public partial class ProductAttribute : VersionedContent<uint>
	{
        /// <summary>
        /// The name of the product attribute
        /// </summary>
        [DatabaseField(Length = 200)]
        [Data("required", true)]
        [Data("validate", "Required")]
		public Localized<string> Name;

		/// <summary>
		/// Attribute key used for identifying it during installation of default values or through name changes.
		/// </summary>
		[Data("readonly", true)]
		public string Key;

		/// <summary>
		/// The group that this attribute is in.
		/// </summary>
		[Data("required", true)]
		[Data("validate", "Required")]
		public uint ProductAttributeGroupId;

		/// <summary>
		/// The field type. 1=long, 2=double, 3=string, 4=image ref, 5=video ref, 6=file ref, 7=boolean ("true" or "false" are the only valid values). 
		/// Don't store prices in attributes. You should instead create multiple product 
		/// variants and each one has its own potentially localised price.
		/// </summary>
		[Module("Admin/Payments/AttributeTypes")]
		[Data("required", true)]
		[Data("validate", "Required")]
		public int ProductAttributeType;

		/// <summary>
		/// 0 if this attribute can't range, 1 if it always does, 2 if it optionally can.
		/// Sometimes a product might only need a "max load" rather than a min one for example.
		/// </summary>
		[Module("Admin/Payments/AttributeRangeTypes")]
		public int RangeType;

		/// <summary>
		/// true if this attribute can have more than one value.
		/// </summary>
		[Data("help", "Tick this if the attribute can be given more than one value")]
		public bool Multiple;

		/// <summary>
		/// e.g. "mm", "cm", "kg", "months", "years". Metric units, primary lowercase (except when SI unit conventions state otherwise) and short form. 
		/// If imperial units is desired, the UI should convert them.
		/// </summary>
		[Module("Admin/Payments/AttributeUnits")]
		public string Units;

		/// <summary>
		/// The feature image ref to allow for colour swatches etc 
		/// </summary>
		[DatabaseField(Length = 300)]
		public string FeatureRef;		

		/// <summary>
		/// Not present most of the time. Temporary group key used to identify the parent when the parents ID is unknown, such as during installation.
		/// </summary>
		[JsonIgnore]
		public string ProductAttributeGroupKey { get; set; }


	}

}