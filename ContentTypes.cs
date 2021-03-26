using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;


namespace Api.Database
{
	/// <summary>
	/// Types - which must inherit DatabaseRow - can be assigned a numeric ID.
	/// This numeric ID - the ContentTypeId - is used in a variety of modules (reactions, comments, uploads etc)
	/// to identify content being related to other content.
	/// </summary>
	public static class ContentTypes
	{
		static ContentTypes()
		{
			// Setup the reverse content type map:
			Map = new Dictionary<string, int>();
			TypeMap = new Dictionary<string, Type>();
			ReverseMap = new Dictionary<int, string>();
			ReverseTypeMap = new Dictionary<int, Type>();
			
			// Collect all the DatabaseRow classes:
			var allTypes = typeof(ContentTypes).Assembly.DefinedTypes;

			foreach (var typeInfo in allTypes)
			{
				// If it:
				// - Is a class
				// - Inherits DatabaseRow
				// Then add to reverse map

				if (!typeInfo.IsClass || typeInfo.IsAbstract)
				{
					continue;
				}

				if (!IsAssignableToGenericType(typeInfo, typeof(Entity<>)))
				{
					continue;
				}

				// Add now:
				var name = typeInfo.Name.ToLower();
				var id = GetId(name);
				Map[name] = id;
				TypeMap[name] = typeInfo;
				ReverseMap[id] = name;
				ReverseTypeMap[id] = typeInfo;
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
		/// A set of all available content types from lowercase name to ID. Use GetId rather than this directly.
		/// </summary>
		public static Dictionary<string, int> Map;

		/// <summary>
		/// A set of all available content types from lowercase name to the system type.
		/// </summary>
		public static Dictionary<string, Type> TypeMap;

		/// <summary>
		/// Reverse mapping from ID to type name.
		/// Setup during DB service startup.
		/// </summary>
		public static Dictionary<int, string> ReverseMap;
		
		/// <summary>
		/// Reverse mapping from ID to type.
		/// Setup during DB service startup.
		/// </summary>
		public static Dictionary<int, Type> ReverseTypeMap;

		/// <summary>
		/// True if given type is a content type.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static bool IsContentType(Type type)
		{
			foreach (var kvp in TypeMap)
			{
				if (kvp.Value == type)
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Gets a name from the given ID.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public static string GetName(int id)
		{
			ReverseMap.TryGetValue(id, out string res);
			return res;
		}

		/// <summary>
		/// Gets a content type from its name. E.g. "Forum" -> typeof(Apis.Forums.Forum).
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public static Type GetType(string name)
		{
			TypeMap.TryGetValue(name.ToLower(), out Type result);
			return result;
		}

		/// <summary>
		/// Gets a content type from its ID.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public static Type GetType(int id)
		{
			ReverseTypeMap.TryGetValue(id, out Type result);
			return result;
		}

		/// <summary>
		/// Gets the ContentTypeId from the type name. Just a hash number function.
		/// </summary>
		/// <param name="typeName"></param>
		/// <returns></returns>
		public static int GetId(string typeName)
		{
			// Note: Caching this would be nice but isn't worthwhile
			// because it _is_ the deterministic .NET hash function. If it was cached in a dictionary 
			// you'd end up running this code anyway during the lookup!

			typeName = typeName.ToLower();
			
			unchecked
			{
				int hash1 = (5381 << 16) + 5381;
				int hash2 = hash1;

				for (int i = 0; i < typeName.Length; i += 2)
				{
					hash1 = ((hash1 << 5) + hash1) ^ typeName[i];
					if (i == typeName.Length - 1)
						break;
					hash2 = ((hash2 << 5) + hash2) ^ typeName[i + 1];
				}

				return hash1 + (hash2 * 1566083941);
			}
		}

		/// <summary>
		/// Gets the content ID for the given system type.
		/// The type itself should be a DatabaseRow derivative - e.g. typeof(User).
		/// </summary>
		/// <param name="systemType"></param>
		/// <returns></returns>
		public static int GetId(Type systemType)
		{
			return GetId(systemType.Name);
		}

    }
}
