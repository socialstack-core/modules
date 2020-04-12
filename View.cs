using System;
using Api.Database;
using Api.Users;

namespace Api.Views
{
	/// <summary>
	/// A view by a particular user to a particular piece of content.
	/// ViewCount is essentially just a counted version of these.
	/// </summary>
	public partial class View : RevisionRow
	{
		/// <summary>
		/// The content type this is a view to.
		/// </summary>
		public int ContentTypeId;
		/// <summary>
		/// The Id of the content that this is a view to.
		/// </summary>
		public int ContentId;
	}
	
}