using Api.SocketServerLibrary;
using Lumity.BlockChains;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Api.BlockDatabase;


/// <summary>
/// Stores general information about a given table stored on a blockchain.
/// </summary>
public class BlockTableMeta
{
	/// <summary>
	/// Definition ID
	/// </summary>
	public ulong Id;
	/// <summary>
	/// Current field count plus one
	/// </summary>
	public uint FieldCountPlusOne;
	/// <summary>
	/// The definition
	/// </summary>
	public Definition Definition;
	/// <summary>
	/// The source type that the definition is derived from
	/// </summary>
	public Type Type;
	/// <summary>
	/// The chain it's on
	/// </summary>
	public BlockChain Chain;

	public Dictionary<ulong, BlockTableField> FieldLookup = new Dictionary<ulong, BlockTableField>();
	
	public List<BlockTableField> Fields = new List<BlockTableField>();

	public void Completed()
	{
		FieldCountPlusOne = (uint)(Fields.Count + 1);
	}

	private long WasSigned(ulong orig)
	{
		if ((orig & 1) == 1)
		{
			return -(long)(orig >> 1);
		}

		return (long)orig >> 1;
	}

	private long? WasSignedNullable(ulong orig)
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

	private ulong? WasNullable(ulong orig)
	{
		if (orig == 0)
		{
			return null;
		}

		return (ulong)(orig - 1);
	}

	/// <summary>
	/// Transfer fields from the given reader to the object.
	/// </summary>
	/// <param name="obj"></param>
	/// <param name="reader"></param>
	public void ReadObject(object obj, TransactionReader reader)
	{
		for (var i = 0; i < reader.FieldCount; i++)
		{
			var def = reader.Fields[i].Field;
			var field = GetField(def.Id);

			if (field == null)
			{
				// Old field which was removed but still has values present in the chain
				continue;
			}

			var nv = reader.Fields[i].NumericValue;

			if (field.FieldType == typeof(string))
			{
				field.TargetField.SetValue(obj, reader.Fields[i].GetNativeString());
			}
			else if (field.FieldType == typeof(byte[]))
			{
				field.TargetField.SetValue(obj, reader.Fields[i].GetBytes());
			}
			else if (field.FieldType == typeof(bool))
			{
				if (field.IsNullable)
				{
					if (nv == 0)
					{
						field.TargetField.SetValue(obj, (bool?)null);
					}
					else if (nv == 1)
					{
						field.TargetField.SetValue(obj, (bool?)false);
					}
					else
					{
						field.TargetField.SetValue(obj, (bool?)true);
					}
				}
				else
				{
					if (nv == 0)
					{
						field.TargetField.SetValue(obj, (bool?)false);
					}
					else
					{
						field.TargetField.SetValue(obj, (bool?)true);
					}
				}
			}
			else if (field.FieldType == typeof(sbyte))
			{
				if (field.IsNullable)
				{
					field.TargetField.SetValue(obj, (sbyte?)WasSignedNullable(nv));
				}
				else
				{
					field.TargetField.SetValue(obj, (sbyte)WasSigned(nv));
				}
			}
			else if (field.FieldType == typeof(byte))
			{
				if (field.IsNullable)
				{
					field.TargetField.SetValue(obj, (byte?)WasNullable(nv));
				}
				else
				{
					field.TargetField.SetValue(obj, (byte)nv);
				}
			}
			else if (field.FieldType == typeof(int))
			{
				if (field.IsNullable)
				{
					field.TargetField.SetValue(obj, (int?)WasSignedNullable(nv));
				}
				else
				{
					field.TargetField.SetValue(obj, (int)WasSigned(nv));
				}
			}
			else if (field.FieldType == typeof(uint))
			{
				if (field.IsNullable)
				{
					field.TargetField.SetValue(obj, (uint?)WasNullable(nv));
				}
				else
				{
					field.TargetField.SetValue(obj, (uint)nv);
				}
			}
			else if (field.FieldType == typeof(short))
			{
				if (field.IsNullable)
				{
					field.TargetField.SetValue(obj, (short?)WasSignedNullable(nv));
				}
				else
				{
					field.TargetField.SetValue(obj, (short)WasSigned(nv));
				}
			}
			else if (field.FieldType == typeof(ushort))
			{
				if (field.IsNullable)
				{
					field.TargetField.SetValue(obj, (ushort?)WasNullable(nv));
				}
				else
				{
					field.TargetField.SetValue(obj, (ushort)nv);
				}
			}
			else if (field.FieldType == typeof(long))
			{
				if (field.IsNullable)
				{
					field.TargetField.SetValue(obj, WasSignedNullable(nv));
				}
				else
				{
					field.TargetField.SetValue(obj, WasSigned(nv));
				}
			}
			else if (field.FieldType == typeof(ulong))
			{
				if (field.IsNullable)
				{
					field.TargetField.SetValue(obj, WasNullable(nv));
				}
				else
				{
					field.TargetField.SetValue(obj, nv);
				}
			}
			else if (field.FieldType == typeof(float))
			{
				if (field.IsNullable)
				{
					var v = WasNullable(nv);

					if (v.HasValue)
					{
						field.TargetField.SetValue(obj, (float?)new FloatBits((uint)v.Value).Float);
					}
					else
					{
						field.TargetField.SetValue(obj, (float?)null);
					}
				}
				else
				{
					var value = new FloatBits((uint)nv).Float;
					field.TargetField.SetValue(obj, value);
				}
			}
			else if (field.FieldType == typeof(DateTime))
			{
				if (field.IsNullable)
				{
					var v = WasSignedNullable(nv);

					if (v.HasValue)
					{
						field.TargetField.SetValue(obj, (DateTime?)new DateTime(v.Value));
					}
					else
					{
						field.TargetField.SetValue(obj, (DateTime?)null);
					}
				}
				else
				{
					var v = WasSigned(nv);
					field.TargetField.SetValue(obj, new DateTime(v));
				}
			}
			else if (field.FieldType == typeof(double))
			{
				if (field.IsNullable)
				{
					var v = WasNullable(nv);

					if (v.HasValue)
					{
						field.TargetField.SetValue(obj, (double?)new DoubleBits((uint)v.Value).Double);
					}
					else
					{
						field.TargetField.SetValue(obj, (double?)null);
					}
				}
				else
				{
					var value = new DoubleBits((uint)nv).Double;
					field.TargetField.SetValue(obj, value);
				}
			}
		}
	}

