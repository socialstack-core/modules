using Api.Contexts;
using Api.Database;
using Api.Permissions;
using Api.SocketServerLibrary;
using Api.Translate;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;

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
		/// If the given property is localized, nullable, or both, then this will 
		/// unpack it and place either the raw value or its default (e.g. 0) on the stack.
		/// </summary>
		/// <param name="generator"></param>
		/// <param name="contentObjArg"></param>
		/// <param name="property"></param>
		/// <param name="ctxArg"></param>
		/// <returns></returns>
		public static Type UnpackLocalisedNullable(ILGenerator generator, int contentObjArg, PropertyInfo property, int ctxArg)
		{
			var propertyType = property.PropertyType;
			Type localisedType = null;

			if (propertyType.IsGenericType)
			{
				var typeDef = propertyType.GetGenericTypeDefinition();

				if (typeDef == typeof(Localized<>))
				{
					localisedType = propertyType;
					propertyType = propertyType.GetGenericArguments()[0];
				}
			}

			// Check if it's a nullable:
			var origNullable = propertyType;
			Type nullableType = Nullable.GetUnderlyingType(propertyType);

			if (nullableType != null)
			{
				propertyType = nullableType;
			}

			if (localisedType != null)
			{
				// It's localised but can also be nullable.
				var localVal = generator.DeclareLocal(propertyType);

				generator.Emit(OpCodes.Ldarg, contentObjArg);
				generator.Emit(OpCodes.Callvirt, property.GetGetMethod());
				generator.Emit(OpCodes.Stloc, localVal);
				generator.Emit(OpCodes.Ldloca, localVal);

				// Push the context and a constant 'true'
				generator.Emit(OpCodes.Ldarg, ctxArg); // context
				generator.Emit(OpCodes.Ldc_I4_1); // fallback = true

				// Can now call Get:
				var localisedGetMethod = localisedType.GetMethod(
					"Get", BindingFlags.Public | BindingFlags.Instance,
					new Type[] {
					typeof(Context),
					typeof(bool)
				});

				generator.Emit(OpCodes.Callvirt, localisedGetMethod);

				if (nullableType == null)
				{
					// The value is on the stack - stop there.
					return propertyType;
				}

				// It's also nullable - we'll now need to unpack it by storing it in a local.
				var nullableLoc = generator.DeclareLocal(origNullable);
				generator.Emit(OpCodes.Stloc, nullableLoc);

				UnpackNullable(generator, origNullable, propertyType, (ILGenerator gen) => {
					gen.Emit(OpCodes.Ldloca, nullableLoc);
				});
			}
			else if (nullableType == null)
			{
				// It is just a regular value.

				generator.Emit(OpCodes.Ldarg, contentObjArg); // the content obj 
				generator.Emit(OpCodes.Callvirt, property.GetGetMethod()); // the value 

				// The value is now on the stack.
				return propertyType;
			}
			else
			{
				// It's a nullable non-localized value.
				UnpackNullable(generator, origNullable, propertyType, (ILGenerator gen) => {
					var localVal = gen.DeclareLocal(propertyType);
					gen.Emit(OpCodes.Ldarg, contentObjArg);
					gen.Emit(OpCodes.Callvirt, property.GetGetMethod());
					gen.Emit(OpCodes.Stloc, localVal);
					gen.Emit(OpCodes.Ldloca, localVal);
				});
			}

			return propertyType;
		}
		
		/// <summary>
		/// If the given field is localized, nullable, or both, then this will 
		/// unpack it and place either the raw value or its default (e.g. 0) on the stack.
		/// </summary>
		/// <param name="generator"></param>
		/// <param name="contentObjArg"></param>
		/// <param name="field"></param>
		/// <param name="ctxArg"></param>
		/// <returns></returns>
		public static Type UnpackLocalisedNullable(ILGenerator generator, int contentObjArg, FieldInfo field, int ctxArg)
		{
			var fieldType = field.FieldType;
			Type localisedType = null;

			if (fieldType.IsGenericType)
			{
				var typeDef = fieldType.GetGenericTypeDefinition();

				if (typeDef == typeof(Localized<>))
				{
					localisedType = fieldType;
					fieldType = fieldType.GetGenericArguments()[0];
				}
			}

			// Check if it's a nullable:
			var origNullable = fieldType;
			Type nullableType = Nullable.GetUnderlyingType(fieldType);

			if (nullableType != null)
			{
				fieldType = nullableType;
			}

			if (localisedType != null)
			{
				// It's localised but can also be nullable.
				generator.Emit(OpCodes.Ldarg, contentObjArg);
				generator.Emit(OpCodes.Ldflda, field);

				// Push the context and a constant 'true'
				generator.Emit(OpCodes.Ldarg, ctxArg); // context
				generator.Emit(OpCodes.Ldc_I4_1); // fallback = true

				// Can now call Get:
				var localisedGetMethod = localisedType.GetMethod(
					"Get", BindingFlags.Public | BindingFlags.Instance,
					new Type[] {
					typeof(Context),
					typeof(bool)
				});

				generator.Emit(OpCodes.Callvirt, localisedGetMethod);

				if (nullableType == null)
				{
					// The value is on the stack - stop there.
					return fieldType;
				}

				// It's also nullable - we'll now need to unpack it by storing it in a local.
				var nullableLoc = generator.DeclareLocal(origNullable);
				generator.Emit(OpCodes.Stloc, nullableLoc);

				UnpackNullable(generator, origNullable, fieldType, (ILGenerator gen) => {
					gen.Emit(OpCodes.Ldloca, nullableLoc);
				});
			}
			else if (nullableType == null)
			{
				// It is just a regular field value.

				generator.Emit(OpCodes.Ldarg, contentObjArg); // the content obj 
				generator.Emit(OpCodes.Ldfld, field); // the value 

				// The value is now on the stack.
				return fieldType;
			}
			else
			{
				// It's a nullable non-localized value.
				UnpackNullable(generator, origNullable, fieldType, (ILGenerator gen) => {
					gen.Emit(OpCodes.Ldarg, contentObjArg);
					gen.Emit(OpCodes.Ldflda, field);
				});
			}
			
			return fieldType;
		}

		private static void UnpackNullable(ILGenerator generator, Type fullType, Type innerType, Action<ILGenerator> pushAddress)
		{
			// Put address on to stack:
			pushAddress(generator);

			// Check if it is null (and if so, put the default value on the stack)
			var hasValueMethod = fullType.GetProperty("HasValue").GetGetMethod();

			generator.Emit(OpCodes.Callvirt, hasValueMethod);

			Label notNullLabel = generator.DefineLabel();
			Label endOfStatementLabel = generator.DefineLabel();

			generator.Emit(OpCodes.Brtrue, notNullLabel);

			// It's null here - emit whatever the default of fieldType is.
			EmitDefault(generator, innerType);

			generator.Emit(OpCodes.Br, endOfStatementLabel);
			generator.MarkLabel(notNullLabel);

			// It's not null here - read the value.

			// Put address on to stack again:
			pushAddress(generator);

			var valueMethod = fullType.GetProperty("Value").GetGetMethod();
			generator.Emit(OpCodes.Callvirt, valueMethod);

			// The value is now on the stack.

			generator.MarkLabel(endOfStatementLabel);
		}

		/// <summary>
		/// Emits a default value for the given target type.
		/// </summary>
		/// <param name="generator"></param>
		/// <param name="type"></param>
		public static void EmitDefault(ILGenerator generator, Type type)
		{
			if (!type.IsValueType)
			{
				generator.Emit(OpCodes.Ldnull);
				return;
			}

			if (type == typeof(bool) || type == typeof(byte) || type == typeof(sbyte) ||
				type == typeof(ushort) || type == typeof(short) ||
				type == typeof(uint) || type == typeof(int))
			{
				generator.Emit(OpCodes.Ldc_I4_0);
				return;
			}

			if (type == typeof(float))
			{
				generator.Emit(OpCodes.Ldc_R4, 0f);
				return;
			}

			if (type == typeof(double))
			{
				generator.Emit(OpCodes.Ldc_R8, 0d);
				return;
			}

			if (type == typeof(ulong) || type == typeof(long))
			{
				generator.Emit(OpCodes.Ldc_I8, 0);
				return;
			}

			// Other structs - call its default ctor:
			var addr = generator.DeclareLocal(type);
			generator.Emit(OpCodes.Ldloca, addr);
			generator.Emit(OpCodes.Initobj, type);
			generator.Emit(OpCodes.Ldloc, addr);
		}

		/// <summary>
		/// Coerses the given value of the given type to a ulong, erroring if it is otherwise not possible.
		/// </summary>
		/// <param name="gen"></param>
		/// <param name="type"></param>
		public static void CoerseToUlong(ILGenerator gen, Type type)
		{
			if (type == typeof(ulong))
			{
				// Already ulong, no conversion needed
				return;
			}

			if (type.IsEnum)
			{
				// Use the underlying numeric type of the enum
				type = Enum.GetUnderlyingType(type);
			}

			if (!type.IsValueType)
			{
				if (type == typeof(string))
				{
					var tryParseMethod = typeof(ulong).GetMethod(
						nameof(ulong.TryParse),
						new[] { typeof(string), typeof(ulong).MakeByRefType() });

					// Declare a local to hold the parsed ulong result
					var resultLocal = gen.DeclareLocal(typeof(ulong));

					// (the string is already on the stack)
					gen.Emit(OpCodes.Ldloca, resultLocal);
					gen.Emit(OpCodes.Call, tryParseMethod);
					gen.Emit(OpCodes.Pop); // pop the bool

					// Push the parsed ulong value onto the stack
					gen.Emit(OpCodes.Ldloc, resultLocal);
					return;
				}

				// Reference type: treat as zero
				gen.Emit(OpCodes.Ldc_I4_0);   // Push 0
				gen.Emit(OpCodes.Conv_U8);    // Convert to ulong
				return;
			}

			switch (Type.GetTypeCode(type))
			{
				case TypeCode.Byte:
				case TypeCode.SByte:
				case TypeCode.Int16:
				case TypeCode.UInt16:
				case TypeCode.Int32:
				case TypeCode.UInt32:
				case TypeCode.Int64:
				case TypeCode.UInt64:
				case TypeCode.Single:  // float
				case TypeCode.Double:  // double
					gen.Emit(OpCodes.Conv_U8);
					break;

				default:
					throw new InvalidOperationException($"Cannot coerce type {type} to ulong.");
			}
		}

		/// <summary>
		/// Used for debugging bolt generated IO.
		/// </summary>
		public static void DebugField(string s)
		{
			Log.Info("bolt", s);
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
		/// Gets JSON field generators.
		/// </summary>
		/// <returns></returns>
		private static ConcurrentDictionary<Type, JsonFieldType> GetTypeMap()
		{
			if (_typeMap != null)
			{
				return _typeMap;
			}
			
			var map = new ConcurrentDictionary<Type, JsonFieldType>();

			AddTo(map, typeof(bool), (ILGenerator code, Action emitValue, bool isDocumentFormat) =>
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
			var writeJsonString = typeof(Writer).GetMethod("Write", new Type[] { typeof(JsonString) });
			var writeEscapedJsonString = typeof(Writer).GetMethod("WriteEscaped", new Type[] { typeof(JsonString) });
			var writeEscapedString = typeof(Writer).GetMethod("WriteEscaped", new Type[] { typeof(string) });
			var writeMapDataString = typeof(Writer).GetMethod("Write", new Type[] { typeof(MappingData) });
			var writeEscapedMapDataString = typeof(Writer).GetMethod("WriteEscaped", new Type[] { typeof(MappingData) });

			AddTo(map, typeof(uint), (ILGenerator code, Action emitValue, bool isDocumentFormat) =>
			{
				code.Emit(OpCodes.Ldarg_2); // Writer
				emitValue();
				code.Emit(OpCodes.Callvirt, writeSUint);
			});

			AddTo(map, typeof(int), (ILGenerator code, Action emitValue, bool isDocumentFormat) =>
			{
				code.Emit(OpCodes.Ldarg_2); // Writer
				emitValue();
				code.Emit(OpCodes.Callvirt, writeSInt);
			});

			AddTo(map, typeof(byte), (ILGenerator code, Action emitValue, bool isDocumentFormat) =>
			{
				code.Emit(OpCodes.Ldarg_2); // Writer
				emitValue();
				code.Emit(OpCodes.Callvirt, writeSUint);
			});

			AddTo(map, typeof(char), (ILGenerator code, Action emitValue, bool isDocumentFormat) =>
			{
				code.Emit(OpCodes.Ldarg_2); // Writer
				emitValue();
				code.Emit(OpCodes.Callvirt, writeSUint);
			});

			AddTo(map, typeof(sbyte), (ILGenerator code, Action emitValue, bool isDocumentFormat) =>
			{
				code.Emit(OpCodes.Ldarg_2); // Writer
				emitValue();
				code.Emit(OpCodes.Callvirt, writeSInt);
			});

			AddTo(map, typeof(ushort), (ILGenerator code, Action emitValue, bool isDocumentFormat) =>
			{
				code.Emit(OpCodes.Ldarg_2); // Writer
				emitValue();
				code.Emit(OpCodes.Callvirt, writeSUint);
			});

			AddTo(map, typeof(short), (ILGenerator code, Action emitValue, bool isDocumentFormat) =>
			{
				code.Emit(OpCodes.Ldarg_2); // Writer
				emitValue();
				code.Emit(OpCodes.Callvirt, writeSInt);
			});

			AddTo(map, typeof(ulong), (ILGenerator code, Action emitValue, bool isDocumentFormat) =>
			{
				code.Emit(OpCodes.Ldarg_2); // Writer
				emitValue();
				code.Emit(OpCodes.Callvirt, writeSUlong);
			});

			AddTo(map, typeof(long), (ILGenerator code, Action emitValue, bool isDocumentFormat) =>
			{
				code.Emit(OpCodes.Ldarg_2); // Writer
				emitValue();
				code.Emit(OpCodes.Callvirt, writeSLong);
			});

			AddTo(map, typeof(DateTime), (ILGenerator code, Action emitValue, bool isDocumentFormat) =>
			{
				code.Emit(OpCodes.Ldarg_2); // Writer
				emitValue();
				code.Emit(OpCodes.Callvirt, writeSDateTime);
			});

			AddTo(map, typeof(ustring), (ILGenerator code, Action emitValue, bool isDocumentFormat) =>
			{
				code.Emit(OpCodes.Ldarg_2); // Writer
				emitValue();
				code.Emit(OpCodes.Callvirt, writeEscapedUString);
			});
			
			AddTo(map, typeof(JsonString), (ILGenerator code, Action emitValue, bool isDocumentFormat) =>
			{
				code.Emit(OpCodes.Ldarg_2); // Writer
				emitValue();
				if (isDocumentFormat)
				{
					code.Emit(OpCodes.Callvirt, writeJsonString);
				}
				else
				{
					code.Emit(OpCodes.Callvirt, writeEscapedJsonString);
				}
			});

			AddTo(map, typeof(MappingData), (ILGenerator code, Action emitValue, bool isDocumentFormat) =>
			{
				code.Emit(OpCodes.Ldarg_2); // Writer
				emitValue();
				if (isDocumentFormat)
				{
					code.Emit(OpCodes.Callvirt, writeMapDataString);
				}
				else
				{
					code.Emit(OpCodes.Callvirt, writeEscapedMapDataString);
				}
			});

			AddTo(map, typeof(string), (ILGenerator code, Action emitValue, bool isDocumentFormat) =>
			{
				code.Emit(OpCodes.Ldarg_2); // Writer
				emitValue();
				code.Emit(OpCodes.Callvirt, writeEscapedString);
			});

			AddTo(map, typeof(double), (ILGenerator code, Action emitValue, bool isDocumentFormat) =>
			{
				code.Emit(OpCodes.Ldarg_2); // Writer
				emitValue();
				code.Emit(OpCodes.Callvirt, writeSDouble);
			});

			AddTo(map, typeof(float), (ILGenerator code, Action emitValue, bool isDocumentFormat) =>
			{
				code.Emit(OpCodes.Ldarg_2); // Writer
				emitValue();
				code.Emit(OpCodes.Callvirt, writeSFloat);
			});

			AddTo(map, typeof(decimal), (ILGenerator code, Action emitValue, bool isDocumentFormat) =>
			{
				code.Emit(OpCodes.Ldarg_2); // Writer
				emitValue();
				code.Emit(OpCodes.Callvirt, writeSDecimal);
			});

			// https://github.com/dotnet/runtime/blob/927b1c54956ddb031a2e1a3eddb94ccc16004c27/src/libraries/System.Private.CoreLib/src/System/Number.Formatting.cs#L1333

			_typeMap = map;

			return map;
		}

		/// <summary>
		/// Generates a document reader/writer for the given content type.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="ID"></typeparam>
		/// <param name="fields"></param>
		/// <returns></returns>
		public static TypeDocumentReaderWriter<T> GenerateDocumentReaderWriter<T, ID>(ContentFields fields)
			where T : Content<ID>, new()
			where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
		{

			AssemblyName assemblyName = new AssemblyName("GeneratedDocRW_" + counter);
			counter++;
			AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);
			ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name);

			// Base type to use:
			var baseType = typeof(TypeDocumentReaderWriter<T>);

			// Start building the type:
			var typeName = typeof(T).Name;
			TypeBuilder typeBuilder = moduleBuilder.DefineType(typeName + "_DocRW", TypeAttributes.Public, baseType);

			var writerMethod = typeBuilder.DefineMethod("WriteStoredJson", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual, typeof(void), new Type[] {
				typeof(T),
				typeof(Writer)
			});

			ILGenerator writeBody = writerMethod.GetILGenerator();

			var first = true;

			EmitWriteChar(writeBody, '{');

			foreach (var field in fields.List)
			{
				if (field.FieldInfo == null)
				// || field.VirtualInfo != null && field.VirtualInfo.IsList { future feature, would need to grab these sets too }
				{
					continue;
				}

				if (field.FieldInfo.FieldType.IsArray)
				{
					Log.Warn("typeio", "Temporarily ignored an array field from probably revisions (WIP)");
					continue;
				}

				if (first)
				{
					first = false;
				}
				else
				{
					EmitWriteChar(writeBody, ',');
				}

				EmitWriteASCII(writeBody, "\"" + field.Name + "\"");
				EmitWriteChar(writeBody, ':');

				var type = field.FieldInfo.FieldType;
				var nextField = writeBody.DefineLabel();

				if (!type.IsValueType)
				{
					// if(x.field == null) { writer.WriteASCII("null"); }
					writeBody.Emit(OpCodes.Ldarg_1); // T obj
					writeBody.Emit(OpCodes.Ldfld, field.FieldInfo); // .theField
					writeBody.Emit(OpCodes.Ldnull);
					writeBody.Emit(OpCodes.Ceq);
					var notNull = writeBody.DefineLabel();
					writeBody.Emit(OpCodes.Brfalse, notNull);
						EmitWriteASCII(writeBody, "null");
						writeBody.Emit(OpCodes.Br, nextField);
					writeBody.MarkLabel(notNull);
				}

				// Here it is either a value type (which can still be a Nullable) or a not null object.

				if (type.IsGenericType)
				{
					var gDef = type.GetGenericTypeDefinition();

					if (gDef == typeof(Localized<>))
					{
						// Call ToJson.
						var toJson = type.GetMethod("ToJson", BindingFlags.Instance | BindingFlags.Public, new Type[] { typeof(Writer) });

						if (toJson == null)
						{
							throw new Exception("ToJson(writer) on a localised type missing somehow!");
						}

						writeBody.Emit(OpCodes.Ldarg_1); // T obj
						writeBody.Emit(OpCodes.Ldflda, field.FieldInfo); // .theLocalizedField (this)
						writeBody.Emit(OpCodes.Ldarg_2); // writer
						writeBody.Emit(OpCodes.Callvirt, toJson); // invoke toJson

					}
					else if (gDef == typeof(Nullable<>))
					{
						EmitWriteBasicField(writeBody, field, (ILGenerator gen) =>
						{
							gen.Emit(OpCodes.Ldarg_1); // T obj
						}, true);
					}
					else
					{
						// probably a list - fail!
						throw new NotSupportedException("Can't currently store this generic type in db documents. " +
							"This can happen if there is e.g. a List in a versioned piece of content.");
					}
				}
				else
				{
					EmitWriteBasicField(writeBody, field, (ILGenerator gen) => {
						gen.Emit(OpCodes.Ldarg_1); // T obj
					}, true);
				}

				writeBody.MarkLabel(nextField);
			}

			EmitWriteChar(writeBody, '}');

			writeBody.Emit(OpCodes.Ret);

			// Finish the type and instance it.
			Type builtType = typeBuilder.CreateType();
			var instance = Activator.CreateInstance(builtType) as TypeDocumentReaderWriter<T>;
			return instance;
		}

		/// <summary>
		/// Emits writer.WriteASCII(val);
		/// </summary>
		/// <param name="gen"></param>
		/// <param name="val"></param>
		public static void EmitWriteASCII(ILGenerator gen, string val)
		{
			gen.Emit(OpCodes.Ldarg_2); // writer.
			var writeAscii = typeof(Writer).GetMethod("WriteASCII", BindingFlags.Public | BindingFlags.Instance);
			gen.Emit(OpCodes.Ldstr, val);
			gen.Emit(OpCodes.Callvirt, writeAscii);
		}
		
		/// <summary>
		/// Emits writer.Write(theChar);
		/// </summary>
		/// <param name="gen"></param>
		/// <param name="val"></param>
		public static void EmitWriteChar(ILGenerator gen, char val)
		{
			gen.Emit(OpCodes.Ldarg_2); // writer.
			var writeByte = typeof(Writer).GetMethod("Write", BindingFlags.Public | BindingFlags.Instance, new Type[]{
				typeof(byte)
			});

			gen.Emit(OpCodes.Ldc_I4, (int)((byte)val));
			gen.Emit(OpCodes.Callvirt, writeByte);
		}

		/// <summary>
		/// Write a field.
		/// </summary>
		/// <param name="body"></param>
		/// <param name="field"></param>
		/// <param name="objLoader"></param>
		/// <param name="ctxLoader"></param>
		/// <param name="isDocumentFormat"></param>
		/// <exception cref="Exception"></exception>
		public static void EmitWriteField(ILGenerator body, ContentField field, Action<ILGenerator> objLoader, Action<ILGenerator> ctxLoader, bool isDocumentFormat = false)
		{
			// Check if it's a nullable:
			var fieldType = field.FieldType;
			var isLocalised = false;

			if (fieldType.IsGenericType)
			{
				var gDef = fieldType.GetGenericTypeDefinition();

				if (gDef == typeof(Localized<>))
				{
					isLocalised = true;
					fieldType = fieldType.GetGenericArguments()[0];
				}
			}

			var nullableType = Nullable.GetUnderlyingType(fieldType);

			if (nullableType != null)
			{
				fieldType = nullableType;
			}

			var typeMap = GetTypeMap();

			if (!typeMap.TryGetValue(fieldType, out JsonFieldType jft))
			{
				// Can't serialise this here.
				throw new Exception("Unable to serialise fields of this type ('" + field.Name + "' was a " + fieldType.Name + ").");
			}

			if (isLocalised)
			{
				jft.EmitLocalisedWrite(body, field.FieldType, field.PropertyInfo?.GetGetMethod(), field.FieldInfo, objLoader, ctxLoader, isDocumentFormat);
			}
			else
			{
				jft.EmitWrite(body, field, nullableType, objLoader, isDocumentFormat);
			}
		}

		/// <summary>
		/// Emits JSON write of a given content field in to the given body. 
		/// It must be a basic type or a nullable of one of those types.
		/// </summary>
		/// <param name="body"></param>
		/// <param name="field"></param>
		/// <param name="objLoader"></param>
		/// <param name="isDocumentFormat"></param>
		/// <exception cref="Exception"></exception>
		public static void EmitWriteBasicField(ILGenerator body, ContentField field, Action<ILGenerator> objLoader, bool isDocumentFormat = false)
		{
			// Check if it's a nullable:
			var fieldType = field.FieldType;

			var nullableType = Nullable.GetUnderlyingType(fieldType);

			if (nullableType != null)
			{
				fieldType = nullableType;
			}

			var typeMap = GetTypeMap();

			if (!typeMap.TryGetValue(fieldType, out JsonFieldType jft))
			{
				// Can't serialise this here.
				throw new Exception("Unable to serialise fields of this type ('" + field.Name + "' was a " + fieldType.Name + ").");
			}

			jft.EmitWrite(body, field, nullableType, objLoader, isDocumentFormat);
		}

		private static JsonFieldType AddTo(ConcurrentDictionary<Type, JsonFieldType> map, Type type, Action<ILGenerator, Action, bool> onWriteValue)
		{
			var fieldType = new JsonFieldType(type, onWriteValue);
			map[type] = fieldType;
			return fieldType;
		}

		private static Dictionary<Type, TypeReaderWriter> _cache = new Dictionary<Type, TypeReaderWriter>();

		/// <summary>
		/// Gets a serializer for any concrete type.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static async ValueTask<TypeReaderWriter<T>> GetSerializer<T>(Context context)
		{
			if (_cache.TryGetValue(typeof(T), out TypeReaderWriter result))
			{
				return result as TypeReaderWriter<T>;
			}

			// Generate and cache it.
			var svcForType = Services.GetByContentType(typeof(T));

			TypeReaderWriter<T> serializer;

			if (svcForType != null)
			{
				serializer = Generate<T>(await svcForType.GetJsonStructure(context), svcForType);
			}
			else
			{
				serializer = Generate<T>(GetFieldSet(typeof(T)), null);
			}

			_cache[typeof(T)] = serializer;
			return serializer;
		}

		/// <summary>
		/// Gets the field set for a given basic (non DB content) type.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		private static List<JsonField> GetFieldSet(Type type)
		{
			var results = new List<JsonField>();

			// Get the public field set:
			var publicFields = type.GetFields();

			// and the public property set:
			var publicProperties = type.GetProperties();

			foreach (var field in publicFields)
			{
				results.Add(new JsonField()
				{
					Name = field.Name,
					OriginalName = field.Name,
					Attributes = ContentField.BuildAttributes(field.CustomAttributes),
					TargetType = field.FieldType,
					FieldInfo = field,
					ContentField = null
				});
			}
			
			foreach (var property in publicProperties)
			{
				results.Add(new JsonField()
				{
					Name = property.Name,
					OriginalName = property.Name,
					Attributes = ContentField.BuildAttributes(property.CustomAttributes),
					PropertyInfo = property,
					TargetType = property.PropertyType,
					PropertyGet = property.GetGetMethod(),
					PropertySet = property.GetSetMethod(),
					ContentField = null,
					Writeable = property.CanWrite,
					// Default behaviour is to hide (from autoforms) non-writeable properties.
					// Using BeforeSettable and setting Hide to false will display a readonly field if you want it to be visible.
					Hide = !property.CanWrite
				});
			}

			return results;
		}

		/// <summary>
		/// Generates a system native write for the given content type.
		/// </summary>
		public static TypeReaderWriter<T> Generate<T>(JsonStructure jsonStructure, AutoService service)
		{
			return Generate<T>(jsonStructure.AllFields.Select(kvp => kvp.Value), service);
		}

		/// <summary>
		/// Generates a system native write for the given structure. It can be any type. If it is a content type however 
		/// then you must also provide the associated autoservice such that it can load the field visibility rules.
		/// </summary>
		public static TypeReaderWriter<T> Generate<T>(IEnumerable<JsonField> fields, AutoService service)
		{
			AssemblyName assemblyName = new AssemblyName("GeneratedTypesRW_" + counter);
			counter++;
			AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);
			ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name);

			// Base type to use:
			var baseType = typeof(TypeReaderWriter<T>);

			// Start building the type:
			var typeName = typeof(T).Name;
			TypeBuilder typeBuilder = moduleBuilder.DefineType(typeName + "_RW", TypeAttributes.Public, baseType);

			ConstructorBuilder ctor0;

			// Just an empty constructor. The actual pre-defined byte[]'s will be set direct to the fields shortly.
			if (service == null)
			{
				ctor0 = typeBuilder.DefineConstructor(
					MethodAttributes.Public,
					CallingConventions.Standard,
					Array.Empty<Type>()
				);
			}
			else
			{
				ctor0 = typeBuilder.DefineConstructor(
					MethodAttributes.Public,
					CallingConventions.Standard,
					[typeof(AutoService)]
				);
			}

			ILGenerator constructorBody = ctor0.GetILGenerator();

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

			// Get the ID field, if there even is one:
			JsonField idField = null;

			foreach (var field in fields)
			{
				if (field.Name.ToLower() == "id")
				{
					idField = field;
					break;
				}
			}

			if (idField != null)
			{
				if (typeMap.TryGetValue(idField.FieldInfo.FieldType, out JsonFieldType idJft))
				{
					// ,"id":
					IdHeaderRef.Write(writerPartialBody);

					idJft.EmitWrite(writerPartialBody, idField, null);
				}
			}

			// }
			WriteChar(writerPartialBody, '}');

			writerPartialBody.Emit(OpCodes.Ret);

			var writeJsonMethod = typeBuilder.DefineMethod("WriteJsonUnclosed", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual, typeof(void), [
				typeof(T),
				typeof(Writer),
				typeof(Context),
				typeof(ContextFlags)
			]);
			ILGenerator writerBody = writeJsonMethod.GetILGenerator();

			// Write the header:
			header.Write(writerBody);

			if (fields != null){

				// Add each field
				foreach(var field in fields)
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

					// Skip all virtual fields.
					if (field.PropertyGet == null && field.FieldInfo == null)
					{
						continue;
					}

					// The type we may be outputting a value outputter for:
					var fieldType = field.TargetType;
					var isLocalised = false;

					if (fieldType.IsGenericType)
					{
						var typeDef = fieldType.GetGenericTypeDefinition();

						if (typeDef == typeof(Localized<>))
						{
							isLocalised = true;
							fieldType = fieldType.GetGenericArguments()[0];
						}
					}

					// Check if it's a nullable:
					Type nullableType = Nullable.GetUnderlyingType(fieldType);

					if (nullableType != null)
					{
						fieldType = nullableType;
					}

					if (!typeMap.TryGetValue(fieldType, out JsonFieldType jft))
					{
						// Can't serialise this here.
						Log.Warn("typeio", "Skipped serialising a field because it is of an unrecognised type, " + typeof(T) + "::" + field.Name + " => " + GetFullTypeSignature(fieldType));
						continue;
					}

					var fieldName = field.Name;
					var lowercaseFirst = char.ToLower(fieldName[0]) + fieldName.Substring(1);
					var readAccessRuleText = service == null ? null : field.GetReadAccessRuleText();

					if (string.IsNullOrEmpty(readAccessRuleText))
					{
						// Not set by admin panel - use the const field instead.
						readAccessRuleText = field.ReadRule;
					}

					bool? constTrueOrFalse = null;
					string filterString = null;

					// if no rule is specified then it is readable by default.
					if(string.IsNullOrEmpty(readAccessRuleText) || readAccessRuleText == "true"){
						constTrueOrFalse = true;
					}else if(readAccessRuleText == "false"){
						constTrueOrFalse = false;
					}else{
						// will be a full filter obj
						filterString = readAccessRuleText;
					}

					if (constTrueOrFalse.HasValue && constTrueOrFalse.Value == false)
					{
						// This field is not for us.
						continue;
					}

					// The field name:
					var property = AddField(typeBuilder, fieldsToInit, ",\"" + lowercaseFirst + "\":");

					// Define a label for the end of the conditional block
					var endOfIfLabel = writerBody.DefineLabel();

					// this/instance
					if (!string.IsNullOrEmpty(filterString))
					{
						var fieldBuilder = typeBuilder.DefineField("filter_" + lowercaseFirst, typeof(FilterBase), FieldAttributes.Private);

						constructorBody.Emit(OpCodes.Ldarg_0);
						constructorBody.Emit(OpCodes.Ldarg_1);
						constructorBody.Emit(OpCodes.Ldstr, filterString); 
						constructorBody.Emit(OpCodes.Ldc_I4_1); 
						constructorBody.Emit(OpCodes.Callvirt,
							typeof(AutoService).GetMethod(
								nameof(AutoService.GetGeneralFilterFor), 
								BindingFlags.Instance | BindingFlags.Public,
								[
									typeof (string), typeof(bool)
								]
							)
						);

						constructorBody.Emit(OpCodes.Stfld, fieldBuilder);
						
						writerBody.Emit(OpCodes.Ldarg_0); // this
						writerBody.Emit(OpCodes.Ldfld, fieldBuilder); // .filter_whatever
						writerBody.Emit(OpCodes.Ldarg_3); // context
						writerBody.Emit(OpCodes.Ldarg_1); // the object
						writerBody.Emit(OpCodes.Ldarg, 4); // SerialiserFlags
						writerBody.Emit(OpCodes.Callvirt, typeof(FilterBase).GetMethod(nameof(FilterBase.Match), BindingFlags.Instance | BindingFlags.Public, [typeof(Context), typeof(object), typeof(ContextFlags) ]));
							
						// Branch if false (skip the block if the condition is false)
						writerBody.Emit(OpCodes.Brfalse, endOfIfLabel);
					}

					// Code to execute if the condition is true
					// ,"fieldName":
					property.Write(writerBody);
					// Value->str:
					if (isLocalised)
					{
						jft.EmitLocalisedWrite(writerBody, field.TargetType, field.PropertyGet, field.FieldInfo, (ILGenerator gen) => {
							writerBody.Emit(OpCodes.Ldarg_1); // the object
						}, (ILGenerator gen) => {
							writerBody.Emit(OpCodes.Ldarg_3); // context
						});
					}
					else
					{
						jft.EmitWrite(writerBody, field, nullableType);
					}
					// Mark the end of the conditional block
					writerBody.MarkLabel(endOfIfLabel);
				}
			}

			writerBody.Emit(OpCodes.Ret);
			constructorBody.Emit(OpCodes.Ret);
				
			// Finish the type.
			Type builtType = typeBuilder.CreateType();
			var instance = (service == null ? Activator.CreateInstance(builtType) : Activator.CreateInstance(builtType, [service])) as TypeReaderWriter<T>;

			foreach (var field in fieldsToInit)
			{
				var fld = instance.GetType().GetField(field.Field.Name, BindingFlags.NonPublic | BindingFlags.Instance);

				fld.SetValue(instance, field.Value);
			}

			return instance;
		}

		private static PreGeneratedByteField True;
		private static PreGeneratedByteField False;
		private static PreGeneratedByteField Null;
		private static PreGeneratedByteField IdHeaderRef;

		private static string GetFullTypeSignature(Type type)
		{
			if (type.IsGenericType)
			{
				// get the trimmed type without the backtick.
				var baseName = type.Name[0..type.Name.IndexOf('`')];
				baseName += "<";

				foreach (var typeArg in type.GetGenericArguments())
				{
					baseName += GetFullTypeSignature(typeArg);
					if (typeArg.IsArray)
					{
						for (var i = 0; i < typeArg.GetArrayRank(); i++)
						{
							baseName += "[]";
						}
					}
				}
				
				if (type.IsArray)
				{
					for (var i = 0; i < type.GetArrayRank(); i++)
					{
						baseName += "[]";
					}
				}
				
				baseName += ">";
				return baseName;
			}

			var name = type.Name;
			
			if (type.IsArray)
			{
				for (var i = 0; i < type.GetArrayRank(); i++)
				{
					name += "[]";
				}
			}

			return name;
		}

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
		public Action<ILGenerator, Action, bool> OnSerialise;

		/// <summary>
		/// Defines info about an available field type.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="onSerialise"></param>
		public JsonFieldType(Type type, Action<ILGenerator, Action, bool> onSerialise)
		{
			Type = type;
			OnSerialise = onSerialise;
		}

		/// <summary>
		/// Emits a write of a Localised field.
		/// </summary>
		/// <param name="body"></param>
		/// <param name="writeObj"></param>
		/// <param name="writeCtx"></param>
		/// <param name="localisedType"></param>
		/// <param name="propertyGet"></param>
		/// <param name="fieldInfo"></param>
		/// <param name="isDocumentFormat"></param>
		public void EmitLocalisedWrite(ILGenerator body, Type localisedType, MethodInfo propertyGet, FieldInfo fieldInfo, Action<ILGenerator> writeObj, Action<ILGenerator> writeCtx, bool isDocumentFormat = false)
		{
			Label endOfStatementLabel = body.DefineLabel();

			// e.g. Localised<uint?> -> valueType is uint?
			var valueType = localisedType.GetGenericArguments()[0];
			var nullableType = Nullable.GetUnderlyingType(valueType);

			// Always put the result value in to a local variable.
			var loc = body.DeclareLocal(valueType);

			if (propertyGet != null)
			{
				var propertyLocal = body.DeclareLocal(localisedType);
				writeObj(body);
				body.Emit(OpCodes.Callvirt, propertyGet);
				body.Emit(OpCodes.Stloc, propertyLocal);
				body.Emit(OpCodes.Ldloca, propertyLocal);
			}
			else
			{
				// Field:
				writeObj(body);
				body.Emit(OpCodes.Ldflda, fieldInfo);
			}

			// An address of the Localized struct is currently on the stack.

			// Push the context and a constant 'true'
			writeCtx(body);
			body.Emit(OpCodes.Ldc_I4_1); // fallback = true

			// Can now call Get:
			var localisedGetMethod = localisedType.GetMethod(
				"Get", BindingFlags.Public | BindingFlags.Instance,
				new Type[] {
					typeof(Context),
					typeof(bool)
				});

			body.Emit(OpCodes.Callvirt, localisedGetMethod);

			// The value, which can still be a nullable, is now in the local:
			body.Emit(OpCodes.Stloc, loc);

			if (nullableType != null)
			{
				body.Emit(OpCodes.Ldloca, loc);

				// Check if it is null (and if so, output "null" and skip the following value)
				var hasValueMethod = valueType.GetProperty("HasValue").GetGetMethod();

				body.Emit(OpCodes.Callvirt, hasValueMethod);

				Label notNullLabel = body.DefineLabel();

				body.Emit(OpCodes.Brtrue, notNullLabel);
				TypeIOEngine.WriteNull(body);
				body.Emit(OpCodes.Br, endOfStatementLabel);
				body.MarkLabel(notNullLabel);
			}

			// Serialise the value:
			OnSerialise(body, () => {

				if (nullableType == null)
				{
					// Emit a read of the local:
					body.Emit(OpCodes.Ldloc, loc);
				}
				else
				{
					// This value is nullable, but it's specifically not null.
					// Put the actual value onto the stack here.
					body.Emit(OpCodes.Ldloca, loc);
					var getValueMethod = valueType.GetProperty("Value").GetGetMethod();
					body.Emit(OpCodes.Callvirt, getValueMethod);
				}
			}, isDocumentFormat);

			body.MarkLabel(endOfStatementLabel);
		}

		/// <summary>
		/// Emits the necessary command to serialise the field.
		/// </summary>
		public void EmitWrite(ILGenerator body, JsonField field, Type nullableType, bool isDocumentFormat = false)
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
			OnSerialise(body, () => {

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
			}, isDocumentFormat);

			if (endOfStatementLabel.HasValue)
			{
				body.MarkLabel(endOfStatementLabel.Value);
			}
		}

		/// <summary>
		/// Emits the necessary command to serialise the field.
		/// </summary>
		public void EmitWrite(ILGenerator body, ContentField field, Type nullableType, Action<ILGenerator> objLoader, bool isDocumentFormat = false)
		{
			Label? endOfStatementLabel = nullableType == null ? null : body.DefineLabel();

			// Check if it is null - output an address to the struct so we can call HasValue.
			LocalBuilder propertyLocal = null;

			if (nullableType != null)
			{
				// the object we're getting the field/ property from:
				objLoader(body);

				if (field.PropertyInfo != null)
				{
					// Property. Store in a local such that the address can be used.
					propertyLocal = body.DeclareLocal(field.FieldType);
					body.Emit(OpCodes.Callvirt, field.PropertyInfo.GetGetMethod());
					body.Emit(OpCodes.Stloc, propertyLocal);
					body.Emit(OpCodes.Ldloca, propertyLocal);
				}
				else
				{
					// Field:
					body.Emit(OpCodes.Ldflda, field.FieldInfo);
				}

				// Check if it is null (and if so, output "null" and skip the following value)
				var hasValueMethod = field.FieldType.GetProperty("HasValue").GetGetMethod();

				body.Emit(OpCodes.Callvirt, hasValueMethod);

				Label notNullLabel = body.DefineLabel();

				body.Emit(OpCodes.Brtrue, notNullLabel);
				TypeIOEngine.WriteNull(body);
				body.Emit(OpCodes.Br, endOfStatementLabel.Value);
				body.MarkLabel(notNullLabel);
			}

			// Serialise the value:
			OnSerialise(body, () => {

				if (nullableType == null)
				{

					// Emit a read of the field value:
					objLoader(body);

					if (field.PropertyInfo != null)
					{
						// Property:
						body.Emit(OpCodes.Callvirt, field.PropertyInfo.GetGetMethod());
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
					if (field.PropertyInfo != null)
					{
						body.Emit(OpCodes.Ldloca, propertyLocal);
					}
					else
					{
						// the object we're getting the field/ property from:
						objLoader(body);
						body.Emit(OpCodes.Ldflda, field.FieldInfo);
					}

					var getValueMethod = field.FieldType.GetProperty("Value").GetGetMethod();
					body.Emit(OpCodes.Callvirt, getValueMethod);
				}
			}, isDocumentFormat);

			if (endOfStatementLabel.HasValue)
			{
				body.MarkLabel(endOfStatementLabel.Value);
			}
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