using Api.Startup;
using Api.Translate;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;


namespace Api.Database
{
	/// <summary>
	/// Metadata about a content type, such as its ID, the service and the type.
	/// </summary>
	public class ContentTypeMeta
	{
		/// <summary>
		/// Content type name.
		/// </summary>
		public string Name;
		/// <summary>
		/// Content type.
		/// </summary>
		public Type ContentType;
		/// <summary>
		/// The service.
		/// </summary>
		public AutoService Service;
	}

	/// <summary>
	/// Types - which must inherit DatabaseRow - can be assigned a numeric ID.
	/// This numeric ID - the ContentTypeId - is used in a variety of modules (reactions, comments, uploads etc)
	/// to identify content being related to other content.
	/// </summary>
	public static class ContentTypes
	{
		/// <summary>
		/// The supported locale set, indexed by locale ID-1. Can be null.
		/// </summary>
		public static Locale[] Locales;

		/// <summary>
		/// A set of all available content types from lowercase name to ID. Use GetId rather than this directly.
		/// </summary>
		public static ConcurrentDictionary<string, ContentTypeMeta> Map;

		/// <summary>
		/// A set of all available content types from lowercase name to ID. Use GetId rather than this directly.
		/// </summary>
		public static ConcurrentDictionary<Type, ContentTypeMeta> TypeMap;

		static ContentTypes()
		{
			// Setup the content maps:
			Map = new ConcurrentDictionary<string, ContentTypeMeta>();
			TypeMap = new ConcurrentDictionary<Type, ContentTypeMeta>();
		}

		/// <summary>
		/// True if the given type is a persistent type (i.e. if it should be stored in the database or not).
		/// </summary>
		/// <param name="t"></param>
		/// <returns></returns>
		public static bool IsPersistentType(Type t)
		{
			var cacheOnlyAttribs = t.GetCustomAttributes(typeof(CacheOnlyAttribute), true);
			return (cacheOnlyAttribs == null || cacheOnlyAttribs.Length == 0);
		}
		
		/// <summary>
		/// Adds or removes the given type from the lookups.
		/// </summary>
		/// <param name="active">True if it's now active, false if it's inactive.</param>
		/// <param name="service">The content types parent service.</param>
		/// <param name="type">The contentType.</param>
		public static void StateChange(bool active, AutoService service, Type type)
		{
			var name = type.Name.ToLower();

			if (active)
			{
				// Add now:
				var typeMeta = new ContentTypeMeta()
				{
					ContentType = type,
					Name = type.Name,
					Service = service
				};
				Map[name] = typeMeta;
				TypeMap[type] = typeMeta;
			}
			else
			{
				// Remove from the maps:
				Map.Remove(name, out ContentTypeMeta _);
				TypeMap.Remove(type, out ContentTypeMeta _);
			}
		}

		/// <summary>
		/// True if the given type is assignable to the given "open" generic type.
		/// </summary>
		/// <param name="givenType"></param>
		/// <param name="genericType"></param>
		/// <returns></returns>
		public static bool IsAssignableToGenericType(Type givenType, Type genericType)
		{
			return IsAssignableToGenericType(givenType, genericType, out _);
		}

		/// <summary>
		/// True if the given type is assignable to the given "open" generic type.
		/// </summary>
		/// <param name="givenType"></param>
		/// <param name="genericType"></param>
		/// <param name="concreteType"></param>
		/// <returns></returns>
		public static bool IsAssignableToGenericType(Type givenType, Type genericType, out Type concreteType)
		{
			var interfaceTypes = givenType.GetInterfaces();

			foreach (var it in interfaceTypes)
			{
				if (it.IsGenericType && it.GetGenericTypeDefinition() == genericType)
				{
					concreteType = it;
					return true;
				}
			}

			if (givenType.IsGenericType && givenType.GetGenericTypeDefinition() == genericType)
			{
				concreteType = givenType;
				return true;
			}

			Type baseType = givenType.BaseType;
			if (baseType == null)
			{
				concreteType = null;
				return false;
			}

			return IsAssignableToGenericType(baseType, genericType, out concreteType);
		}

		/// <summary>
		/// True if given type is a content type.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static bool IsContentType(Type type)
		{
			return TypeMap.TryGetValue(type, out ContentTypeMeta _);
		}

		/// <summary>
		/// Gets a content type from its name. E.g. "Forum" -> typeof(Apis.Forums.Forum).
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public static Type GetType(string name)
		{
			if (Map.TryGetValue(name.ToLower(), out ContentTypeMeta meta))
			{
				return meta.ContentType;
			}
			return null;
		}

    }
}
