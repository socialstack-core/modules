using System;
using Api.Database;
using Api.Users;

namespace Api.TypeScript
{
	public partial class TypeScriptService : AutoService
	{

		/// <summary>
		/// Gets the first encountered entity type nested in the given one, or null otherwise.
		/// </summary>
		/// <param name="t"></param>
		/// <returns></returns>
		public static Type GetEntityType(Type t)
		{
			t = Nullable.GetUnderlyingType(t) ?? t;

			if (t.IsArray)
			{
				t = t.GetElementType();
			}

			if (t.IsGenericType)
			{
				var genericArgs = t.GetGenericArguments();

				for (var i = 0; i < genericArgs.Length; i++)
				{
					var arg = genericArgs[i];
					var argType = GetEntityType(arg);

					if (argType != null)
					{
						return argType;
					}
				}

				return null;
			}

			if (IsEntityType(t))
			{
				return t;
			}

			return null;
		}

		/// <summary>
		/// True if the given type is an entity type.
		/// </summary>
		/// <param name="t"></param>
		/// <returns></returns>
		public static bool IsEntityType(Type t)
		{
			while (t != null && t != typeof(object))
			{
				if (t.IsGenericType)
				{
					var genericDef = t.GetGenericTypeDefinition();

					if (genericDef == typeof(Content<>) ||
						genericDef == typeof(UserCreatedContent<>) ||
						genericDef == typeof(VersionedContent<>))
					{
						return true;
					}
				}

				t = t.BaseType;
			}

			return false;
		}
		
	}
}
