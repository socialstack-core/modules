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
	/// A group of product attributes. For example, "Dimensions".
	/// Can be nested in a tree, and a group can be added to multiple parents.
	/// Exists only for the admin panel.
	/// </summary>
	public partial class ProductAttributeGroup : VersionedContent<uint>
	{
		/// <summary>
		/// A key for the group based on the name. Used to identify generated groups.
		/// </summary>
		public string Key;

		/// <summary>
		/// Groups are in 1 or 0 parents. Unparented groups appear at the root. 
		/// This eliminates risks of orphaning and keeps the structure straight forward.
		/// </summary>
		public uint ParentGroupId;

        /// <summary>
        /// The name of the attribute group e.g. "Dimensions".
        /// </summary>
        [DatabaseField(Length = 200)]
        [Data("required", true)]
        [Data("validate", "Required")]
		public Localized<string> Name;

		/// <summary>
		/// Not available most of the time. A temporary lookup mechanism used by group installation.
		/// </summary>
		[JsonIgnore]
		public string ParentGroupKey { get; set; }
	}

}