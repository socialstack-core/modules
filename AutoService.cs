using Api.Contexts;
using Api.Database;
using Api.Permissions;
using Api.Revisions;
using Api.Startup;
using Api.Users;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;


/// <summary>
/// A general use service which manipulates an entity type. In the global namespace due to its common use.
/// Deletes, creates, lists and updates them whilst also firing off a series of events.
/// Note that you don't have to inherit this to create a service - it's just for convenience for common functionality.
/// Services are actually detected purely by name.
/// </summary>
public partial class AutoService<T, ID>{
	
	/// <summary>
	/// True if this type supports revisions.
	/// </summary>
	private bool? _isRevisionType;

	/// <summary>
	/// True if this type supports revisions.
	/// </summary>
	/// <returns></returns>
	public bool IsRevisionType()
	{
		if (_isRevisionType.HasValue)
		{
			return _isRevisionType.Value;
		}

		_isRevisionType = ContentTypes.IsAssignableToGenericType(typeof(T), typeof(VersionedContent<>));
		return _isRevisionType.Value;
	}

	// private RevisionService<T, ID> _revs;

	/// <summary>
	/// The revision service (null if this type doesn't support them).
	/// </summary>
	public RevisionService<T, ID> Revisions
	{
		get
		{
			if (!IsRevisionType())
			{
				return null;
			}

			throw new Exception("Unsafe, incomplete");
			// Because RevisionService hasn't been specialised enough yet.
			// Deleting a revision will delete the actual row.

			/*
			if (_revs == null)
			{
				_revs = new RevisionService<T, ID>(this);
			}

			return _revs;
			*/
		}
	}

}
