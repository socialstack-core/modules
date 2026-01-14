using Api.AutoForms;
using Api.Database;
using Api.Translate;
using Api.Users;

namespace Api.Payments
{
    /// <summary>
    /// Base class for content categories.
    /// </summary>
    public abstract partial class BaseCategory : VersionedContent<uint>
    {
        /// <summary>
        /// The name of the product category
        /// </summary>
        [DatabaseField(Length = 200)]
        [Data("required", true)]
        [Data("validate", "Required")]
		public Localized<string> Name;

        /// <summary>
        /// The slug for product category
        /// </summary>
        [DatabaseField(Length = 1000)]
		[Data("readonly", true)]
		public string Slug;

        /// <summary>
        /// The description of this product category.
        /// </summary>
        [DatabaseField(Length = 200)]
        [Data("required", true)]
        [Data("validate", "Required")]
        public Localized<string> DescriptionHtml;

        /// <summary>
        /// The category image ref
        /// </summary>
        [DatabaseField(Length = 300)]
        [Data("required", true)]
        [Data("validate", "Required")]
        public string FeatureRef;

		/// <summary>
		/// Optional product image overlay (square aspect ratio with transparent background recommended)
		/// </summary>
		[DatabaseField(Length = 300)]
		[Data("hint", "Square aspect ratio with transparent background recommended")]
		public string ProductImageRef;

		/// <summary>
		/// Set true to replace all white areas of the image (typically background) to transparent
		/// </summary>
		[Data("hint", "Set true to replace all white areas of the image (typically background) to transparent")]
		public bool ReplaceWhiteWithTransparency;

		/// <summary>
		/// Optionally nudge product image horizontally
		/// </summary>
		public int? ProductImageHorizontalOffset;

		/// <summary>
		/// Optionally nudge product image vertically
		/// </summary>
		public int? ProductImageVerticalOffset;

		/// <summary>
		/// Optional icon to show with this item.
		/// </summary>
		[DatabaseField(Length = 300)]
        [Data("type", "icon")]
        [Data("required", true)]
        [Data("validate", "Required")]
        public string IconRef;

        /// <summary>
        /// The optional parent id
        /// </summary>
        public uint? ParentId;

		/// <summary>
		/// Is this part of the the primary category for a product
		/// </summary>
		public bool IsPrimary;
    }
}
