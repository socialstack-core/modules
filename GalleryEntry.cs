using System;
using System.Collections.Generic;
using Api.Database;
using Api.Users;


namespace Api.GalleryEntries
{
	
	/// <summary>
	/// An entry within a particular gallery.
	/// </summary>
	public partial class GalleryEntry : RevisionRow
	{
		/// <summary>
		/// The gallery this is in.
		/// </summary>
		public int GalleryId;

		/// <summary>
		/// The ID of the page that this entry appears on when viewed standalone.
		/// </summary>
		public int PageId;
		
		/// <summary>
		/// The name of the gallery in the site default language.
		/// </summary>
		[DatabaseField(Length = 200)]
		public string Name;

		/// <summary>
		/// Optional description of this gallery entry.
		/// </summary>
		[DatabaseField(Length = 500)]
		public string Description;

		/// <summary>
		/// The image ref. See also: "Upload.Ref" in the Uploads module.
		/// </summary>
		[DatabaseField(Length = 80)]
		public string ImageRef;

	}
	
}