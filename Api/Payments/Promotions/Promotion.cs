using System;
using System.Collections.Generic;
using Api.AutoForms;
using Api.Database;
using Api.Pages;
using Api.Startup;
using Api.Translate;
using Api.Users;


namespace Api.Payments
{
	
	/// <summary>
	/// A Promotion
	/// </summary>
	public partial class Promotion : VersionedContent<uint>
	{
		
        /// <summary>
        /// The name of the promotion
        /// </summary>
		public string Name;

		/// <summary>
		/// Link CTA (optional)
		/// </summary>
		public string Cta;

		/// <summary>
		/// Target
		/// </summary>
		public string TargetUrl;

		/// <summary>
		/// Promo description.
		/// </summary>
		public string Description;

		/// <summary>
		/// The feature image ref
		/// </summary>
		[DatabaseField(Length = 80)]
		public string FeatureRef;

		/// <summary>
		/// Is this promotion currently active?
		/// </summary>
		public bool IsActive;

		/// <summary>
		/// Earliest date this promotion should be displayed
		/// </summary>
		public DateTime? StartDate;

		/// <summary>
		/// Latest date this promotion should be displayed
		/// </summary>
		public DateTime? EndDate;

		/// <summary>
		/// Configuration for where this promotion appears.
		/// </summary>
		[Module("Admin/Payments/Promotion/Editor")]
		public string PlacementSitesJson;

		/// <summary>
		/// Sticker title (lozenge offer text)
		/// </summary>
		public string StickerTitle;

		/// <summary>
		/// Sticker description (e.g. "Promotion ends on 31.05.25")
		/// </summary>
		public string StickerDescription;
		
	}

}