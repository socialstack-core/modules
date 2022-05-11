using Api.SocketServerLibrary;
using Api.Startup;
using Lumity.BlockChains;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Api.BlockDatabase;

/// <summary>
/// Writes a field to the given writer in the chain format. Doesn't write the field id - only the value.
/// </summary>
/// <param name="obj"></param>
/// <param name="writer"></param>
public delegate void WriteChainField(object obj, Writer writer);

/// <summary>
/// Reads a field from the given transaction reader in the chain format.
/// </summary>
/// <param name="obj"></param>
/// <param name="fields"></param>
/// <param name="fieldIndex"></param>
/// <param name="isNullable"></param>
public delegate void ReadChainField(object obj, FieldData[] fields, int fieldIndex, bool isNullable);

/// <summary>
/// A class which generates readers and writers for fields of content types in bulk.
/// </summary>
public class ChainFieldIO
{
	private static int counter = 1;
	
	private ModuleBuilder _module;
	private TypeBuilder _type;
	private List<ContentField> _fields = new List<ContentField>();

	/// <summary>
	/// Get the fields in this field IO.
	/// </summary>
	public List<ContentField> Fields => _fields;

	/// <summary>
	/// Populates fields with suitable information for the given type.
	/// Exists primarily for testing.
	/// </summary>
	/// <param name="type"></param>
	public void GenerateForType(Type type)
	{
		var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

		foreach (var field in fields)
		{
			AddField(new ContentField(field));
		}

		if (_fields.Count == 0)
		{
			throw new Exception("The given type does not have any public/private instance fields.");
		}

		Bake();
	}

	/// <summary>
	/// Adds the given field to the set.
	/// </summary>
	public void AddField(ContentField field)
	{
		// field.FieldInfo
		
		if(_module == null){
			AssemblyName assemblyName = new AssemblyName("CFIO_" + counter);
			counter++;
			AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);
			_module = assemblyBuilder.DefineDynamicModule(assemblyName.Name);
			_type = _module.DefineType("IO", TypeAttributes.Public);
		}

		var index = _fields.Count;
		_fields.Add(field);

		var writeMethod = _type.DefineMethod("W" + index, MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig, typeof(void), new Type[] {
			typeof(object), // The object the field is in
			typeof(Writer) // The writer to write it to
		});

		var body = writeMethod.GetILGenerator();

		// Write it:
		GenerateWriteMethod(field, body);

		body.Emit(OpCodes.Ret);

		var readMethod = _type.DefineMethod("R" + index, MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig, typeof(void), new Type[] {
			typeof(object), // The object the field is in
			typeof(FieldData[]), // The field set
			typeof(int), // The field offset
			typeof(bool) // True if source is nullable
		});

		body = readMethod.GetILGenerator();

		/*
		// Put reader on stack:
		body.Emit(OpCodes.Ldarg_2);

		// Read the field value:
		body.Emit(OpCodes.Ldarg_1);
		body.Emit(OpCodes.Ldfld, field.FieldInfo);

		*/

		// Read it:
		GenerateReadMethod(field, body);

