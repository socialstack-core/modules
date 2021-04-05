using Api.Contexts;
using Api.Startup;
using Api.Users;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Api.AutoForms;


namespace Api.Database
{
	/// <summary>
	/// Used to represent an entity which can either be stored in the cache only or in the database.
	/// By default, unless you specify [CacheOnly] on your type, the entity will be stored in the database.
	/// A database table will always have the columns defined here as fields.
	/// Will often be ContentType{int}
	/// </summary>
	/// <typeparam name="ID">The type of ID of your entity. Usually int.</typeparam>
	public abstract partial class Content<ID> : IHaveId<ID> where ID : struct
	{
		/// <summary>
		/// The row ID.
		/// </summary>
		[DatabaseIndex]
		[DatabaseField(AutoIncrement = true)]
		[Module(Hide = true)]
		public ID Id;

		/// <summary>
		/// The name of the type. Can be used to obtain the content ID.
		/// </summary>
		[Module(Hide = true)]
		public string Type {
			get {
				return GetType().Name;
			}
		}

		/// <summary>
		/// Gets the ID of this row.
		/// </summary>
		/// <returns></returns>
		public ID GetId()
		{
			return Id;
		}

		/// <summary>
		/// Sets the ID of this row.
		/// </summary>
		/// <returns></returns>
		public void SetId(ID id)
		{
			Id = id;
		}
	}
	
	/// <summary>
	/// Gets content generically by its content type ID.
	/// </summary>
	public static class Content
	{
		/// <summary>
		/// Updates a piece of generic content.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="content"></param>
		/// <param name="options"></param>
		/// <returns></returns>
		public static async Task<object> Update(Context context, object content, DataOptions options = DataOptions.Default)
		{
			if (content == null)
			{
				return content;
			}

			// Get the service:
			var service = Services.GetByContentType(content.GetType());

			if (service == null)
			{
				return null;
			}

			content = await service.UpdateObject(context, content, options);
			return content;
		}

		/// <summary>
		/// Gets a piece of content from only its content ID and type.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="contentTypeId"></param>
		/// <param name="contentId"></param>
		/// <param name="permCheck"></param>
		/// <param name="convertUser">Converts User objects to UserProfile if true (default).</param>
		/// <returns></returns>
		public static async Task<object> Get(Context context, int contentTypeId, uint contentId, bool permCheck = false, bool convertUser = true)
		{
			// Get the service:
			var service = Services.GetByContentTypeId(contentTypeId);

			if (service == null)
			{
				return null;
			}

			var objResult = await service.GetObject(context, contentId, permCheck ? DataOptions.Default : DataOptions.IgnorePermissions);

			// Special case for users, up until UserProfile is removed.
			if (convertUser && objResult is User)
			{
				objResult = (service as UserService).GetProfile(objResult as User);
			}

			return objResult;
		}

		/// <summary>
		/// Loads child content onto the given list of parent content, where the child content can be mixed (any type).
		/// For example, parent content objects could be messages. They have contentType and contentId which references e.g. a video, a poll, a photo etc. 
		/// Any generic "mixed" content. For each object, it asks for the contentType + contentId, then efficiently loads them in bulk.
		/// From there, it then loops through the host content again to apply the discovered objects.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="parentContents"></param>
		/// <param name="getTypeAndId"></param>
		/// <param name="applyContent"></param>
		/// <param name="convertUser"></param>
		/// <returns></returns>
		public static async Task ApplyMixed(
			Context context, IEnumerable parentContents, Func<object, ContentTypeAndId> getTypeAndId, 
			Action<object, object> applyContent, bool convertUser = true
		) {
			if (parentContents == null)
			{
				// Nothing to do.
				return;
			}

			Dictionary<int, ContentLoader> loaders = new Dictionary<int, ContentLoader>();

			foreach (var host in parentContents)
			{
				if (host == null)
				{
					continue;
				}

				// Get the content type and ID:
				var typeAndId = getTypeAndId(host);

				if (!loaders.TryGetValue(typeAndId.ContentTypeId, out ContentLoader loader))
				{
					loader = new ContentLoader();
					loaders[typeAndId.ContentTypeId] = loader;
				}

				// Add the content ID:
				loader.Contents[typeAndId.ContentId] = null;
			}

			// Got any loaders to load?
			if (loaders.Count == 0)
			{
				// Nothinug to do.
				return;
			}

			// Otherwise, load each one.
			foreach (var kvp in loaders)
			{
				var loader = kvp.Value;

				// Load the content list now:
				var content = await List(context, kvp.Key, loader.ContentIds(), convertUser);

				// Apply it back to the loader:
				loader.Apply(content);
			}

			// Ok - each loader (each content type) now has loaded its list of contents, and applied them into a fast lookup.
			// Next, loop over the host contents again and apply the objects there too.
			foreach (var host in parentContents)
			{
				if (host == null)
				{
					continue;
				}

				// Get the content type and ID:
				var typeAndId = getTypeAndId(host);

				if (!loaders.TryGetValue(typeAndId.ContentTypeId, out ContentLoader loader))
				{
					continue;
				}

				// Apply the content (value can be null - that's fine):
				loader.Contents.TryGetValue(typeAndId.ContentId, out object value);
				applyContent(host, value);
			}

		}

