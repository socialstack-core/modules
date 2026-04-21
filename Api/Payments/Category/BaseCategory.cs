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
        public Localized<string> DescriptionHtml;

        /// <summary>
        /// The category image ref
        /// </summary>
        [DatabaseField(Length = 300)]
		[Data("hint", "Used for full background images in banners, recommended wide aspect ratio")]
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
		/// Optional icon to show with this item.
		/// </summary>
		[DatabaseField(Length = 300)]
        [Data("type", "icon")]
        public string IconRef;

        /// <summary>
        /// The optional parent id
        /// </summary>
        public uint? ParentId;

		/// <summary>
		/// Is this part of the the primary category for a product
		/// </summary>
		public bool IsPrimary;

		/// <summary>
		/// Is this category hidden, normally for use with seasonal secondary categories
		/// </summary>
		public bool IsHidden;

		/// <summary>
		/// Is this category to be used as a landing page for sub categories, normally only for secondary categories
		/// </summary>
		[Data("hint", "Set true to indicate that the category page should show sub categories rather than products (normally secondary categories")]
		public bool IsLandingPage;

	}
}
