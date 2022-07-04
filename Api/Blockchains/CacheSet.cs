using Api.Contexts;
using Api.Users;
using Lumity.BlockChains;
using System;

namespace Api.Startup;

/// <summary>
/// CacheSet extensions
/// </summary>
public partial class CacheSet
{
	private ContentField[] FieldSet;


	/// <summary>
	/// Gets field specific IO for the given definition.
	/// </summary>
	public ContentField GetField(FieldDefinition definition)
	{
		var defId = (int)definition.Id;

		if (FieldSet == null)
		{
			FieldSet = new ContentField[defId + 5];
		}
		else if (defId >= FieldSet.Length)
		{
			Array.Resize(ref FieldSet, defId + 5);
		}

		var cField = FieldSet[defId];

		if (cField == null)
		{
			// Find the field by name.
			if (ContentFields.TryGetValue(definition.Name.ToLower(), out cField))
			{
				if (cField.FieldWriter != null)
				{
					FieldSet[defId] = cField;
				}
				// Otherwise this field doesn't exist on this type.
			}
		}

		return cField;
	}

	/// <summary>
	/// Special blockchain add variant which sets the CreatedUtc, EditedUtc and Id fields as well.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="obj"></param>
	/// <param name="createTimeUtc"></param>
	/// <param name="id"></param>
	public virtual void Add(Context context, object obj, DateTime createTimeUtc, ulong id)
	{
	}
	
	/// <summary>
	/// Removes an object by its ID from the cache. This also removes any of its variants.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="cacheIndex"></param>
	/// <param name="id"></param>
	public virtual void Remove(Context context, int cacheIndex, ulong id)
	{
	}

	/// <summary>
	/// Gets a primary object by its ID (not a locale variant of the object).
	/// </summary>
	/// <param name="id"></param>
	/// <param name="cacheIndex"></param>
	public virtual object Get(ulong id, int cacheIndex)
	{
		return null;
	}

}

public partial class CacheSet<T,ID>
{

	/// <summary>
	/// Special blockchain add variant which sets the CreatedUtc, EditedUtc and Id fields as well.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="obj"></param>
	/// <param name="createTimeUtc"></param>
	/// <param name="id"></param>
	public override void Add(Context context, object obj, DateTime createTimeUtc, ulong id)
	{
		var entity = (T)obj;
		
		// Set ID:
		entity.Id = _idConverter.Convert(id);

		if (entity is IHaveTimestamps revRow)
		{
			// Set the edited/ created times as well:
			revRow.SetEditedUtc(createTimeUtc);
			revRow.SetCreatedUtc(createTimeUtc);
		}

		// Add it to the cache:
		AddPrimary(context, obj);
	}

	/// <summary>
	/// Removes an object by its ID from the cache. This also removes any of its variants.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="cacheIndex"></param>
	/// <param name="id"></param>
	public override void Remove(Context context, int cacheIndex, ulong id)
	{
		_cache[cacheIndex].Remove(context, _idConverter.Convert(id));
	}

	/// <summary>
	/// Gets a primary object by its ID (not a locale variant of the object).
	/// </summary>
	/// <param name="id"></param>
	/// <param name="cacheIndex"></param>
	public override object Get(ulong id, int cacheIndex)
	{
		return _cache[cacheIndex].Get(_idConverter.Convert(id));
	}

}