		/// <summary>
		/// Gets a list of content by their content ID.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="contentTypeId"></param>
		/// <param name="contentIds"></param>
		/// <param name="convertUser">Converts User objects to UserProfile if true (default).</param>
		/// <returns></returns>
		public static async Task<IEnumerable> List(Context context, int contentTypeId, IEnumerable<uint> contentIds, bool convertUser = true)
		{
			// Get the service:
			var service = Services.GetByContentTypeId(contentTypeId);

			if (service == null)
			{
				return null;
			}

			var objResult = await service.ListObjects(context, contentIds);

			// Special case for users, up until UserProfile is removed.
			if(objResult != null && convertUser && service.ServicedType == typeof(User))
			{
				var _users = service as UserService;

				var result = new List<UserProfile>();

				foreach (var obj in objResult)
				{
					// (Doesn't hit the database)
					result.Add(_users.GetProfile(obj as User));
				}

				objResult = result;
			}
			
			return objResult;
		}

		/// <summary>
		/// Gets a list of content by their content ID.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="contentTypeId"></param>
		/// <param name="filterJson"></param>
		/// <param name="convertUser">Converts User objects to UserProfile if true (default).</param>
		/// <param name="applyPermissions"></param>
		/// <returns></returns>
		public static async Task<ListWithTotal> List(Context context, int contentTypeId, string filterJson, bool convertUser = true, bool applyPermissions = true)
		{
			// Get the service:
			var service = Services.GetByContentTypeId(contentTypeId);

			if (service == null)
			{
				return null;
			}

			var objResult = await service.ListObjects(context, filterJson, applyPermissions ? DataOptions.Default : DataOptions.IgnorePermissions);

			return objResult;
		}

	}

	/// <summary>
	/// A struct of content type and content ID.
	/// </summary>
	public struct ContentTypeAndId
	{
		/// <summary>
		/// Content type. See also: ContentTypes class.
		/// </summary>
		public int ContentTypeId;
		/// <summary>
		/// Content Id.
		/// </summary>
		public uint ContentId;


		/// <summary>
		/// Creates a content type and ID struct (in that order).
		/// </summary>
		/// <param name="contentTypeId"></param>
		/// <param name="id"></param>
		public ContentTypeAndId(int contentTypeId, uint id)
		{
			ContentTypeId = contentTypeId;
			ContentId = id;
		}

		/// <summary>
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() => (ContentTypeId, ContentId).GetHashCode();

		/// <summary>
		/// </summary>
		/// <returns></returns>
		public override bool Equals(object other) => other is ContentTypeAndId ct && Equals(ct);

		/// <summary>
		/// </summary>
		/// <returns></returns>
		public bool Equals(ContentTypeAndId other) => ContentTypeId == other.ContentTypeId && ContentId == other.ContentId;

		/// <summary>
		/// Equals convenience shortcut.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator ==(ContentTypeAndId left, ContentTypeAndId right)
		{
			return left.Equals(right);
		}

		/// <summary>
		/// Not equals convenience shortcut.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator !=(ContentTypeAndId left, ContentTypeAndId right)
		{
			return !(left == right);
		}
	}

	/// <summary>
	/// Loads a list of content of unknown type.
	/// </summary>
	public class ContentLoader
	{
		/// <summary>
		/// The mapping of loaded contents.
		/// </summary>
		public Dictionary<uint, object> Contents = new Dictionary<uint, object>();

		/// <summary>
		/// Enumerator of content IDs.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<uint> ContentIds()
		{
			return Contents.Keys;
		}

		/// <summary>
		/// Applies the given list of content from the database to the internal content lookup.
		/// </summary>
		/// <param name="contents"></param>
		public void Apply(IEnumerable contents)
		{
			if (contents == null)
			{
				return;
			}

			foreach (var content in contents)
			{
				if (content is not IHaveId<uint> entry)
				{
					continue;
				}
				Contents[entry.GetId()] = entry;
			}
		}
	}
}
