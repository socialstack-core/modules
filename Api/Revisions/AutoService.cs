using System;
using System.Threading.Tasks;
using Api.Contexts;
using Api.Database;
using Api.Revisions;
using Api.Users;


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
	/// <summary>
	/// Computes the revision diff between two chronologically sequential revisions of content,
	/// returning only the fields that changed and are accessible to the caller.
	/// </summary>
	/// <param name="context">
	/// The <see cref="Context"/> used during revision retrieval and content deserialization.
	/// </param>
	/// <param name="contentId">
	/// The unique identifier of the content in a service-agnostic format.
	/// Will be converted to the format used by this service via <see cref="ConvertId"/>.
	/// </param>
	/// <param name="revisionId">
	/// The revision number of the current state to compare. The previous state is retrieved as <c>revisionId - 1</c>.
	/// </param>
	/// <param name="isDelete">
	/// A flag indicating whether this diff represents a deletion. Default is <c>false</c>.
	/// When <c>true</c>, the diff represents [previous content, null].
	/// When <c>false</c>, the diff represents [null, current content] for creations.
	/// Ignored when both previous and current content exist.
	/// </param>
	/// <returns>
	/// A <see cref="RevisionDiff{T, ID}"/> containing the changed fields and their before/after values.
	/// Only fields that were modified and are accessible to the caller are included in the result.
	/// For creations (previous is null), the "previous" field is null and the "next" field contains the created content.
	/// For deletions (current is null), the "previous" field contains the deleted content and the "next" field is null.
	/// For modifications, both fields contain instances with only the changed fields set.
	/// </returns>
	/// <remarks>
	/// <para>
	/// This method operates in three scenarios:
	/// <list type="number">
	/// <item>
	/// <description>
	/// <strong>Modification:</strong> Both current and previous content exist. The method identifies changed fields
	/// using <see cref="Diff"/>, creates instances containing only the modified fields, and applies field-level
	/// access control so users see only changes they are permitted to view.
	/// </description>
	/// </item>
	/// <item>
	/// <description>
	/// <strong>Deletion:</strong> Current content is null, previous content exists. The diff contains the deleted state
	/// in the "previous" field and null in the "next" field (when <paramref name="isDelete"/> is <c>true</c>).
	/// </description>
	/// </item>
	/// <item>
	/// <description>
	/// <strong>Creation:</strong> Previous content is null, current content exists. The diff contains null in the
	/// "previous" field and the created state in the "next" field (when <paramref name="isDelete"/> is <c>false</c>).
	/// </description>
	/// </item>
	/// </list>
	/// </para>
	/// <para>
	/// <strong>Field Filtering:</strong> The method implements field filtering via a Venn diagram intersection model:
	/// one circle represents all changed fields, another represents fields that should be included in output.
	/// The result contains only fields in both sets, allowing selective visibility of changes.
	/// </para>
	/// <para>
	/// <strong>Performance Consideration:</strong> For modification scenarios, new instances are created and populated
	/// via reflection to set only changed fields. This ensures efficient output serialization and prevents unintended
	/// information disclosure.
	/// </para>
	/// </remarks>
	/// <exception cref="InvalidOperationException">
	/// May be thrown during reflection operations if field metadata is invalid or if <see cref="ConvertId"/> fails.
	/// </exception>
	public override async ValueTask<RevisionDiff> GetRevisionDiff(Context context, ulong contentId, uint revisionId, bool isDelete = false)
	{
		// first, convert the content ID into the format
		// this service allows. 
		var id = ConvertId(contentId);

		var current = await Revisions.GetByRevisionNumber(context, id, revisionId);
		var previous = await Revisions.GetByRevisionNumber(context, id, revisionId - 1);

		var currentContent = FromStoredJson(current?.ContentJson);
		var previousContent = FromStoredJson(previous?.ContentJson);

		if (currentContent is null && previousContent is null)
		{
			// when both are null, write a notice, and exit early.
			Log.Error("revisions/diff", $"Couldn't calculate revision diff, both the previous and current content are null, this is an invalid state. {typeof(T).Name}(ContentId={contentId}, Revision={revisionId}, Previous={revisionId - 1})");
			return new RevisionDiff<T,ID>(currentContent, previousContent);
		}
		
		// diffs only occur when both currentContent & previousContent
		// are not null, because when one of the 2 is null, we know
		// it's all different, so we wrap this in a conditional branch as 
		// its fairly unique compared to the other paths.
		if (currentContent is not null && previousContent is not null)
		{
			// Get the differences.
			var fieldsChanged = Diff(currentContent, previousContent);

			// here we only set the fields
			// that have been set, this means that
			// wherever these are output,
			// field rules apply, so the user
			// sees a) changes only, b) fields they can access.
			// think of it like a venn diagram, one circle
			// you have all changes. the overlapping circle
			// is the fields the user can see, the result is
			// the middle. 
			// if a user edit has occured and been logged, 
			// things like PasswordHash or 2FA code can be omitted. 
			var beforeInstance = new T();
			var afterInstance = new T();
	
			// iterate the changed fields, simple consumption loop.
			foreach (var change in fieldsChanged)
			{
				// use the field so we can use reflection
				// to set the value. 
				var field = change.TargetField;

				field.SetValue(beforeInstance, field.GetValue(previousContent));
				field.SetValue(afterInstance, field.GetValue(currentContent));
			}
			
			// return the calc'd diff. 
			return new RevisionDiff<T,ID>(beforeInstance, afterInstance);
		}
		
		// This handles the other states. 
		// [prev = {object}, current = null] <= Deleted
		// [prev = null, current = {object}] <= Created
		return new RevisionDiff<T,ID>(isDelete ? currentContent : previousContent, isDelete ? null : currentContent);
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
	/// <summary>
	/// Exists for contract satisfaction reasons, see AutoService&lt;T,ID&gt;::GetRevisionDiff.
	/// </summary>
	public virtual ValueTask<RevisionDiff> GetRevisionDiff(Context ctx, ulong contentId, uint revisionId, bool isDelete = false)
	{
		throw new NotImplementedException();
	}
}