	public void WriteObject(object obj, Writer writer)
	{
		#warning todo - create an emitted writer

		for (var i = 0; i < Fields.Count; i++)
		{
			var field = Fields[i];
			var def = field.Definition;

			writer.WriteInvertibleCompressed(def.Id);

			var fieldValue = field.TargetField.GetValue(obj);

			if (field.FieldType == typeof(string))
			{
				writer.WriteInvertibleUTF8((string)fieldValue);
			}
			else if (field.FieldType == typeof(byte[]))
			{
				writer.WriteInvertible((byte[])fieldValue);
			}
			else if (field.FieldType == typeof(bool))
			{
				if (field.IsNullable)
				{
					var v = (bool?)fieldValue;
					writer.WriteInvertibleCompressed(v.HasValue ? (v.Value ? (ulong)2 : (ulong)1) : 0);
				}
				else
				{
					var v = (bool)fieldValue;
					writer.WriteInvertibleCompressed(v ? (ulong)1 : (ulong)0);
				}
			}
			else if (field.FieldType == typeof(sbyte))
			{
				if (field.IsNullable)
				{
					var v = (sbyte?)fieldValue;
					writer.WriteInvertibleCompressedSigned((long?)v);
				}
				else
				{
					var v = (sbyte)fieldValue;
					writer.WriteInvertibleCompressedSigned(v);
				}
			}
			else if (field.FieldType == typeof(byte))
			{
				if (field.IsNullable)
				{
					var v = (byte?)fieldValue;
					writer.WriteInvertibleCompressed((ulong?)v);
				}
				else
				{
					var v = (byte)fieldValue;
					writer.WriteInvertibleCompressed(v);
				}
			}
			else if (field.FieldType == typeof(int))
			{
				if (field.IsNullable)
				{
					var v = (int?)fieldValue;
					writer.WriteInvertibleCompressedSigned((long?)v);
				}
				else
				{
					var v = (int)fieldValue;
					writer.WriteInvertibleCompressedSigned(v);
				}
			}
			else if (field.FieldType == typeof(uint))
			{
				if (field.IsNullable)
				{
					var v = (uint?)fieldValue;
					writer.WriteInvertibleCompressed((ulong?)v);
				}
				else
				{
					var v = (uint)fieldValue;
					writer.WriteInvertibleCompressed(v);
				}
			}
			else if (field.FieldType == typeof(short))
			{
				if (field.IsNullable)
				{
					var v = (short?)fieldValue;
					writer.WriteInvertibleCompressedSigned((long?)v);
				}
				else
				{
					var v = (short)fieldValue;
					writer.WriteInvertibleCompressedSigned(v);
				}
			}
			else if (field.FieldType == typeof(ushort))
			{
				if (field.IsNullable)
				{
					var v = (ushort?)fieldValue;
					writer.WriteInvertibleCompressed((ulong?)v);
				}
				else
				{
					var v = (ushort)fieldValue;
					writer.WriteInvertibleCompressed(v);
				}
			}
			else if (field.FieldType == typeof(long))
			{
				if (field.IsNullable)
				{
					var v = (long?)fieldValue;
					writer.WriteInvertibleCompressedSigned(v);
				}
				else
				{
					var v = (long)fieldValue;
					writer.WriteInvertibleCompressedSigned(v);
				}
			}
			else if (field.FieldType == typeof(ulong))
			{
				if (field.IsNullable)
				{
					var v = (ulong?)fieldValue;
					writer.WriteInvertibleCompressed(v);
				}
				else
				{
					var v = (ulong)fieldValue;
					writer.WriteInvertibleCompressed(v);
				}
			}
			else if (field.FieldType == typeof(float))
			{
				if (field.IsNullable)
				{
					var v = (float?)fieldValue;

					if (v.HasValue)
					{
						ulong? value = new FloatBits(v.Value).Int;
						writer.WriteInvertibleCompressed(value);
					}
					else
					{
						writer.WriteInvertibleCompressed(0);
					}
				}
				else
				{
					var v = (float)fieldValue;
					ulong value = new FloatBits(v).Int;
					writer.WriteInvertibleCompressed(value);
				}
			}
			else if (field.FieldType == typeof(DateTime))
			{
				if (field.IsNullable)
				{
					var v = (DateTime?)fieldValue;

					if (v.HasValue)
					{
						writer.WriteInvertibleCompressedSigned((long?)v.Value.Ticks);
					}
					else
					{
						writer.WriteInvertibleCompressedSigned(0);
					}
				}
				else
				{
					var v = (DateTime)fieldValue;
					writer.WriteInvertibleCompressedSigned(v.Ticks);
				}
			}
			else if (field.FieldType == typeof(double))
			{
				if (field.IsNullable)
				{
					var v = (double?)fieldValue;

					if (v.HasValue)
					{
						ulong? value = new DoubleBits(v.Value).Int;
						writer.WriteInvertibleCompressed(value);
					}
					else
					{
						writer.WriteInvertibleCompressed(0);
					}
				}
				else
				{
					var v = (double)fieldValue;
					ulong value = new DoubleBits(v).Int;
					writer.WriteInvertibleCompressed(value);
				}
			}

			writer.WriteInvertibleCompressed(def.Id);
		}
	}

	public void AddField(BlockDatabaseColumnDefinition col, FieldDefinition field) {
		var btf = new BlockTableField() {
			Id = field.Id,
			Definition = field,
			FieldType = col.FieldType,
			IsNullable = col.IsNullable,
			TargetField = col.Field.TargetField
		};

		Fields.Add(btf);
		FieldLookup[field.Id] = btf;
	}
	
	public BlockTableField GetField(ulong id){
		FieldLookup.TryGetValue(id, out BlockTableField result);
		return result;
	}
}

/// <summary>
/// A field in the blockchain with meta for mapping to a C# field
/// </summary>
public class BlockTableField
{
	/// <summary>
	/// The ID in the blockchain
	/// </summary>
	public ulong Id;
	/// <summary>
	/// The type of the fields value on the C# object
	/// </summary>
	public Type FieldType;
	/// <summary>
	/// True if this field is nullable.
	/// </summary>
	public bool IsNullable;
	/// <summary>
	/// The blockchain def
	/// </summary>
	public FieldDefinition Definition;
	/// <summary>
	/// Field to write/ read
	/// </summary>
	public FieldInfo TargetField;
}