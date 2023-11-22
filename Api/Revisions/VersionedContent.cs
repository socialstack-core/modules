using Api.AutoForms;
using System;

namespace Api.Users
{	
	/// <summary>
	/// Use this to get a UserId, CreatedUtc and EditedUtc with automatic creator user field support, which is also capable of revisions.
	/// Alternatively use DatabaseRow directly if you want total control over your table.
	/// </summary>
	public abstract partial class VersionedContent<T>
	{
        /// <summary>
        /// If populated then auto publish the content on the required date 
        /// </summary>
        protected DateTime? _PublishDraftDate;

        /// <summary>
        /// If populated then auto publish the content on the required date 
        /// </summary>
        [Module(Hide = true)]
        public DateTime? PublishDraftDate
        {
            get
            {
                return _PublishDraftDate;
            }
            set
            {
                _PublishDraftDate = value;
            }
        }
    }
}
