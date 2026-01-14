using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using System;
using Api.Users;

namespace Api.Revisions
{
	/// <summary>
	/// A typeless revision service.
	/// </summary>
	public partial interface RevisionService
    {
		/// <summary>
		/// Generic publishing by ulong ID.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		/// <param name="options"></param>
		/// <returns></returns>
		ValueTask<object> PublishGenericId(Context context, ulong id, DataOptions options = DataOptions.Default);

		/// <summary>
		/// Gets the entity name, of the form "x_revisions".
		/// </summary>
		/// <returns></returns>
		string GetEntityName();
	}

	/// <summary>
	/// Revision service for a particular type.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <typeparam name="ID"></typeparam>
	public class RevisionService<T, ID> : AutoService<Revision<T, ID>, ID>, RevisionService
		where T : Content<ID>, new()
		where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
	{

		/// <summary>
		/// The parent service.
		/// </summary>
		public AutoService<T, ID> Parent;

		/// <summary>
		/// Instances as revision service for a particular type. See also: aService.Revisions
		/// </summary>
		public RevisionService(AutoService<T, ID> parent) : base(new EventGroup<Revision<T, ID>, ID>(), null, parent.EntityName + "_revisions")
		{
			Parent = parent;
		}

		/// <summary>
		/// Gets the entity name, of the form "x_revisions".
		/// </summary>
		/// <returns></returns>
		public string GetEntityName()
		{
			return EntityName;
		}

		/// <summary>
		/// Gets the revision service on this autoservice, if there is one.
		/// </summary>
		/// <returns></returns>
		public override RevisionService GetRevisions()
		{
			return this;
		}

		/// <summary>
		/// Gets the latest draft (or null if none) of the given content ID, newer than the specified date.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="sinceDate"></param>
		/// <param name="contentId"></param>
		/// <returns></returns>
		public async ValueTask<Revision<T, ID>> GetNewerDraft(Context context, DateTime sinceDate, ID contentId)
		{
			var filter = Where("ContentId=? and IsDraft=? and CreatedUtc>?")
				.Bind(contentId)
				.Bind(true)
				.Bind(sinceDate);
			filter.Sort("CreatedUtc", false);
			return await filter.First(context);
		}

		/// <summary>
		/// Publish a revision by a generic revision ID.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		/// <param name="options"></param>
		/// <returns></returns>
		public async ValueTask<object> PublishGenericId(Context context, ulong id, DataOptions options = DataOptions.Default)
		{
			return await Publish(context, ConvertId(id), options);
		}

		/// <summary>
		/// Publish a revision by a revision ID.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		/// <param name="options"></param>
		/// <returns></returns>
		public async ValueTask<T> Publish(Context context, ID id, DataOptions options = DataOptions.Default)
		{
			var content = await Get(context, id);
			return await PublishRevision(context, content, options);
		}

		/// <summary>
		/// Publishes the given revision. The entity content ID may not exist at all.
		/// </summary>
		public virtual async ValueTask<T> PublishRevision(Context context, Revision<T, ID> rev, DataOptions options = DataOptions.Default)
		{
			var val = Parent.FromStoredJson(rev.ContentJson);

			if (val == null)
			{
				return null;
			}

			val.Id = rev.ContentId;

			T result = null;

			if (val.Id.Equals(default(ID)))
			{
				// It has not been created at all yet.
				result = await Parent.Create(context, val, options);
			}
			else
			{
				// This is actually an update on the row.
				result = await Parent.UpdateExact(context, val, options);
			}

			if (result == null)
			{
				return null;
			}

			// Mark the revision as no longer a draft and set its publish time.
			await Update(context, rev, (Context ctx, Revision<T, ID> toUpdate, Revision<T, ID> orig) => {
				toUpdate.PublishDraftDate = DateTime.UtcNow;
				toUpdate.IsDraft = false;
			}, options);

			return result;
		}

	}

}
