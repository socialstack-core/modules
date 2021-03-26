using Api.AutoForms;
using Api.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Users
{	
	/// <summary>
	/// Use this to get a UserId, CreatedUtc and EditedUtc with automatic creator user field support, which is also capable of revisions.
	/// Alternatively use DatabaseRow directly if you want total control over your table.
	/// </summary>
	public abstract class RevisionEntity<T> : UserCreatedEntity<T> where T:struct
	{
		/// <summary>
		/// The revision number of a particular piece of content. Starts at 1 and goes up linearly.
		/// </summary>
		[Module(Hide = true)]
		public int Revision = 1;
		
		/// <summary>
		/// This is only set if you have a revision object of the content. This is always null for the latest content.
		/// This is unique within all revisions for a particular type. It's the row ID for the revisions table, and doesn't exist at all in the main type table.
		/// </summary>
		protected T? _RevisionId;

		/// <summary>
		/// This is only set if you have a revision object of the content. This is always null for the latest content (what you'll have most of the time).
		/// </summary>
		[Module(Hide = true)]
		public T? RevisionId{
			get {
				return _RevisionId;
			}
			set {
				_RevisionId = value;
			}
		}

		/// <summary>
		/// This is true if this revision is a draft. It's false if you don't have a revision object.
		/// </summary>
		protected bool _IsDraft;

		/// <summary>
		/// This is true if this revision is a draft. It's false if you don't have a revision object.
		/// </summary>
		[Module(Hide = true)]
		public bool IsDraft
		{
			get
			{
				return _IsDraft;
			}
			set
			{
				_IsDraft = value;
			}
		}
	}
}
