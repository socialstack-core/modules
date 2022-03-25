using Api.Contexts;
using Api.Startup;
using Api.Users;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Api.AutoForms;
using Newtonsoft.Json;
using Api.Permissions;

namespace Api.Database
{
	/// <summary>
	/// Used to represent an entity which can either be stored in the cache only or in the database.
	/// By default, unless you specify [CacheOnly] on your type, the entity will be stored in the database.
	/// A database table will always have the columns defined here as fields.
	/// Will often be ContentType{int}
	/// </summary>
	/// <typeparam name="ID">The type of ID of your entity. Usually int.</typeparam>
	public abstract partial class Content<ID> where ID : struct
	{
		/// <summary>
		/// The row ID.
		/// </summary>
		[DatabaseIndex]
		[DatabaseField(AutoIncrement = true)]
		[Module(Hide = true)]
		[Permissions(HideFieldByDefault = false)]
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

		/// <summary>
		/// Marks field changes for this object.
		/// Only valid during an Update.
		/// </summary>
		private ChangedFields _changes;
		
		
		/// <summary>
		/// The current changes on this content. Only valid during an Update.
		/// </summary>
		[JsonIgnore]
		public ChangedFields Changes{
			get{
				return _changes;
			}
		}
		
		/// <summary>
		/// Resets the changed fields to "none". This is called for you by StartUpdate or Update.
		/// </summary>
		public void ResetChanges()
		{
			_changes = new ChangedFields(0); // struct
		}
		
		/// <summary>
		/// Marks the given field(s) as changed.
		/// </summary>
		public void MarkChanged(ChangedFields fields){
			_changes += fields; // struct = struct + struct
		}

		/// <summary>
		/// True if the given field(s) are all marked as changed.
		/// </summary>
		/// <param name="fields"></param>
		/// <returns></returns>
		public bool HasChanged(ChangedFields fields) {
			return _changes.Contains(fields);
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
}
