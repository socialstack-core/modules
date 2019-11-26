using System;
using Api.Database;
using Api.Users;

namespace Api.Galleries
{
	
	/// <summary>
	/// A gallery.
	/// </summary>
	public partial class Gallery : RevisionRow
	{
		/// <summary>
		/// The primary ID of the page that this gallery appears on.
		/// </summary>
		public int PageId;

		/// <summary>
		/// The page ID that entries will appear on.
		/// </summary>
		public int EntryPageId;
		
		/// <summary>
		/// The name of the gallery in the site default language.
		/// </summary>
		[DatabaseField(Length = 200)]
		public string Name;

		/// <summary>
		/// A description of this gallery.
		/// </summary>
		[DatabaseField(Length = 200)]
		public string Description;

		/// <summary>
		/// The feature image ref. See also: "Upload.Ref" in the Uploads module.
		/// </summary>
		[DatabaseField(Length = 80)]
		public string FeatureRef;

		/// <summary>
		/// The icon ref. See also: "Upload.Ref" in the Uploads module.
		/// </summary>
		[DatabaseField(Length = 80)]
		public string IconRef;
	}
	
}