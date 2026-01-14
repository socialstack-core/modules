using Api.Database;
using Api.Startup;
using Newtonsoft.Json;
using System;
using System.Runtime.Intrinsics.Arm;

namespace Api.NavMenus
{
	
	/// <summary>
	/// A particular entry within a navigation menu.
	/// </summary>
	public partial class AdminNavMenuItem : Content<uint>
	{
		/// <summary>
		/// The title of this nav menu entry.
		/// </summary>
		[DatabaseField(Length = 200)]
		public string Title;
		
		/// <summary>
		/// The Page key to target.
		/// </summary>
		public string PageKey;
		
		/// <summary>
		/// The target URL.
		/// </summary>
		public string Url;

		/// <summary>
		/// Optional image to show with this item.
		/// </summary>
		[DatabaseField(Length = 100)]
		public string IconRef;
		
		/// <summary>
		/// The Admin nav menu item key (is unique)
		/// </summary>
		public string Key;
		
		/// <summary>
		/// Parent node of this item.
		/// </summary>
		public uint ParentId;

		/// <summary>
		/// The content type present on this page (if any).
		/// </summary>
		/// <exception cref="PublicException"></exception>
		[JsonIgnore]
		public Type PageContentType
		{
			get {
				return PageContentService?.ServicedType;
			}
		}
		
		/// <summary>
		/// The service which handles the content type for this page (if any).
		/// </summary>
		/// <exception cref="PublicException"></exception>
		[JsonIgnore]
		public AutoService PageContentService
		{
			get
			{
				if (string.IsNullOrEmpty(PageKey))
				{
					return null;
				}

				if (!PageKey.Contains(':'))
				{
					return null;
				}
				var type = PageKey?.Split(":")[1];

				if (string.IsNullOrEmpty(type))
				{
					throw new PublicException($"Invalid PageKey found, {PageKey} is invalid",
						"admin-nav-menu-item/bad-page-key");
				}

				var svc = Services.Get(type + "Service");
				return svc;
			}
		}
	}

}