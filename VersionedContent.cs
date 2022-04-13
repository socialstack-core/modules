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
	public abstract partial class VersionedContent<T> : UserCreatedContent<T> where T:struct
	{
	}
}