		body.Emit(OpCodes.Ret);

	}

	private MethodInfo writer_utf8;
	private MethodInfo writer_bytes;
	private MethodInfo writer_ulong;
	private MethodInfo writer_long;
	private MethodInfo writer_nulllong;
	private MethodInfo reader_string;
	private MethodInfo reader_bytes;
	private MethodInfo reader_wassigned;
	
	private MethodInfo reader_wassignedn_long;
	private MethodInfo reader_wassignedtn_long;
	private MethodInfo reader_wassignedn_int;
	private MethodInfo reader_wassignedtn_int;
	private MethodInfo reader_wassignedn_short;
	private MethodInfo reader_wassignedtn_short;
	private MethodInfo reader_wassignedn_sbyte;
	private MethodInfo reader_wassignedtn_sbyte;

	private MethodInfo reader_wasn_float;
	private MethodInfo reader_wastn_float;
	private MethodInfo reader_wasn_double;
	private MethodInfo reader_wastn_double;
	private MethodInfo reader_wassignedn_datetime;
	private MethodInfo reader_wassignedtn_datetime;

	private MethodInfo reader_wasn_ulong;
	private MethodInfo reader_wastn_ulong;
	private MethodInfo reader_wasn_uint;
	private MethodInfo reader_wastn_uint;
	private MethodInfo reader_wasn_ushort;
	private MethodInfo reader_wastn_ushort;
	private MethodInfo reader_wasn_byte;
	private MethodInfo reader_wastn_byte;

	private MethodInfo reader_wassignednn;
	private MethodInfo reader_wasnn;
	private MethodInfo reader_wasnn_float;
	private MethodInfo reader_tofloat;
	private MethodInfo reader_todouble;
	private MethodInfo reader_todatetime;
	private MethodInfo reader_wasnn_double;
	private MethodInfo reader_wasnn_datetime;
	private FieldInfo reader_numeric;

	/// <summary>
	/// Interprets orig as a signed, not nullable number.
	/// </summary>
	/// <param name="orig"></param>
	/// <returns></returns>
	public static long WasSigned(ulong orig)
	{
		if ((orig & 1) == 1)
		{
			return -(long)(orig >> 1);
		}

		return (long)orig >> 1;
	}

	/// <summary>
	/// Interprets orig as a nullable signed value, except null is treated as a 0.
	/// </summary>
	/// <param name="orig"></param>
	/// <returns></returns>
	public static long WasSignedNotNull(ulong orig)
	{
		if (orig == 0)
		{
			return 0;
		}

		orig--;

		if ((orig & 1) == 1)
		{
			return -(long)(orig >> 1);
		}

		return (long)orig >> 1;
	}

	/// <summary>
	/// Same as was signed except outputs a nullable long.
	/// </summary>
	/// <param name="orig"></param>
	/// <returns></returns>
	public static sbyte? WasSignedTargetNullable_Sbyte(ulong orig)
	{
		if ((orig & 1) == 1)
		{
			return (sbyte)-((byte)(orig >> 1));
		}

		return (sbyte)(orig >> 1);
	}

	/// <summary>
	/// Interprets orig as a nullable signed value.
	/// </summary>
	/// <param name="orig"></param>
	/// <returns></returns>
	public static sbyte? WasSignedNullable_Sbyte(ulong orig)
	{
		if (orig == 0)
		{
			return null;
		}

		orig--;

		if ((orig & 1) == 1)
		{
			return (sbyte)-((byte)(orig >> 1));
		}

		return (sbyte)(orig >> 1);
	}

	/// <summary>
	/// Same as was signed except outputs a nullable long.
	/// </summary>
	/// <param name="orig"></param>
	/// <returns></returns>
	public static short? WasSignedTargetNullable_Short(ulong orig)
	{
		if ((orig & 1) == 1)
		{
			return (short)-((ushort)(orig >> 1));
		}

		return (short)(orig >> 1);
	}

	/// <summary>
	/// Interprets orig as a nullable signed value.
	/// </summary>
	/// <param name="orig"></param>
	/// <returns></returns>
	public static short? WasSignedNullable_Short(ulong orig)
	{
		if (orig == 0)
		{
			return null;
		}

		orig--;

		if ((orig & 1) == 1)
		{
			return (short)-((ushort)(orig >> 1));
		}

		return (short)(orig >> 1);
	}

	/// <summary>
	/// Same as was signed except outputs a nullable long.
	/// </summary>
	/// <param name="orig"></param>
	/// <returns></returns>
	public static DateTime? WasSignedTargetNullable_Datetime(ulong orig)
	{
		if ((orig & 1) == 1)
		{
			return new DateTime(-(long)(orig >> 1));
		}

		return new DateTime((long)orig >> 1);
	}

	/// <summary>
	/// Interprets orig as a nullable signed value.
	/// </summary>
	/// <param name="orig"></param>
	/// <returns></returns>
	public static DateTime? WasSignedNullable_Datetime(ulong orig)
	{
		if (orig == 0)
		{
			return null;
		}

		orig--;

		if ((orig & 1) == 1)
		{
			return new DateTime(-(long)(orig >> 1));
		}

		return new DateTime((long)orig >> 1);
	}
	
	/// <summary>
	/// Same as was signed except outputs a nullable long.
	/// </summary>
	/// <param name="orig"></param>
	/// <returns></returns>
	public static int? WasSignedTargetNullable_Int(ulong orig)
	{
		if ((orig & 1) == 1)
		{
			return -(int)(orig >> 1);
		}

		return (int)orig >> 1;
	}

	/// <summary>
	/// Interprets orig as a nullable signed value.
	/// </summary>
	/// <param name="orig"></param>
	/// <returns></returns>
	public static int? WasSignedNullable_Int(ulong orig)
	{
		if (orig == 0)
		{
			return null;
		}

		orig--;

		if ((orig & 1) == 1)
		{
			return -(int)(orig >> 1);
		}

		return (int)orig >> 1;
	}

	/// <summary>
	/// Same as was signed except outputs a nullable long.
	/// </summary>
	/// <param name="orig"></param>
	/// <returns></returns>
	public static long? WasSignedTargetNullable_Long(ulong orig)
	{
		if ((orig & 1) == 1)
		{
			return -(long)(orig >> 1);
		}

		return (long)orig >> 1;
	}

	/// <summary>
	/// Interprets orig as a nullable signed value.
	/// </summary>
	/// <param name="orig"></param>
	/// <returns></returns>
	public static long? WasSignedNullable_Long(ulong orig)
	{
		if (orig == 0)
		{
			return null;
		}

		orig--;

		if ((orig & 1) == 1)
		{
			return -(long)(orig >> 1);
		}

		return (long)orig >> 1;
	}

	/// <summary>
	/// Converts the given value to a float.
	/// </summary>
	/// <param name="orig"></param>
	/// <returns></returns>
	public static float ToFloat(ulong orig)
	{
		return new FloatBits((uint)orig).Float;
	}

	/// <summary>
	/// Converts the given value to a double.
	/// </summary>
	/// <param name="orig"></param>
	/// <returns></returns>
	public static double ToDouble(ulong orig)
	{
		return new DoubleBits(orig).Double;
	}
	
	/// <summary>
	/// Converts the given value to a datetime.
	/// </summary>
	/// <param name="orig"></param>
	/// <returns></returns>
	public static DateTime ToDateTime(ulong orig)
	{
		// It is simply the value as a long:
		return new DateTime((long)orig);
	}

	/// <summary>
	/// Interprets orig as a nullable value, but null is returned as 0 (float).
	/// </summary>
	/// <param name="orig"></param>
	/// <returns></returns>
	public static float WasNullableNotNull_Float(ulong orig)
	{
		if (orig == 0)
		{
			return 0f;
		}

		uint bits = (uint)(orig - 1);
		return new FloatBits(bits).Float;
	}
	
	/// <summary>
	/// Interprets orig as a nullable value, but null is returned as 0 (double).
	/// </summary>
	/// <param name="orig"></param>
	/// <returns></returns>
	public static double WasNullableNotNull_Double(ulong orig)
	{
		if (orig == 0)
		{
			return 0f;
		}

		ulong bits = (orig - 1);
		return new DoubleBits(bits).Double;
	}
	
	/// <summary>
	/// Interprets orig as a nullable value, but null is returned as 0 ticks (DateTime).
	/// </summary>
	/// <param name="orig"></param>
	/// <returns></returns>
	public static DateTime WasNullableNotNull_Datetime(ulong orig)
	{
		if (orig == 0)
		{
			return new DateTime((long)0);
		}

		orig--;
		return new DateTime((long)orig);
	}
	
	/// <summary>
	/// Interprets orig as a nullable value, but null is returned as 0.
	/// </summary>
	/// <param name="orig"></param>
	/// <returns></returns>
	public static ulong WasNullableNotNull(ulong orig)
	{
		if (orig == 0)
		{
			return 0;
		}

		return (orig - 1);
	}

	/// <summary>
	/// Interprets orig as a 1 byte nullable value.
	/// </summary>
	/// <param name="orig"></param>
	/// <returns></returns>
	public static byte? WasNullableTargetNullable_Byte(ulong orig)
	{
		if (orig == 0)
		{
			return null;
		}

		return (byte)(orig - 1);
	}

	/// <summary>
	/// Interprets orig as a nullable value. If null is encountered, returns 0.
	/// </summary>
	/// <param name="orig"></param>
	/// <returns></returns>
	public static byte WasNullable_Byte(ulong orig)
	{
		if (orig == 0)
		{
			return 0;
		}

		return (byte)(orig - 1);
	}

	/// <summary>
	/// Interprets orig as a 2 byte nullable value.
	/// </summary>
	/// <param name="orig"></param>
	/// <returns></returns>
	public static ushort? WasNullableTargetNullable_Ushort(ulong orig)
	{
		if (orig == 0)
		{
			return null;
		}

		return (ushort)(orig - 1);
	}

	/// <summary>
	/// Interprets orig as a nullable value. If null is encountered, returns 0.
	/// </summary>
	/// <param name="orig"></param>
	/// <returns></returns>
	public static ushort WasNullable_Ushort(ulong orig)
	{
		if (orig == 0)
		{
			return 0;
		}

		return (ushort)(orig - 1);
	}

	/// <summary>
	/// Interprets orig as a 4 byte nullable value.
	/// </summary>
	/// <param name="orig"></param>
	/// <returns></returns>
	public static uint? WasNullableTargetNullable_Uint(ulong orig)
	{
		if (orig == 0)
		{
			return null;
		}

		return (uint)(orig - 1);
	}

	/// <summary>
	/// Interprets orig as a nullable value. If null is encountered, returns 0.
	/// </summary>
	/// <param name="orig"></param>
	/// <returns></returns>
	public static uint WasNullable_Uint(ulong orig)
	{
		if (orig == 0)
		{
			return 0;
		}

		return (uint)(orig - 1);
	}

	/// <summary>
	/// Interprets orig as an 8 byte nullable value.
	/// </summary>
	/// <param name="orig"></param>
	/// <returns></returns>
	public static ulong? WasNullableTargetNullable_Ulong(ulong orig)
	{
		if (orig == 0)
		{
			return null;
		}

		return (ulong)(orig - 1);
	}

	/// <summary>
	/// Interprets orig as a nullable value. If null is encountered, returns 0.
	/// </summary>
	/// <param name="orig"></param>
	/// <returns></returns>
	public static ulong WasNullable_Ulong(ulong orig)
	{
		if (orig == 0)
		{
			return 0;
		}

		return (ulong)(orig - 1);
	}
	
	/// <summary>
	/// Interprets orig as an 4 byte nullable value.
	/// </summary>
	/// <param name="orig"></param>
	/// <returns></returns>
	public static float? WasNullableTargetNullable_Float(ulong orig)
	{
		if (orig == 0)
		{
			return null;
		}

		return new FloatBits((uint)(orig - 1)).Float;
	}

	/// <summary>
	/// Interprets orig as a nullable value. If null is encountered, returns 0.
	/// </summary>
	/// <param name="orig"></param>
	/// <returns></returns>
	public static float WasNullable_Float(ulong orig)
	{
		if (orig == 0)
		{
			return 0f;
		}

		return new FloatBits((uint)(orig - 1)).Float;
	}
	
	/// <summary>
	/// Converts the given float to a ulong.
	/// </summary>
	/// <param name="orig"></param>
	/// <returns></returns>
	public static ulong FromFloat(float orig)
	{
		return new FloatBits(orig).Int;
	}
	
	/// <summary>
	/// Converts the given float to a ulong.
	/// </summary>
	/// <param name="orig"></param>
	/// <returns></returns>
	public static ulong FromFloatNullable(float orig)
	{
		// It's converted to a ulong straight away to avoid float.MaxValue+1 overflowing a uint.
		var result = (ulong)new FloatBits(orig).Int;
		result += 1;
		return result;
	}
	
	/// <summary>
	/// Converts the given double to a ulong.
	/// </summary>
	/// <param name="orig"></param>
	/// <returns></returns>
	public static ulong FromDouble(double orig)
	{
		return new DoubleBits(orig).Int;
	}
	
	/// <summary>
	/// Converts the given float to a ulong.
	/// </summary>
	/// <param name="orig"></param>
	/// <returns></returns>
	public static ulong FromDoubleNullable(double orig)
	{
		var result = new DoubleBits(orig).Int;

		// Note that double.MaxValue will overflow to a null.
		// Would need 16 byte number support to avoid that one.
		result += 1;
		return result;
	}
	
	/// <summary>
	/// Interprets orig as an 8 byte nullable value.
	/// </summary>
	/// <param name="orig"></param>
	/// <returns></returns>
	public static double? WasNullableTargetNullable_Double(ulong orig)
	{
		if (orig == 0)
		{
			return null;
		}

		return new DoubleBits((uint)(orig - 1)).Double;
	}

	/// <summary>
	/// Interprets orig as a nullable value. If null is encountered, returns 0.
	/// </summary>
	/// <param name="orig"></param>
	/// <returns></returns>
	public static double WasNullable_Double(ulong orig)
	{
		if (orig == 0)
		{
			return 0f;
		}

		return new DoubleBits((uint)(orig - 1)).Double;
	}
	
	private void GenerateReadMethod(ContentField contentField, ILGenerator body)
	{
		var fieldType = contentField.FieldType;
		var targetIsNullable = false;

		var underlying = Nullable.GetUnderlyingType(fieldType);

		if (underlying != null)
		{
			targetIsNullable = true;
			fieldType = underlying;
		}

		if (fieldType == typeof(string))
		{
			body.Emit(OpCodes.Ldarg_0); // The object

			body.Emit(OpCodes.Ldarg_1);                     // Reader.
			body.Emit(OpCodes.Ldarg_2);                     // x
			body.Emit(OpCodes.Ldelema, typeof(FieldData));  // [x]
			if (reader_string == null)
			{
				reader_string = typeof(FieldData).GetMethod("GetNativeString");
			}

			body.Emit(OpCodes.Call, reader_string);     // .GetNativeString();
			body.Emit(OpCodes.Stfld, contentField.FieldInfo); // theField = {value};
			return;
		}
		else if (fieldType == typeof(byte[]))
		{
			body.Emit(OpCodes.Ldarg_0); // The object

			body.Emit(OpCodes.Ldarg_1);                     // Reader fields
			body.Emit(OpCodes.Ldarg_2);                     // x
			body.Emit(OpCodes.Ldelema, typeof(FieldData));  // [x]
			if (reader_bytes == null)
			{
				reader_bytes = typeof(FieldData).GetMethod("GetBytes");
			}

			body.Emit(OpCodes.Call, reader_bytes); // .GetBytes();
			body.Emit(OpCodes.Stfld, contentField.FieldInfo); // theField = {value};
			return;
		}

		// Everything else uses numeric value read, so output that now:
		if (reader_numeric == null)
		{
			reader_numeric = typeof(FieldData).GetField("NumericValue");
			reader_wassigned = typeof(ChainFieldIO).GetMethod(nameof(WasSigned), BindingFlags.Public | BindingFlags.Static);
			reader_wassignednn = typeof(ChainFieldIO).GetMethod(nameof(WasSignedNotNull), BindingFlags.Public | BindingFlags.Static);
			reader_wasnn = typeof(ChainFieldIO).GetMethod(nameof(WasNullableNotNull), BindingFlags.Public | BindingFlags.Static);
			reader_wasnn_float = typeof(ChainFieldIO).GetMethod(nameof(WasNullableNotNull_Float), BindingFlags.Public | BindingFlags.Static);
			reader_wasnn_double = typeof(ChainFieldIO).GetMethod(nameof(WasNullableNotNull_Double), BindingFlags.Public | BindingFlags.Static);
			reader_wasnn_datetime = typeof(ChainFieldIO).GetMethod(nameof(WasNullableNotNull_Datetime), BindingFlags.Public | BindingFlags.Static);

			reader_tofloat = typeof(ChainFieldIO).GetMethod(nameof(ToFloat), BindingFlags.Public | BindingFlags.Static);
			reader_todouble = typeof(ChainFieldIO).GetMethod(nameof(ToDouble), BindingFlags.Public | BindingFlags.Static);
			reader_todatetime = typeof(ChainFieldIO).GetMethod(nameof(ToDateTime), BindingFlags.Public | BindingFlags.Static);

			reader_wasn_float = typeof(ChainFieldIO).GetMethod(nameof(WasNullable_Float), BindingFlags.Public | BindingFlags.Static);
			reader_wastn_float = typeof(ChainFieldIO).GetMethod(nameof(WasNullableTargetNullable_Float), BindingFlags.Public | BindingFlags.Static);
			reader_wasn_double = typeof(ChainFieldIO).GetMethod(nameof(WasNullable_Double), BindingFlags.Public | BindingFlags.Static);
			reader_wastn_double = typeof(ChainFieldIO).GetMethod(nameof(WasNullableTargetNullable_Double), BindingFlags.Public | BindingFlags.Static);
			reader_wassignedn_datetime = typeof(ChainFieldIO).GetMethod(nameof(WasSignedNullable_Datetime), BindingFlags.Public | BindingFlags.Static);
			reader_wassignedtn_datetime = typeof(ChainFieldIO).GetMethod(nameof(WasSignedTargetNullable_Datetime), BindingFlags.Public | BindingFlags.Static);

			// Unsigned
			reader_wasn_ulong = typeof(ChainFieldIO).GetMethod(nameof(WasNullable_Ulong), BindingFlags.Public | BindingFlags.Static);
			reader_wastn_ulong = typeof(ChainFieldIO).GetMethod(nameof(WasNullableTargetNullable_Ulong), BindingFlags.Public | BindingFlags.Static);

			reader_wasn_uint = typeof(ChainFieldIO).GetMethod(nameof(WasNullable_Uint), BindingFlags.Public | BindingFlags.Static);
			reader_wastn_uint = typeof(ChainFieldIO).GetMethod(nameof(WasNullableTargetNullable_Uint), BindingFlags.Public | BindingFlags.Static);

			reader_wasn_ushort = typeof(ChainFieldIO).GetMethod(nameof(WasNullable_Ushort), BindingFlags.Public | BindingFlags.Static);
			reader_wastn_ushort = typeof(ChainFieldIO).GetMethod(nameof(WasNullableTargetNullable_Ushort), BindingFlags.Public | BindingFlags.Static);

			reader_wasn_byte = typeof(ChainFieldIO).GetMethod(nameof(WasNullable_Byte), BindingFlags.Public | BindingFlags.Static);
			reader_wastn_byte = typeof(ChainFieldIO).GetMethod(nameof(WasNullableTargetNullable_Byte), BindingFlags.Public | BindingFlags.Static);
			
			// Signed
			reader_wassignedn_long = typeof(ChainFieldIO).GetMethod(nameof(WasSignedNullable_Long), BindingFlags.Public | BindingFlags.Static);
			reader_wassignedtn_long = typeof(ChainFieldIO).GetMethod(nameof(WasSignedTargetNullable_Long), BindingFlags.Public | BindingFlags.Static);

			reader_wassignedn_int = typeof(ChainFieldIO).GetMethod(nameof(WasSignedNullable_Int), BindingFlags.Public | BindingFlags.Static);
			reader_wassignedtn_int = typeof(ChainFieldIO).GetMethod(nameof(WasSignedTargetNullable_Int), BindingFlags.Public | BindingFlags.Static);

			reader_wassignedn_short = typeof(ChainFieldIO).GetMethod(nameof(WasSignedNullable_Short), BindingFlags.Public | BindingFlags.Static);
			reader_wassignedtn_short = typeof(ChainFieldIO).GetMethod(nameof(WasSignedTargetNullable_Short), BindingFlags.Public | BindingFlags.Static);
			
			reader_wassignedn_sbyte = typeof(ChainFieldIO).GetMethod(nameof(WasSignedNullable_Sbyte), BindingFlags.Public | BindingFlags.Static);
			reader_wassignedtn_sbyte = typeof(ChainFieldIO).GetMethod(nameof(WasSignedTargetNullable_Sbyte), BindingFlags.Public | BindingFlags.Static);

		}

		// Read numeric value:
		body.Emit(OpCodes.Ldarg_0);
		body.Emit(OpCodes.Ldarg_1);                     // Reader fields
		body.Emit(OpCodes.Ldarg_2);                     // x
		body.Emit(OpCodes.Ldelema, typeof(FieldData));  // [x]
		body.Emit(OpCodes.Ldfld, reader_numeric);

		if (fieldType == typeof(bool))
		{
			if (targetIsNullable)
			{
				// Source may be 'bool' or 'bool?'

				var nBoolLocal = body.DeclareLocal(typeof(bool?));

				body.Emit(OpCodes.Ldarg_3); // True if source is nullable
				var isNotNullableLabel = body.DefineLabel();
				body.Emit(OpCodes.Brfalse_S, isNotNullableLabel);
				{
					// Source is nullable, target is nullable.
					body.Emit(OpCodes.Conv_I4); // Convert the number to int32.
					body.Emit(OpCodes.Dup);
					var isZeroLabel = body.DefineLabel();
					body.Emit(OpCodes.Brfalse_S, isZeroLabel);
					{
						// Fell in here indicating the value is non-zero. 1=false, 2=true.
						body.Emit(OpCodes.Ldc_I4_2);
						body.Emit(OpCodes.Ceq); // nv == 2
						var isOneLabel = body.DefineLabel();
						body.Emit(OpCodes.Brfalse_S, isOneLabel);
						{
							// It's equal to 2 (true)
							body.Emit(OpCodes.Ldloca, nBoolLocal);
							body.Emit(OpCodes.Ldc_I4_1);
						}
						body.MarkLabel(isOneLabel);
						{
							// It's equal to 1 (false)
							body.Emit(OpCodes.Ldloca, nBoolLocal);
							body.Emit(OpCodes.Ldc_I4_0);
						}
						body.Emit(OpCodes.Call, typeof(bool?).GetConstructor(new Type[] { typeof(bool) }));
					}
					body.MarkLabel(isZeroLabel);
					{
						// It's zero (null)
						body.Emit(OpCodes.Pop);
						body.Emit(OpCodes.Ldloca, nBoolLocal);
						body.Emit(OpCodes.Initobj, typeof(bool?));
					}
				}
				body.MarkLabel(isNotNullableLabel);
				{
					// Source is not nullable, target is.
					body.Emit(OpCodes.Conv_I4); // Convert the number to int32.

					var isZeroLabel = body.DefineLabel();
					body.Emit(OpCodes.Brfalse_S, isZeroLabel);
					{
						// Fell in here indicating the value is non-zero. Output true.
						body.Emit(OpCodes.Ldloca, nBoolLocal);
						body.Emit(OpCodes.Ldc_I4_1);
						body.Emit(OpCodes.Call, typeof(bool?).GetConstructor(new Type[] { typeof(bool) }));
					}
					body.MarkLabel(isZeroLabel);
					{
						// It's zero
						body.Emit(OpCodes.Ldloca, nBoolLocal);
						body.Emit(OpCodes.Ldc_I4_0);
						body.Emit(OpCodes.Call, typeof(bool?).GetConstructor(new Type[] { typeof(bool) }));
					}
				}

				body.Emit(OpCodes.Ldloc, nBoolLocal);
				body.Emit(OpCodes.Stfld, contentField.FieldInfo);
			}
			else
			{
				// Source may be 'bool' or 'bool?'
				
				body.Emit(OpCodes.Ldarg_3); // True if source is nullable
				var isNotNullableLabel = body.DefineLabel();
				body.Emit(OpCodes.Brfalse_S, isNotNullableLabel);
				{
					// Source is nullable, target is not. Null will be translated to 'false' via nv>1
					body.Emit(OpCodes.Conv_I4); // Convert the number to int32.
					body.Emit(OpCodes.Ldc_I4_1);
					body.Emit(OpCodes.Cgt); // nv>1
					body.Emit(OpCodes.Stfld, contentField.FieldInfo);
					body.Emit(OpCodes.Ret);
				}
				body.MarkLabel(isNotNullableLabel);
				{
					// Source or target are not nullable.
					body.Emit(OpCodes.Conv_I4); // Convert the number to int32.
					
					var isZeroLabel = body.DefineLabel();
					body.Emit(OpCodes.Brfalse_S, isZeroLabel);
					{
						// Fell in here indicating the value is non-zero.
						body.Emit(OpCodes.Ldc_I4_1);
						body.Emit(OpCodes.Stfld, contentField.FieldInfo);
						body.Emit(OpCodes.Ret);
					}
					body.MarkLabel(isZeroLabel);
					{
						// It's zero
						body.Emit(OpCodes.Ldc_I4_0);
						body.Emit(OpCodes.Stfld, contentField.FieldInfo);
					}
				}
			}
		}
		else if (fieldType == typeof(sbyte))
		{
			ReadSignedField(body, targetIsNullable, OpCodes.Conv_I1, contentField.FieldInfo, reader_wassignedn_sbyte, reader_wassignedtn_sbyte);
		}
		else if (fieldType == typeof(byte))
		{
			ReadUnsignedField(body, targetIsNullable, contentField.FieldInfo, reader_wasn_byte, reader_wastn_byte, OpCodes.Conv_I1);
		}
		else if (fieldType == typeof(int))
		{
			ReadSignedField(body, targetIsNullable, OpCodes.Conv_I4, contentField.FieldInfo, reader_wassignedn_int, reader_wassignedtn_int);
		}
		else if (fieldType == typeof(uint))
		{
			ReadUnsignedField(body, targetIsNullable, contentField.FieldInfo, reader_wasn_uint, reader_wastn_uint, OpCodes.Conv_I4);
		}
		else if (fieldType == typeof(short))
		{
			ReadSignedField(body, targetIsNullable, OpCodes.Conv_I2, contentField.FieldInfo, reader_wassignedn_short, reader_wassignedtn_short);
		}
		else if (fieldType == typeof(ushort))
		{
			ReadUnsignedField(body, targetIsNullable, contentField.FieldInfo, reader_wasn_ushort, reader_wastn_ushort, OpCodes.Conv_I2);
		}
		else if (fieldType == typeof(long))
		{
			ReadSignedField(body, targetIsNullable, OpCodes.Nop, contentField.FieldInfo, reader_wassignedn_long, reader_wassignedtn_long);
		}
		else if (fieldType == typeof(ulong))
		{
			ReadUnsignedField(body, targetIsNullable, contentField.FieldInfo, reader_wasn_ulong, reader_wastn_ulong, OpCodes.Nop);
		}
		else if (fieldType == typeof(float))
		{
			ReadGeneralField(body, targetIsNullable, contentField.FieldInfo, reader_wasn_float, reader_wastn_float, reader_wasnn_float, reader_tofloat, OpCodes.Nop);
		}
		else if (fieldType == typeof(DateTime))
		{
			ReadGeneralField(body, targetIsNullable, contentField.FieldInfo, reader_wassignedn_datetime, reader_wassignedtn_datetime, reader_wasnn_datetime, reader_todatetime, OpCodes.Nop);
		}
		else if (fieldType == typeof(double))
		{
			ReadGeneralField(body, targetIsNullable, contentField.FieldInfo, reader_wasn_double, reader_wastn_double, reader_wasnn_double, reader_todouble, OpCodes.Nop);
		}
		else
		{
			// Pop the numeric value and the object ref
			body.Emit(OpCodes.Pop);
			body.Emit(OpCodes.Pop);
		}
	}

	private void ReadNullableField(ILGenerator body, FieldInfo fieldInfo, MethodInfo srcNullable, MethodInfo targNullable)
	{
		// Source may be T or T?
		body.Emit(OpCodes.Ldarg_3); // True if source is nullable
		var isNotNullableLabel = body.DefineLabel();
		body.Emit(OpCodes.Brfalse_S, isNotNullableLabel);
		{
			// Source is nullable. Target is nullable. Null will be translated to 0.
			body.Emit(OpCodes.Call, srcNullable);
			body.Emit(OpCodes.Stfld, fieldInfo);
			body.Emit(OpCodes.Ret);
		}
		body.MarkLabel(isNotNullableLabel);
		{
			// Source is not nullable. Target is nullable.
			body.Emit(OpCodes.Call, targNullable);
			body.Emit(OpCodes.Stfld, fieldInfo);
		}

		// No conversions necessary as we output the correct nullable onto the stack.
	}

	private void ReadGeneralField(ILGenerator body, bool targetIsNullable, FieldInfo fieldInfo, MethodInfo srcNullable, MethodInfo targNullable, MethodInfo srcNullableNT, MethodInfo noneNullable, System.Reflection.Emit.OpCode convertTo)
	{
		if (targetIsNullable)
		{
			ReadNullableField(body, fieldInfo, srcNullable, targNullable);
		}
		else
		{
			// Source may be T or T?

			body.Emit(OpCodes.Ldarg_3); // True if source is nullable
			var isNotNullableLabel = body.DefineLabel();
			body.Emit(OpCodes.Brfalse_S, isNotNullableLabel);
			{
				// Source is nullable. Target is not nullable. Null will be translated to 0.
				if (srcNullableNT != null)
				{
					body.Emit(OpCodes.Call, srcNullableNT);
				}
				if (convertTo != OpCodes.Nop)
				{
					body.Emit(convertTo);
				}
				body.Emit(OpCodes.Stfld, fieldInfo);
				body.Emit(OpCodes.Ret);
			}
			body.MarkLabel(isNotNullableLabel);
			{
				// Source is not nullable. Target is not nullable. Value is optionally converted depending on the fields needs.
				if (noneNullable != null)
				{
					body.Emit(OpCodes.Call, noneNullable);
				}
				if (convertTo != OpCodes.Nop)
				{
					body.Emit(convertTo);
				}
				body.Emit(OpCodes.Stfld, fieldInfo);
			}

		}
	}

	private void ReadUnsignedField(ILGenerator body, bool targetIsNullable, FieldInfo fieldInfo, MethodInfo srcNullable, MethodInfo targNullable, System.Reflection.Emit.OpCode convertTo)
	{
		ReadGeneralField(body, targetIsNullable, fieldInfo, srcNullable, targNullable, reader_wasnn, null, convertTo);
	}

	private void ReadSignedField(ILGenerator body, bool targetIsNullable, System.Reflection.Emit.OpCode convertTo, FieldInfo fieldInfo, MethodInfo srcNullable, MethodInfo targNullable)
	{
		ReadGeneralField(body, targetIsNullable, fieldInfo, srcNullable, targNullable, reader_wassignednn, reader_wassigned, convertTo);
	}

	private void GenerateWriteMethod(ContentField field, ILGenerator body)
	{
		var fieldType = field.FieldType;
		var isNullable = false;

		var underlying = Nullable.GetUnderlyingType(fieldType);

		if (underlying != null)
		{
			isNullable = true;
			var hasValueProperty = fieldType.GetProperty("HasValue").GetGetMethod();
			var valueProperty = fieldType.GetProperty("Value").GetGetMethod();
			fieldType = underlying;

			// Get the field address and read HasValue:
			body.Emit(OpCodes.Ldarg_0);
			body.Emit(OpCodes.Ldflda, field.FieldInfo);
			body.Emit(OpCodes.Call, hasValueProperty);
			
			// If we don't have a value, write a 0 and stop.
			// Otherwise, read the actual value and put it on the stack.
			var afterNullCheck = body.DefineLabel();
			body.Emit(OpCodes.Brtrue_S, afterNullCheck);
			{
				// It's null! Write a 0. Even though it's technically a compressed number, a single 0 byte is the same.

				// Put writer on stack:
				body.Emit(OpCodes.Ldarg_1);

				var writeByteMethod = typeof(Writer).GetMethod("Write", new Type[] { typeof(byte) });

				body.Emit(OpCodes.Ldc_I4_0);
				// body.Emit(OpCodes.Conv_I1);
				body.Emit(OpCodes.Call, writeByteMethod);
				body.Emit(OpCodes.Ret);
			}

			body.MarkLabel(afterNullCheck);

			// It's not null here.

			// Put writer on stack:
			body.Emit(OpCodes.Ldarg_1);
			
			// Read the value:
			body.Emit(OpCodes.Ldarg_0);
			body.Emit(OpCodes.Ldflda, field.FieldInfo);
			body.Emit(OpCodes.Call, valueProperty);
		}
		else
		{
			// Put writer on stack:
			body.Emit(OpCodes.Ldarg_1);

			// Read the field value:
			body.Emit(OpCodes.Ldarg_0);
			body.Emit(OpCodes.Ldfld, field.FieldInfo);
		}

		// At this point, the value is on the stack.

		if (fieldType == typeof(string))
		{
			if (writer_utf8 == null)
			{
				writer_utf8 = typeof(Writer).GetMethod("WriteInvertibleUTF8");
			}

			body.Emit(OpCodes.Call, writer_utf8);
		}
		else if (fieldType == typeof(byte[]))
		{
			if (writer_bytes == null)
			{
				writer_bytes = typeof(Writer).GetMethod("WriteInvertible", new Type[] { typeof(byte[]) });
			}

			body.Emit(OpCodes.Call, writer_bytes);
		}
		else if (fieldType == typeof(bool) || fieldType == typeof(byte) || fieldType == typeof(ushort) ||  fieldType == typeof(uint) || fieldType == typeof(ulong))
		{
			// Unsigned values.
			// Simply add 1 if it was originally nullable.

			if (fieldType != typeof(ulong))
			{
				body.Emit(OpCodes.Conv_U8); // Ensure we have a ulong
			}

			if (isNullable)
			{
				// Add 1:
				body.Emit(OpCodes.Ldc_I4_1);
				body.Emit(OpCodes.Conv_U8);
				body.Emit(OpCodes.Add);
			}

			if (writer_ulong == null)
			{
				writer_ulong = typeof(Writer).GetMethod(nameof(Writer.WriteInvertibleCompressed), new Type[] { typeof(ulong) });
			}

			body.Emit(OpCodes.Call, writer_ulong);
		}
		else if (fieldType == typeof(sbyte) || fieldType == typeof(short) || fieldType == typeof(int) || fieldType == typeof(long))
		{
			if (fieldType != typeof(ulong))
			{
				body.Emit(OpCodes.Conv_I8); // Ensure we have a long
			}

			// In the nullable case here we can't add 1 as -1 would then be treated as a null.
			// Must use a specialised method instead.

			if (isNullable)
			{
				if (writer_nulllong == null)
				{
					writer_nulllong = typeof(Writer).GetMethod(nameof(Writer.WriteInvertibleCompressedSignedNullable));
				}

				body.Emit(OpCodes.Call, writer_nulllong);
			}
			else
			{
				if (writer_long == null)
				{
					writer_long = typeof(Writer).GetMethod(nameof(Writer.WriteInvertibleCompressedSigned), new Type[] { typeof(long) });
				}

				body.Emit(OpCodes.Call, writer_long);
			}
		}
		else if (fieldType == typeof(float))
		{
			// Convert via FloatBits by calling FromFloat
			
			if (isNullable)
			{
				var writer_nullfloat = typeof(ChainFieldIO).GetMethod(nameof(FromFloatNullable));
				body.Emit(OpCodes.Call, writer_nullfloat);
			}
			else
			{
				var writer_float = typeof(ChainFieldIO).GetMethod(nameof(FromFloat));
				body.Emit(OpCodes.Call, writer_float);
			}

			// There is now a ulong on the stack which needs to be written out:
			if (writer_ulong == null)
			{
				writer_ulong = typeof(Writer).GetMethod(nameof(Writer.WriteInvertibleCompressed), new Type[] { typeof(ulong) });
			}

			body.Emit(OpCodes.Call, writer_ulong);
		}
		else if (fieldType == typeof(double))
		{
			// Convert via DoubleBits by calling FromDouble
			
			if (isNullable)
			{
				var writer_nulldouble = typeof(ChainFieldIO).GetMethod(nameof(FromDoubleNullable));
				body.Emit(OpCodes.Call, writer_nulldouble);
			}
			else
			{
				var writer_double = typeof(ChainFieldIO).GetMethod(nameof(FromDouble));
				body.Emit(OpCodes.Call, writer_double);
			}

			// There is now a ulong on the stack which needs to be written out:
			if (writer_ulong == null)
			{
				writer_ulong = typeof(Writer).GetMethod(nameof(Writer.WriteInvertibleCompressed), new Type[] { typeof(ulong) });
			}

			body.Emit(OpCodes.Call, writer_ulong);

		}
		else if (fieldType == typeof(DateTime))
		{
			// Read the ticks by storing the DateTime into a local:
			var loc = body.DeclareLocal(typeof(DateTime));
			body.Emit(OpCodes.Stloc, loc);
			body.Emit(OpCodes.Ldloca, loc);

			var ticksMethod = typeof(DateTime).GetProperty("Ticks").GetGetMethod();

			body.Emit(OpCodes.Call, ticksMethod);

			// Convert to ulong (from long):
			body.Emit(OpCodes.Conv_U8);

			// writer+ulong is now on the stack. It's written out like a regular unsigned value is:
			if (isNullable)
			{
				// Add 1:
				body.Emit(OpCodes.Ldc_I4_1);
				body.Emit(OpCodes.Conv_U8);
				body.Emit(OpCodes.Add);
			}

			if (writer_ulong == null)
			{
				writer_ulong = typeof(Writer).GetMethod(nameof(Writer.WriteInvertibleCompressed), new Type[] { typeof(ulong) });
			}

			body.Emit(OpCodes.Call, writer_ulong);
		}
	}

	/// <summary>
	/// Completes this set.
	/// </summary>
	public void Bake()
	{
		// Finish the type.
		Type compiledType = _type.CreateType();

		// For each field, get the delegates now:
		var allMethods = compiledType.GetMethods();

		foreach (var method in allMethods)
		{
			if (method.Name[0] == 'R')
			{
				var index = int.Parse(method.Name.Substring(1));
				var reader = method.CreateDelegate<ReadChainField>();
				_fields[index].FieldReader = reader;
			}
			else if (method.Name[0] == 'W')
			{
				var index = int.Parse(method.Name.Substring(1));
				var writer = method.CreateDelegate<WriteChainField>();
				_fields[index].FieldWriterMethodInfo = method;
				_fields[index].FieldWriter = writer;
			}

		}

	}

}
