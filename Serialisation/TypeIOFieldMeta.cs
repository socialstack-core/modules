using Api.SocketServerLibrary;
using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace Api.Startup
{
	/// <summary>
	/// Field metadata.
	/// </summary>
	public class TypeIOFieldMeta
	{
		/// <summary>
		/// The field itself.
		/// </summary>
		public FieldInfo Field;

		/// <summary>
		/// The output type of this field.
		/// </summary>
		public BoltFieldInfo OutputType;

		/// <summary>
		/// The name of this field.
		/// </summary>
		public string Name;
	}

	/// <summary>
	/// Shared metadata about a particular type.
	/// </summary>
	public class BoltFieldInfo
	{
		/// <summary>
		/// lookup of existing field info.
		/// </summary>
		private static ConcurrentDictionary<Type, BoltFieldInfo> _lookup = new ConcurrentDictionary<Type, BoltFieldInfo>();

		/// <summary>
		/// Gets (or creates) the .Writer(..) and Read method to use for the given type.
		/// The meta also includes a sendable name which can also be used to obtain the same output type on a remote server.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static BoltFieldInfo Get(Type type)
		{
			if (_lookup.TryGetValue(type, out BoltFieldInfo result))
			{
				return result;
			}

			var methodName = "Write";

			// Special case for string:
			if (type == typeof(string))
			{
				methodName = "WriteUTF16";
			}

			// Attempt to get the write method:
			var writeMethod = typeof(Writer).GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance, null, new Type[] { type }, null);

			if (writeMethod == null)
			{
				return null;
			}

			string niceName;
			var baseType = Nullable.GetUnderlyingType(type);

			if (baseType != null)
			{
				niceName = baseType.Name + "?";
			}
			else
			{
				niceName = type.Name;
			}

			// Attempt to get the matching Read method.
			var methods = typeof(Client).GetMethods();
			MethodInfo readMethod = null;

			for (var i = 0; i < methods.Length; i++)
			{
				var method = methods[i];

				if (!method.Name.StartsWith("Read") || method.Name == "ReadCompressed")
				{
					continue;
				}

				// If the return type matches what we want, we have a winner.
				if (method.ReturnType == type)
				{
					readMethod = method;
					break;
				}
			}

			if (readMethod == null)
			{
				return null;
			}

			result = new BoltFieldInfo() {
				WriteMethod = writeMethod,
				ReadMethod = readMethod,
				TypeName = niceName
			};

			_lookup[type] = result;
			return result;
		}

		/// <summary>
		/// The Writer.Write() method to use when outputting this.
		/// </summary>
		public MethodInfo WriteMethod;
		
		/// <summary>
		/// The Client.Read*() method to use when reading this from the client.
		/// </summary>
		public MethodInfo ReadMethod;

		/// <summary>
		/// A simple type matching mechanism.
		/// </summary>
		public string TypeName;
	}
}