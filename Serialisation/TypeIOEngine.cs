using Api.Database;
using Api.SocketServerLibrary;
using Api.Startup;
using Api.Users;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;

namespace Api.Startup
{
	/// <summary>
	/// Generates a method which will write out all the fields of a type using raw byte copying whenever possible.
	/// As JSON strings are utf8, virtually all the heavy overhead is in conversion to and from UTF8. 
	/// Use ustring whenever feasible to avoid this overhead.
	/// </summary>
	public static class TypeIOEngine
    {
		/// <summary>
		/// Ensures unique names for assemblies generated during this session.
		/// </summary>
		private static int counter = 1;
		
		/// <summary>
		/// Generates a system native write for the given structure.
		/// </summary>
		public static TypeReaderWriter<T> Generate<T, ID>(JsonStructure<T,ID> fieldSet)
			where T : Content<ID>, new()
			where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
		{
			var set = new List<JsonStructure<T, ID>>
			{
				fieldSet
			};
			return Generate(set)[0];
		}

		/// <summary>
		/// The bytes for "true"
		/// </summary>
		public static readonly byte[] TrueBytes = new byte[] { 116, 114, 117, 101 };

		/// <summary>
		/// The bytes for ',"id":
		/// </summary>
		public static readonly byte[] IdHeader = new byte[] { (byte)',', (byte)'"', (byte)'i', (byte)'d', (byte)'"', (byte)':' };

		/// <summary>
		/// The bytes for "false"
		/// </summary>
		public static readonly byte[] FalseBytes = new byte[] { 102, 97, 108, 115 , 101 }; 

		/// <summary>
		/// The bytes for "null"
		/// </summary>
		public static readonly byte[] NullBytes = new byte[] { 110, 117, 108, 108 };

		private static ConcurrentDictionary<Type, JsonFieldType> _typeMap;

		/// <summary>
		/// Escape the given control char. The given control char value must be true for Unicode.IsControl.
		/// </summary>
		/// <param name="control"></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static byte[] EscapedControl(byte control)
		{
			return _controlMap[control];
		}

		/// <summary>
		/// Array of 128 pre-serialised control characters.
		/// </summary>
		private static byte[][] _controlMap;

