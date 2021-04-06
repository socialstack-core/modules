using System;
using System.Collections;
using System.Collections.Generic;


namespace Api.SocketServerLibrary
{
	/// <summary>
	/// Field type info for automatically generated field maps.
	/// </summary>
	public static class OpCodeFieldTypes
	{
		
		private static Dictionary<Type, FieldTypeInfo> Handlers;
		private static Dictionary<int, FieldTypeInfo> HandlersById;
		
		/// <summary>
		/// Loads the field type info.
		/// </summary>
		private static void Load()
		{
			Handlers = new Dictionary<Type, FieldTypeInfo>();
			HandlersById = new Dictionary<int, FieldTypeInfo>();

			Add(
				1,
				(ClientReader reader) => reader.Read(reader.ThenSetField_Bool_D),
				(Writer writer, bool value) => writer.Write((byte)(value ? 1 : 0))
			);

			Add(
				2,
				(ClientReader reader) => reader.ReadBytes(reader.ThenSetField_ByteArray_D),
				(Writer writer, byte[] value) => writer.WriteBuffer(value)
			);

			Add(
				3,
				(ClientReader reader) => reader.ReadStringList(reader.ThenSetField_ListString_D),
				(Writer writer, List<string> value) => writer.Write(value)
			);

			Add(
				4,
				(ClientReader reader) => reader.ReadBytes(reader.ThenSetField_String_D),
				(Writer writer, string value) => writer.Write(value)
			);

			Add(
				5,
				(ClientReader reader) => reader.ReadDateTime(reader.ThenSetField_DateTime_D),
				(Writer writer, DateTime value) => writer.Write(value)
			);

			Add(
				6,
				(ClientReader reader) => reader.ReadFloat(reader.ThenSetField_Float_D),
				(Writer writer, float value) => writer.Write(value)
			);

			Add(
				7,
				(ClientReader reader) => reader.ReadDouble(reader.ThenSetField_Double_D),
				(Writer writer, double value) => writer.Write(value)
			);

			/*
			Add(
				8,
				(ClientReader reader) => reader.Read(reader.ThenSetField_SByte_D),
				(Writer writer, sbyte value) => writer.Write(value)
			);
			*/

			Add(
				9,
				(ClientReader reader) => reader.ReadInt16(reader.ThenSetField_Short_D),
				(Writer writer, short value) => writer.Write(value)
			);

			Add(
				10,
				(ClientReader reader) => reader.ReadInt32(reader.ThenSetField_Int32_D),
				(Writer writer, int value) => writer.Write(value)
			);

			Add(
				11,
				(ClientReader reader) => reader.ReadInt64(reader.ThenSetField_Int64_D),
				(Writer writer, long value) => writer.Write(value)
			);
			
			Add(
				12,
				(ClientReader reader) => reader.Read(reader.ThenSetField_Byte_D),
				(Writer writer, byte value) => writer.Write(value)
			);

			Add(
				13,
				(ClientReader reader) => reader.ReadUInt16(reader.ThenSetField_UShort_D),
				(Writer writer, ushort value) => writer.Write(value)
			);

			Add(
				14,
				(ClientReader reader) => reader.ReadUInt32(reader.ThenSetField_UInt32_D),
				(Writer writer, uint value) => writer.Write(value)
			);

			Add(
				15,
				(ClientReader reader) => reader.ReadUInt64(reader.ThenSetField_UInt64_D),
				(Writer writer, ulong value) => writer.Write(value)
			);
			
			Add(
				110,
				(ClientReader reader) => reader.ReadInt32(reader.ThenSetField_NInt32_D),
				(Writer writer, int? value) => writer.Write(value)
			);

			Add(
				114,
				(ClientReader reader) => reader.ReadUInt32(reader.ThenSetField_NUInt32_D),
				(Writer writer, uint? value) => writer.Write(value)
			);

			Add(
				105,
				(ClientReader reader) => reader.ReadDateTime(reader.ThenSetField_NDateTime_D),
				(Writer writer, DateTime? value) => writer.Write(value)
			);

		}

		private static FieldTypeInfo Add<T>(int id, Action<ClientReader> reader, Action<Writer, T> writer)
		{
			var systemType = typeof(T);

			var typeInfo = new FieldTypeInfo(){
				Id = id,
				Type = systemType,
				Reader = reader,
				Writer = writer
			};
			
			Handlers[systemType] = typeInfo;
			HandlersById[id] = typeInfo;
			
			return typeInfo;
		}
		
		/// <summary>
		/// Gets the field type info for the given type ID.
		/// </summary>
		public static FieldTypeInfo Get(int id)
		{
			if (Handlers == null)
			{
				Load();
			}
			HandlersById.TryGetValue(id, out FieldTypeInfo res);
			return res;
		}
		
		/// <summary>
		/// Gets the field type info for the given type.
		/// </summary>
		public static FieldTypeInfo Get(Type type)
		{
			if (Handlers == null)
			{
				Load();
			}
			Handlers.TryGetValue(type, out FieldTypeInfo res);
			return res;
		}
		
	}
	
	/// <summary>
	/// Helps figure out which reader/ writer to use for particular system types.
	/// </summary>
	public class FieldTypeInfo
	{
		/// <summary>
		/// Unique ID of this field type info.
		/// </summary>
		public int Id;
		
		/// <summary>
		/// Field type
		/// </summary>
		public Type Type;
		
		/// <summary>
		/// Reader action
		/// </summary>
		public Action<ClientReader> Reader;
		
		/// <summary>
		/// Writer action.
		/// </summary>
		public object Writer;
	}
	
}