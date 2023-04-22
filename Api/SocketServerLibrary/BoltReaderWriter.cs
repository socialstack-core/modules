using Api.ColourConsole;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Api.SocketServerLibrary
{
	
	/// <summary>
	/// Creates bolt reader/ writers and caches them for given types.
	/// </summary>
	public static class BoltReaderWriter
	{
		
		private static ConcurrentDictionary<Type, object> _boltIO = new ConcurrentDictionary<Type, object>();

		/// <summary>
		/// Gets a bolt reader/ writer for the given type. Note that this does not write the type name etc - 
		/// it purely writes the fields, and provides a portable field description.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static BoltReaderWriter<T> Get<T>()
			where T : new()
		{
			if (_boltIO.TryGetValue(typeof(T), out object io))
			{
				return (BoltReaderWriter<T>)io;
			}
			
			// Generate:
			var bolt = Generate<T>();
			_boltIO[typeof(T)] = bolt;
			return bolt;
		}

		/// <summary>
		/// Ensures unique names for assemblies generated during this session.
		/// </summary>
		private static int counter = 1;
		
		/// <summary>
		/// Creates a reader/ writer for the given message or content type.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		private static BoltReaderWriter<T> Generate<T>()
			where T : new()
		{
			// Collect and sort fields.
			var fields = typeof(T).GetFields();

			var fieldMeta = new List<BoltFieldMeta>();

			foreach (var field in fields)
			{
				if (field.GetCustomAttribute<BoltIgnoreAttribute>() != null)
				{
					// Ignore this field.
					continue;
				}

				// Get the output type:
				var outputType = BoltFieldInfo.Get(field.FieldType);

				if (outputType == null)
				{
                    WriteColourLine.Warning("[WARN]: Unable to sync field '" + field.Name + "' on '" + typeof(T).Name + "'. " +
						field.FieldType + " was not recognised as a valid writeable field.");
					continue;
				}

				fieldMeta.Add(new BoltFieldMeta()
				{
					Field = field,
					Name = field.Name,
					OutputType = outputType
				});
			}

			// Sort the fields. This helps ensure compatibility between servers.
			fieldMeta.Sort((p, q) => p.Name.CompareTo(q.Name));

			// In instances where an object is likely to change, the link MUST have a textual description of an object sent over it.
			// This is the description itself:
			var sb = new StringBuilder();
			BuildDescription(fieldMeta, sb);
			var description = sb.ToString();

			AssemblyName assemblyName = new AssemblyName("GeneratedBolts_" + counter);
			counter++;
			AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);
			ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name);

			// Base type to use:
			var baseType = typeof(BoltReaderWriter<T>);

			// Start building the type:
			var typeName = typeof(T).Name;
			TypeBuilder typeBuilder = moduleBuilder.DefineType(typeName + "_RW", TypeAttributes.Public, baseType);

			// all byte[] fields to initialise:
			var fieldsToInit = new List<PreGeneratedByteField>();

			var writeMethod = typeBuilder.DefineMethod("Write", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual, typeof(void), new Type[] {
					typeof(T),
					typeof(Writer)
				});

			ILGenerator writerBody = writeMethod.GetILGenerator();

			foreach (var field in fieldMeta)
			{
				// Get the .Write(VALUE) method:
				var method = field.OutputType.WriteMethod;
				
				/*
				var debugField = typeof(TypeIOEngine).GetMethod("DebugField", BindingFlags.Public | BindingFlags.Static);
				writerBody.Emit(OpCodes.Ldstr, "write " + field.Name);
				writerBody.Emit(OpCodes.Call, debugField);
				*/
				
				// get the writer:
				writerBody.Emit(OpCodes.Ldarg_2);

				// Load the field value from the arg:
				writerBody.Emit(OpCodes.Ldarg_1);
				writerBody.Emit(OpCodes.Ldfld, field.Field);

				// Invoke the write method:
				writerBody.Emit(OpCodes.Callvirt, method);
			}

			writerBody.Emit(OpCodes.Ret);

			var readMethod = typeBuilder.DefineMethod("Read", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual, typeof(void), new Type[] {
					typeof(T),
					typeof(Client)
				});

			ILGenerator readerBody = readMethod.GetILGenerator();

			foreach (var field in fieldMeta)
			{
				// Get the client.Read*() method:
				var method = field.OutputType.ReadMethod;
				
				/*
				var debugField = typeof(TypeIOEngine).GetMethod("DebugField", BindingFlags.Public | BindingFlags.Static);
				readerBody.Emit(OpCodes.Ldstr, "read "+ field.Name);
				readerBody.Emit(OpCodes.Call, debugField);
				*/
				
				// Set the field value to the arg.
				readerBody.Emit(OpCodes.Ldarg_1);

				// get the client reader:
				readerBody.Emit(OpCodes.Ldarg_2);

				// Invoke the write method:
				readerBody.Emit(OpCodes.Callvirt, method);

				readerBody.Emit(OpCodes.Stfld, field.Field);
			}

			readerBody.Emit(OpCodes.Ret);

			// Just an empty constructor. The actual pre-defined byte[]'s will be set direct to the fields shortly.
			ConstructorBuilder ctor0 = typeBuilder.DefineConstructor(
				MethodAttributes.Public,
				CallingConventions.Standard,
				Type.EmptyTypes
			);
			ILGenerator constructorBody = ctor0.GetILGenerator();
			constructorBody.Emit(OpCodes.Ret);

			// Finish the type.
			Type builtType = typeBuilder.CreateType();
			var instance = Activator.CreateInstance(builtType) as BoltReaderWriter<T>;

			foreach (var field in fieldsToInit)
			{
				var fld = instance.GetType().GetField(field.Field.Name, BindingFlags.NonPublic | BindingFlags.Instance);
				fld.SetValue(instance, field.Value);
			}

			// Set the bolt description:
			instance.Description = description;

			return instance;
		}

		/// <summary>
		/// Writes the given meta fields to the given builder, in a human readable way.
		/// </summary>
		/// <param name="fields"></param>
		/// <param name="builder"></param>
		private static void BuildDescription(List<BoltFieldMeta> fields, StringBuilder builder)
		{
			for (var i = 0; i < fields.Count; i++)
			{
				// Field name:
				var field = fields[i];

				builder.Append('\n');
				builder.Append(field.Name);
				builder.Append('=');
				builder.Append(field.OutputType.TypeName);
			}

			builder.Append('\n');
		}
		
	}

	/// <summary>
	/// Reads/writes in the binary bolt format.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class BoltReaderWriter<T>
		where T: new()
	{
		/// <summary>
		/// Writes the given object to the given writer in raw bolt continuous field format.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="writer"></param>
		public virtual void Write(T obj, Writer writer)
		{

		}

		/// <summary>
		/// Reads the type from the reader and removes the concrete type ref.
		/// </summary>
		/// <param name="client"></param>
		/// <returns></returns>
		public object ReadGeneric(Client client)
		{
			var result = new T();
			Read(result, client);
			return result;
		}

		/// <summary>
		/// Allocates an object of the given type and reads it from the given client.
		/// </summary>
		/// <param name="client"></param>
		/// <returns></returns>
		public T Read(Client client)
		{
			var result = new T();
			Read(result, client);
			return result;
		}

		/// <summary>
		/// Reads to the given object from the given writer in raw bolt continuous field format.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="reader"></param>
		public virtual void Read(T obj, Client reader)
		{

		}

		/// <summary>
		/// Field description.
		/// </summary>
		public string Description;
	}

}