		private static ConcurrentDictionary<Type, JsonFieldType> GetTypeMap()
		{
			if (_typeMap != null)
			{
				return _typeMap;
			}

			_controlMap = new byte[160][];

			for (uint i = 0; i < 160; i++)
			{
				if (Startup.Utf8Helpers.Unicode.IsControl(i))
				{
					byte[] escapedChar;

					if (i == (int)'\r')
					{
						escapedChar = new byte[] { (byte)'\\', (byte)'r' };
					}
					else if (i == (int)'\n')
					{
						escapedChar = new byte[] { (byte)'\\', (byte)'n' };
					}
					else if (i == (int)'\t')
					{
						escapedChar = new byte[] { (byte)'\\', (byte)'t' };
					}
					else if (i == (int)'\b')
					{
						escapedChar = new byte[] { (byte)'\\', (byte)'b' };
					}
					else if (i == (int)'\f')
					{
						escapedChar = new byte[] { (byte)'\\', (byte)'f' };
					}
					else
					{
						// hex representation. "\u followed by 4 hex characters".
						escapedChar = new byte[6];
						escapedChar[0] = (byte)'\\';
						escapedChar[1] = (byte)'u';
						escapedChar[2] = (byte)'0';
						escapedChar[3] = (byte)'0';

						// Upper nibble:
						var n = (i >> 4);
						n += (n > 9u) ? 87u : 48u; // ('a'- 10) : '0';
						escapedChar[4] = (byte)n;

						// Lower nibble:
						n = (i & 15);
						n += (n > 9u) ? 87u : 48u; // ('a' - 10) : '0';
						escapedChar[5] = (byte)n;
					}

					_controlMap[i] = escapedChar;
				}
			}

			var map = new ConcurrentDictionary<Type, JsonFieldType>();
			_typeMap = map;

			AddTo(map, typeof(bool), (ILGenerator code, JsonField field, Action emitValue) =>
			{
				emitValue();
				WriteBool(code);
			});

			var writeSUint = typeof(Writer).GetMethod("WriteS", new Type[] { typeof(uint) });
			var writeSInt = typeof(Writer).GetMethod("WriteS", new Type[] { typeof(int) });

			var writeSUlong = typeof(Writer).GetMethod("WriteS", new Type[] { typeof(ulong) });
			var writeSLong = typeof(Writer).GetMethod("WriteS", new Type[] { typeof(long) });
			var writeSDateTime = typeof(Writer).GetMethod("WriteS", new Type[] { typeof(DateTime) });
			var writeSDouble = typeof(Writer).GetMethod("WriteS", new Type[] { typeof(double) });
			var writeSFloat = typeof(Writer).GetMethod("WriteS", new Type[] { typeof(float) });
			var writeSDecimal = typeof(Writer).GetMethod("WriteS", new Type[] { typeof(decimal) });
			var writeEscapedUString = typeof(Writer).GetMethod("WriteEscaped", new Type[] { typeof(ustring) });
			var writeEscapedString = typeof(Writer).GetMethod("WriteEscaped", new Type[] { typeof(string) });

			AddTo(map, typeof(uint), (ILGenerator code, JsonField field, Action emitValue) =>
			{
				code.Emit(OpCodes.Ldarg_2); // Writer
				emitValue();
				code.Emit(OpCodes.Callvirt, writeSUint);
			});

			AddTo(map, typeof(int), (ILGenerator code, JsonField field, Action emitValue) =>
			{
				code.Emit(OpCodes.Ldarg_2); // Writer
				emitValue();
				code.Emit(OpCodes.Callvirt, writeSInt);
			});

			AddTo(map, typeof(byte), (ILGenerator code, JsonField field, Action emitValue) =>
			{
				code.Emit(OpCodes.Ldarg_2); // Writer
				emitValue();
				code.Emit(OpCodes.Callvirt, writeSUint);
			});

			AddTo(map, typeof(char), (ILGenerator code, JsonField field, Action emitValue) =>
			{
				code.Emit(OpCodes.Ldarg_2); // Writer
				emitValue();
				code.Emit(OpCodes.Callvirt, writeSUint);
			});

			AddTo(map, typeof(sbyte), (ILGenerator code, JsonField field, Action emitValue) =>
			{
				code.Emit(OpCodes.Ldarg_2); // Writer
				emitValue();
				code.Emit(OpCodes.Callvirt, writeSInt);
			});

			AddTo(map, typeof(ushort), (ILGenerator code, JsonField field, Action emitValue) =>
			{
				code.Emit(OpCodes.Ldarg_2); // Writer
				emitValue();
				code.Emit(OpCodes.Callvirt, writeSUint);
			});

			AddTo(map, typeof(short), (ILGenerator code, JsonField field, Action emitValue) =>
			{
				code.Emit(OpCodes.Ldarg_2); // Writer
				emitValue();
				code.Emit(OpCodes.Callvirt, writeSInt);
			});

			AddTo(map, typeof(ulong), (ILGenerator code, JsonField field, Action emitValue) =>
			{
				code.Emit(OpCodes.Ldarg_2); // Writer
				emitValue();
				code.Emit(OpCodes.Callvirt, writeSUlong);
			});

			AddTo(map, typeof(long), (ILGenerator code, JsonField field, Action emitValue) =>
			{
				code.Emit(OpCodes.Ldarg_2); // Writer
				emitValue();
				code.Emit(OpCodes.Callvirt, writeSLong);
			});

			AddTo(map, typeof(DateTime), (ILGenerator code, JsonField field, Action emitValue) =>
			{
				code.Emit(OpCodes.Ldarg_2); // Writer
				emitValue();
				code.Emit(OpCodes.Callvirt, writeSDateTime);
			});

			AddTo(map, typeof(ustring), (ILGenerator code, JsonField field, Action emitValue) =>
			{
				code.Emit(OpCodes.Ldarg_2); // Writer
				emitValue();
				code.Emit(OpCodes.Callvirt, writeEscapedUString);
			});

			AddTo(map, typeof(string), (ILGenerator code, JsonField field, Action emitValue) =>
			{
				code.Emit(OpCodes.Ldarg_2); // Writer
				emitValue();
				code.Emit(OpCodes.Callvirt, writeEscapedString);
			});

			AddTo(map, typeof(double), (ILGenerator code, JsonField field, Action emitValue) =>
			{
				code.Emit(OpCodes.Ldarg_2); // Writer
				emitValue();
				code.Emit(OpCodes.Callvirt, writeSDouble);
			});

			AddTo(map, typeof(float), (ILGenerator code, JsonField field, Action emitValue) =>
			{
				code.Emit(OpCodes.Ldarg_2); // Writer
				emitValue();
				code.Emit(OpCodes.Callvirt, writeSFloat);
			});

			AddTo(map, typeof(decimal), (ILGenerator code, JsonField field, Action emitValue) =>
			{
				code.Emit(OpCodes.Ldarg_2); // Writer
				emitValue();
				code.Emit(OpCodes.Callvirt, writeSDecimal);
			});

			// https://github.com/dotnet/runtime/blob/927b1c54956ddb031a2e1a3eddb94ccc16004c27/src/libraries/System.Private.CoreLib/src/System/Number.Formatting.cs#L1333

			return map;
		}

