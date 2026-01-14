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

	/// <summary>
	/// The revision service (null if this type doesn't support them or hasn't been setup yet).
	/// </summary>
	public RevisionService<T, ID> Revisions;

	/// <summary>
	/// Gets the revision service on this autoservice, if there is one.
	/// </summary>
	/// <returns></returns>
	public override RevisionService GetRevisions()
	{
		return Revisions;
	}

}

public partial class AutoService
{

	/// <summary>
	/// Gets the revision service on this autoservice, if there is one.
	/// </summary>
	/// <returns></returns>
	public virtual RevisionService GetRevisions()
	{
		return null;
	}

}