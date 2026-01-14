using System;
using Api.Database;
using Api.TypeScript.Objects;
using Api.Users;

namespace Api.TypeScript
{
    public partial class TypeScriptService : AutoService
    {
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
        
        /// <summary>
        /// True if the given type is any kind of AutoController.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public bool  IsControllerDescendant(Type t)
        {
            while (t != null && t != typeof(object))
            {
                if (t.IsGenericType)
                {
                    var genericDef = t.GetGenericTypeDefinition();

                    if (genericDef == typeof(AutoController<>) || 
                        genericDef == typeof(AutoController<,>))
                    {
                        return true;
                    }
                }
                else if (t == typeof(AutoController))
                {
                    return true;
                }

                t = t.BaseType;
            }

            return false;
        }

        /// <summary>
        /// True if the given type is the autocontroller for an entity.
        /// </summary>
        /// <param name="t"></param>
        /// <param name="entityType"></param>
        /// <returns></returns>
        public bool IsEntityController(Type t, out Type entityType)
        {
            entityType = null;

            while (t != null && t != typeof(object))
            {
                if (t.IsGenericType)
                {
                    var genericDef = t.GetGenericTypeDefinition();

                    if (genericDef == typeof(AutoController<>) || genericDef == typeof(AutoController<,>))
                    {
                        entityType = t.GetGenericArguments()[0];
                        return true;
                    }
                }

                t = t.BaseType;
            }

            return false;
        }

    }
}