		/// <summary>
		/// Generates ID collectors for the given fields.
		/// </summary>
		/// <param name="idFields"></param>
		/// <returns></returns>
		public static void GenerateIDCollectors(ContentField[] idFields)
		{
			var atLeastOne = false;

			for (var i = 0; i < idFields.Length; i++)
			{
				if (idFields[i].IDCollectorType == null)
				{
					atLeastOne = true;
					break;
				}
			}

			if (!atLeastOne)
			{
				return;
			}

			AssemblyName assemblyName = new AssemblyName("GeneratedTypes_" + counter);
			counter++;
			AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);
			ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name);

			for (var i = 0; i < idFields.Length; i++)
			{
				var idField = idFields[i];

				if (idField.IDCollectorType != null)
				{
					continue;
				}

				// If the field type is nullable, use the base type here.
				var nullableBaseType = Nullable.GetUnderlyingType(idField.FieldType);

				var baseType = typeof(IDCollector<>).MakeGenericType(new Type[] {
					nullableBaseType == null ? idField.FieldType : nullableBaseType
				});

				// Get the Add method:
				var addMethod = baseType.GetMethod("Add");

				// Build the type:
				TypeBuilder typeBuilder = moduleBuilder.DefineType("FieldWriter_" + idField.Name + "_" + counter, TypeAttributes.Public, baseType);
				counter++;

				var collect = typeBuilder.DefineMethod("Collect", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual, typeof(void), new Type[] {
					typeof(object)
				});

				// "This" is the ID collector, with it's Add method.
				// arg1 is the object to collect from.
				var body = collect.GetILGenerator();

				body.Emit(OpCodes.Ldarg_0);

				// Put the field (or property) value on the stack:
				if (idField.FieldInfo != null)
				{
					body.Emit(OpCodes.Ldarg_1);
					body.Emit(OpCodes.Ldfld, idField.FieldInfo);
				}
				else
				{
					// Property
					body.Emit(OpCodes.Ldarg_1);
					body.Emit(OpCodes.Callvirt, idField.PropertyInfo.GetGetMethod());
				}

				if (nullableBaseType != null)
				{
					// If nullable, and it's null, do nothing. To check though, we need an address. We'll need to store it in a local for that:
					var loc = body.DeclareLocal(idField.FieldType);
					var after = body.DefineLabel();
					body.Emit(OpCodes.Stloc, loc);
					body.Emit(OpCodes.Ldloca, 0);
					body.Emit(OpCodes.Dup);
					body.Emit(OpCodes.Callvirt, idField.FieldType.GetProperty("HasValue").GetGetMethod());
					// The t/f is now on the stack. Check if it's null, and if so, ret.
					body.Emit(OpCodes.Ldc_I4_0);
					body.Emit(OpCodes.Ceq);
					body.Emit(OpCodes.Brfalse, after);
					body.Emit(OpCodes.Pop); // Remove the Ldarg_0 and the val (which we duped above to read HasValue) from the stack.
					body.Emit(OpCodes.Pop);
					body.Emit(OpCodes.Ret);
					body.MarkLabel(after);
					body.Emit(OpCodes.Callvirt, idField.FieldType.GetProperty("Value").GetGetMethod());
				}

				// Add:
				body.Emit(OpCodes.Callvirt, addMethod);
				body.Emit(OpCodes.Ret);

				// Lock the type:
				idField.SetIDCollectorType(typeBuilder.CreateType());
			}

		}

		private static JsonFieldType AddTo(ConcurrentDictionary<Type, JsonFieldType> map, Type type, Action<ILGenerator, JsonField, Action> onWriteValue)
		{
			var fieldType = new JsonFieldType(type, onWriteValue);
			map[type] = fieldType;
			return fieldType;
		}

		/// <summary>
		/// Writes the given meta fields to the given builder, in a human readable way.
		/// </summary>
		/// <param name="fields"></param>
		/// <param name="builder"></param>
		private static void BuildDescription(List<TypeIOFieldMeta> fields, StringBuilder builder)
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

		private static ConcurrentDictionary<Type, object> _boltIO = new ConcurrentDictionary<Type, object>();

		/// <summary>
		/// Gets a bolt reader/ writer for the given type. Note that this does not write the type name etc - 
		/// it purely writes the fields, and provides a portable field description.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static BoltReaderWriter<T> GetBolt<T>()
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
		/// Creates a reader/ writer for the given message or content type.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		private static BoltReaderWriter<T> Generate<T>()
			where T : new()
		{
			// Collect and sort fields.
			var fields = typeof(T).GetFields();

			var fieldMeta = new List<TypeIOFieldMeta>();

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
					Console.WriteLine("[WARN]: Unable to sync field '" + field.Name + "' on '" + typeof(T).Name + "'. " +
						field.FieldType + " was not recognised as a valid writeable field.");
					continue;
				}

				fieldMeta.Add(new TypeIOFieldMeta()
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

			AssemblyName assemblyName = new AssemblyName("GeneratedTypes_" + counter);
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
		/// Generates a system native write for the given structures. This list will usually be the list of all roles for a type on the first run.
		/// </summary>
		public static List<TypeReaderWriter<T>> Generate<T, ID>(List<JsonStructure<T, ID>> fieldSets)
			where T : Content<ID>, new()
			where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
		{
			var result = new List<TypeReaderWriter<T>>();
			
			AssemblyName assemblyName = new AssemblyName("GeneratedTypes_" + counter);
			counter++;
			AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);
			ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name);

			foreach (var set in fieldSets)
			{
				if (set == null)
				{
					continue;
				}

				// Base type to use:
				var baseType = typeof(TypeReaderWriter<T>);

				// Start building the type:
				var typeName = typeof(T).Name;
				TypeBuilder typeBuilder = moduleBuilder.DefineType(typeName + "_RW", TypeAttributes.Public, baseType);

				// all byte[] fields to initialise:
				var fieldsToInit = new List<PreGeneratedByteField>();

				// The JSON "header" which will be of the form {"type":"typename"
				var header = AddField(typeBuilder, fieldsToInit, "{\"type\":\"" + typeName + "\"");

				var writeJsonPartialMethod = typeBuilder.DefineMethod("WriteJsonPartial", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual, typeof(void), new Type[] {
					typeof(T),
					typeof(Writer)
				});

				ILGenerator writerPartialBody = writeJsonPartialMethod.GetILGenerator();

				// Write the header:
				header.Write(writerPartialBody);

				if (IdHeaderRef == null)
				{
					IdHeaderRef = GetFromCommonField(nameof(IdHeader), IdHeader);
				}

				var typeMap = GetTypeMap();

				// The ID:
				var idField = set.GetField("id", JsonFieldGroup.Any);

				if (typeMap.TryGetValue(idField.FieldInfo.FieldType, out JsonFieldType idJft))
				{
					// ,"id":
					IdHeaderRef.Write(writerPartialBody);

					idJft.EmitWrite(writerPartialBody, idField, null);
				}

				// }
				WriteChar(writerPartialBody, '}');

				writerPartialBody.Emit(OpCodes.Ret);

				var writeJsonMethod = typeBuilder.DefineMethod("WriteJson", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual, typeof(void), new Type[] {
					typeof(T),
					typeof(Writer)
				});
				ILGenerator writerBody = writeJsonMethod.GetILGenerator();

				// Write the header:
				header.Write(writerBody);
				
				if (set.ReadableFields != null){

					// Add each field
					foreach(var field in set.ReadableFields)
					{
						if(field == null || field.Name == "Type")
						{
							continue;
						}

						var ignore = false;

						if (field.Attributes != null)
						{
							foreach (var attrib in field.Attributes)
							{
								if ((attrib as Newtonsoft.Json.JsonIgnoreAttribute) != null || (attrib as System.Text.Json.Serialization.JsonIgnoreAttribute) != null)
								{
									// This field is not for us.
									ignore = true;
									break;
								}
							}
						}

						if (ignore)
						{
							continue;
						}

						// The type we may be outputting a value outputter for:
						var fieldType = field.TargetType;

						// Check if it's a nullable:
						Type nullableType = Nullable.GetUnderlyingType(field.TargetType);

						if (nullableType != null)
						{
							fieldType = nullableType;
						}

						if (!typeMap.TryGetValue(fieldType, out JsonFieldType jft))
						{
							// Can't serialise this here.
							continue;
						}

						var fieldName = field.Name;
						var lowercaseFirst = char.ToLower(fieldName[0]) + fieldName.Substring(1);

						// The field name:
						var property = AddField(typeBuilder, fieldsToInit, ",\"" + lowercaseFirst + "\":");

						// ,"fieldName":
						property.Write(writerBody);

						// Value->str:
						jft.EmitWrite(writerBody, field, nullableType);
					}
				}

				// }
				WriteChar(writerBody, '}');

				writerBody.Emit(OpCodes.Ret);

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
				var instance = Activator.CreateInstance(builtType) as TypeReaderWriter<T>;

				foreach (var field in fieldsToInit)
				{
					var fld = instance.GetType().GetField(field.Field.Name, BindingFlags.NonPublic | BindingFlags.Instance);

					fld.SetValue(instance, field.Value);
				}

				result.Add(instance);
			}
			
			return result;
		}

		private static PreGeneratedByteField True;
		private static PreGeneratedByteField False;
		private static PreGeneratedByteField Null;
		private static PreGeneratedByteField IdHeaderRef;

		/// <summary>
		/// Writes either "true" or "false" based on the current value on the top of the stack.
		/// </summary>
		/// <param name="writerBody"></param>
		public static void WriteNull(ILGenerator writerBody)
		{
			if (Null == null)
			{
				Null = GetFromCommonField(nameof(NullBytes), NullBytes);
			}

			Null.Write(writerBody);
		}

		/// <summary>
		/// Writes either "true" or "false" based on the current value on the top of the stack.
		/// </summary>
		/// <param name="writerBody"></param>
		public static void WriteBool(ILGenerator writerBody)
		{
			if (True == null)
			{
				True = GetFromCommonField(nameof(TrueBytes), TrueBytes);
				False = GetFromCommonField(nameof(FalseBytes), FalseBytes);
			}

			var startOfFalse = writerBody.DefineLabel();
			var endOfIfElse = writerBody.DefineLabel();
			writerBody.Emit(OpCodes.Brfalse, startOfFalse);
			True.Write(writerBody);
			writerBody.Emit(OpCodes.Br, endOfIfElse);
			writerBody.MarkLabel(startOfFalse);
			False.Write(writerBody);
			writerBody.MarkLabel(endOfIfElse);
		}

		/// <summary>
		/// Creates a pre-gen byte field from one of the common fields.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="val"></param>
		/// <returns></returns>
		private static PreGeneratedByteField GetFromCommonField(string name, byte[] val)
		{
			var commonField = typeof(TypeIOEngine).GetField(name, BindingFlags.Public | BindingFlags.Static);
			return new PreGeneratedByteField()
			{
				Field = commonField,
				Value = val
			};
		}

		/// <summary>
		/// Emits IL which will write exactly 1 character (the given one) to the stream.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="character"></param>
		private static void WriteChar(ILGenerator writer, char character)
		{
			if (_writeByte == null)
			{
				_writeByte = typeof(Writer).GetMethod("Write", new Type[] {
					typeof(byte)
				});
			}

			var byteVal = (int)character;
			writer.Emit(OpCodes.Ldarg_2);
			writer.Emit(OpCodes.Ldc_I4, byteVal);
			writer.Emit(OpCodes.Callvirt, _writeByte);
		}

		/// <summary>
		/// Writer.Write(byte)
		/// </summary>
		private static MethodInfo _writeByte;

		/// <summary>
		/// Adds a field with the given textual content. It will be added as a UTF8 byte[].
		/// </summary>
		/// <param name="typeBuilder"></param>
		/// <param name="fields"></param>
		/// <param name="content"></param>
		/// <param name="includeLength"></param>
		/// <returns></returns>
		public static PreGeneratedByteField AddField(TypeBuilder typeBuilder, List<PreGeneratedByteField> fields, string content, bool includeLength = false)
		{
			var textBytes = System.Text.Encoding.UTF8.GetBytes(content);

			if (includeLength)
			{
				// Text which is 250 bytes or shorter:
				var len = textBytes.Length;
				var bytes = new byte[1 + textBytes.Length];
				Array.Copy(textBytes, 0, bytes, 1, textBytes.Length);
				bytes[0] = (byte)len;
			}

			return AddField(typeBuilder, fields, textBytes);
		}

		/// <summary>
		/// Adds a field with the given byte[] content.
		/// </summary>
		/// <param name="typeBuilder"></param>
		/// <param name="fields"></param>
		/// <param name="content"></param>
		/// <returns></returns>
		public static PreGeneratedByteField AddField(TypeBuilder typeBuilder, List<PreGeneratedByteField> fields, byte[] content)
		{
			var field = typeBuilder.DefineField("data_" + fields.Count, typeof(byte[]), FieldAttributes.InitOnly | FieldAttributes.Private);

			var fieldToInit = new PreGeneratedByteField()
			{
				Field = field,
				Value = content
			};

			fields.Add(fieldToInit);
			return fieldToInit;
		}
	}

	/// <summary>
	/// A particular supported JSON field type.
	/// </summary>
	public class JsonFieldType
	{
		/// <summary>
		/// System type.
		/// </summary>
		public Type Type;

		/// <summary>
		/// Serialises the value which is already on the stack.
		/// </summary>
		public Action<ILGenerator, JsonField, Action> OnSerialise;

		/// <summary>
		/// Defines info about an available field type.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="onSerialise"></param>
		public JsonFieldType(Type type, Action<ILGenerator, JsonField, Action> onSerialise)
		{
			Type = type;
			OnSerialise = onSerialise;
		}

		/// <summary>
		/// Emits the necessary command to serialise the field.
		/// </summary>
		public void EmitWrite(ILGenerator body, JsonField field, Type nullableType)
		{
			Label? endOfStatementLabel = nullableType == null ? null : body.DefineLabel();

			// Check if it is null - output an address to the struct so we can call HasValue.
			LocalBuilder propertyLocal = null;

			if (nullableType != null)
			{
				// the object we're getting the field/ property from:
				body.Emit(OpCodes.Ldarg_1);

				if (field.PropertyGet != null)
				{
					// Property. Store in a local such that the address can be used.
					propertyLocal = body.DeclareLocal(field.TargetType);
					body.Emit(OpCodes.Callvirt, field.PropertyGet);
					body.Emit(OpCodes.Stloc, propertyLocal);
					body.Emit(OpCodes.Ldloca, propertyLocal);
				}
				else
				{
					// Field:
					body.Emit(OpCodes.Ldflda, field.FieldInfo);
				}

				// Check if it is null (and if so, output "null" and skip the following value)
				var hasValueMethod = field.TargetType.GetProperty("HasValue").GetGetMethod();

				body.Emit(OpCodes.Callvirt, hasValueMethod);

				Label notNullLabel = body.DefineLabel();

				body.Emit(OpCodes.Brtrue, notNullLabel);
				TypeIOEngine.WriteNull(body);
				body.Emit(OpCodes.Br, endOfStatementLabel.Value);
				body.MarkLabel(notNullLabel);
			}

			// Serialise the value:
			OnSerialise(body, field, () => {

				if (nullableType == null)
				{

					// Emit a read of the field value:
					body.Emit(OpCodes.Ldarg_1);
					
					if (field.PropertyGet != null)
					{
						// Property:
						body.Emit(OpCodes.Callvirt, field.PropertyGet);
					}
					else
					{
						// Field:
						body.Emit(OpCodes.Ldfld, field.FieldInfo);
					}
				}
				else
				{
					// This value is nullable.

					// Not null output. Put the actual value onto the stack here.
					if (field.PropertyGet != null)
					{
						body.Emit(OpCodes.Ldloca, propertyLocal);
					}
					else
					{
						// the object we're getting the field/ property from:
						body.Emit(OpCodes.Ldarg_1);
						body.Emit(OpCodes.Ldflda, field.FieldInfo);
					}

					var getValueMethod = field.TargetType.GetProperty("Value").GetGetMethod();
					body.Emit(OpCodes.Callvirt, getValueMethod);
				}
			});

			if (endOfStatementLabel.HasValue)
			{
				body.MarkLabel(endOfStatementLabel.Value);
			}
		}
	}

	/// <summary>
	/// A field containing a pre-generated byte[].
	/// </summary>
	public class PreGeneratedByteField
	{
		// Writer.Write(byte[], int, int)
		private static MethodInfo _writeByteArray;

		/// <summary>
		/// The field.
		/// </summary>
		public FieldInfo Field;
		/// <summary>
		/// The value.
		/// </summary>
		public byte[] Value;

		/// <summary>
		/// Emits a write of this field into the ILGenerator.
		/// </summary>
		/// <param name="writerBody"></param>
		public void Write(ILGenerator writerBody)
		{

			// Get the Write(byte[], int, int) writer method. We'll use it to write, for example, the common JSON header for this type.
			if (_writeByteArray == null)
			{
				_writeByteArray = typeof(Writer).GetMethod("Write", new Type[] {
					typeof(byte[]),
					typeof(int),
					typeof(int)
				});
			}

			writerBody.Emit(OpCodes.Ldarg_2); // Writer
			if (Field.IsStatic)
			{
				writerBody.Emit(OpCodes.Ldsfld, Field); // field the byte[] is in
			}
			else
			{
				writerBody.Emit(OpCodes.Ldarg_0); // this.
				writerBody.Emit(OpCodes.Ldfld, Field); // field the byte[] is in
			}
			writerBody.Emit(OpCodes.Ldc_I4, 0); // offset
			writerBody.Emit(OpCodes.Ldc_I4, Value.Length); // length (the length of the byte[])
			writerBody.Emit(OpCodes.Call, _writeByteArray); // Call Write(byte[], int, int)
		}
	}

	/// <summary>
	/// A constructed custom content type.
	/// </summary>
	public class ConstructedCustomContentType
	{
		/// <summary>
		/// The ID of the CustomContentType.
		/// </summary>
		public uint Id;
		/// <summary>
		/// The underlying custom type.
		/// </summary>
		public Type ContentType;
		/// <summary>
		/// The controller type for this custom type.
		/// </summary>
		public Type ControllerType;
		/// <summary>
		/// The autoservice for this custom type.
		/// </summary>
		public AutoService Service;
	}

}