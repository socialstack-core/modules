using Api.Contexts;
using Api.Startup;
using Api.Translate;
using System;
using System.Threading.Tasks;

namespace Api.Pages;


/// <summary>
/// A convenience engine for obtaining relative URLs.
/// </summary>
public class RelativeUrlLoader
{
	ContentField idSource = null;
	AutoService relativeToService = null;
	Func<Context, object, ulong> idLoader = null;
	AutoService svc = null;
	string relativeField = null;
	
	/// <summary>
	/// Creates a new relative URL loader using the named field for objects from the given service.
	/// </summary>
	/// <param name="relField"></param>
	/// <param name="s"></param>
	public RelativeUrlLoader(string relField, AutoService s){
		relativeField = relField;
		svc = s;
	}

	/// <summary>
	/// Gets the relative path for the given object from the service that this loader is initted for.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="obj"></param>
	/// <returns></returns>
	/// <exception cref="PublicException"></exception>
	public async ValueTask<string> GetRelativeTo(Context context, object obj)
	{
		if (relativeToService == null)
		{
			var fields = svc.GetContentFields();
			if (!fields.TryGetOrGlobal(relativeField.ToLower(), out ContentField relative))
			{
				throw new PublicException("Incorrect content type configuration. The field used to generate the URL was not found: " + relativeField + " doesn't exist on " + svc.EntityName, "relative_field/required");
			}

			if (relative.VirtualInfo == null || relative.VirtualInfo.IsList)
			{
				throw new PublicException("Incorrect content type configuration. The field used to generate the URL was not an entity field: " + relativeField + " must be a virtual field on " + svc.EntityName, "relative_field/entity");
			}

			relativeToService = relative.VirtualInfo.Service;
			idSource = relative.VirtualInfo.IdSource;

			if (idSource == null || idSource.FieldInfo == null || relativeToService == null)
			{
				throw new PublicException("Incorrect content type configuration; unable to resolve the service for a virtual field (" + relativeField + ")", "relative/unresolved");
			}

			bool isLocalised = false;
			bool isNullable = false;
			var idFieldType = idSource.FieldType;
			if (idFieldType.IsGenericType)
			{
				var gtd = idFieldType.GetGenericTypeDefinition();

				if (gtd == typeof(Localized<>))
				{
					isLocalised = true;
					idFieldType = idFieldType.GetGenericArguments()[0];
				}
			}

			var underType = Nullable.GetUnderlyingType(idFieldType);
			if (underType != null)
			{
				isNullable = true;
				idFieldType = underType;
			}

			if (idFieldType != typeof(uint) && idFieldType != typeof(ulong))
			{
				throw new PublicException("Incorrect content type configuration; cannot use specified ID field because it's a '" + idFieldType + "' (uint or ulong only).", "id/int_only");
			}

			bool isUint = idFieldType == typeof(uint);

			if (isUint)
			{
				if (isLocalised)
				{
					if (isNullable)
					{
						idLoader = (Context context, object id) => ((Localized<uint?>)id).Get(context).GetValueOrDefault();
					}
					else
					{
						idLoader = (Context context, object id) => ((Localized<uint>)id).Get(context);
					}
				}
				else
				{
					if (isNullable)
					{
						idLoader = (Context context, object id) => ((uint?)id).GetValueOrDefault();
					}
					else
					{
						idLoader = (Context context, object id) => (uint)id;
					}
				}
			}
			else
			{
				if (isLocalised)
				{
					if (isNullable)
					{
						idLoader = (Context context, object id) => ((Localized<ulong?>)id).Get(context).GetValueOrDefault();
					}
					else
					{
						idLoader = (Context context, object id) => ((Localized<ulong>)id).Get(context);
					}
				}
				else
				{
					if (isNullable)
					{
						idLoader = (Context context, object id) => ((ulong?)id).GetValueOrDefault();
					}
					else
					{
						idLoader = (Context context, object id) => (ulong)id;
					}
				}
			}

		}

		// Read the ID:
		object id = idSource.FieldInfo.GetValue(obj);

		if (id == null)
		{
			throw new PublicException("Unable to resolve relative ID for the URL (" + relativeField + ")", "relative/id_failed");
		}

		var ulongId = idLoader(context, id);

		// Load the relative to object:
		var forObject = await relativeToService.GetObject(context, ulongId);

		var url = relativeToService.GetPrimaryUrlObject(context, forObject);

		if (string.IsNullOrEmpty(url))
		{
			throw new PublicException("Unable to resolve the URL for the specified target object (" + ulongId + " #" + ulongId + ")", "relative/url_failed");
		}

		return url;
	}

	
}