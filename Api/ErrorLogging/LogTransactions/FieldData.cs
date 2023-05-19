using Api.SocketServerLibrary;

namespace Api.ErrorLogging;

/// <summary>
/// A particular field.
/// </summary>
public struct FieldData
{
	/// <summary>
	/// The field definition
	/// </summary>
	public FieldDefinition Field;

	/// <summary>
	/// True if the value is null.
	/// </summary>
	public bool IsNull;

	/// <summary>
	/// Offset to where this fields data starts
	/// </summary>
	public int DataStart;

	/// <summary>
	/// The length of the data
	/// </summary>
	public int DataLength;

	/// <summary>
	/// This is set if the value is just a compressed number (Field.SizeIsValue is true)
	/// </summary>
	public ulong NumericValue;

	/// <summary>
	/// First buffer that variable data is in. It's a linked list.
	/// </summary>
	public BufferedBytes FirstBuffer;

	/// <summary>
	/// For "uint" dataType fields.
	/// </summary>
	/// <returns></returns>
	public ulong GetUInt()
	{
		return NumericValue;
	}

	/// <summary>
	/// For "int" dataType fields.
	/// </summary>
	/// <returns></returns>
	public long GetInt()
	{
		var sign = NumericValue & 1;
		var v = (long)(NumericValue >> 1);
		return sign == 0 ? v : -v;
	}

	/// <summary>
	/// For "bytes" dataType fields. Writes the data into the given writer. No-op if the data is null.
	/// </summary>
	/// <param name="writer"></param>
	public void GetToWriter(Writer writer)
	{
		if (IsNull || DataLength == 0)
		{
			return;
		}

		var dataBuffer = FirstBuffer;
		var offset = dataBuffer.Offset + DataStart;

		var bytesToRead = DataLength;

		while (true)
		{
			int bytesFromThisBuffer = bytesToRead;
			var bufferFill = dataBuffer.Length - offset;

			if (bytesFromThisBuffer >= bufferFill)
			{
				bytesFromThisBuffer = bufferFill;
			}

			writer.Write(dataBuffer.Bytes, offset, bytesFromThisBuffer);
			bytesToRead -= bytesFromThisBuffer;

			if (bytesToRead == 0)
			{
				break;
			}

			dataBuffer = dataBuffer.After;
			offset = 0;
		}
	}

	/// <summary>
	/// For "bytes" dataType fields. Allocates a byte array. Avoid unless necessary.
	/// </summary>
	/// <returns></returns>
	public byte[] GetBytes()
	{
		if (IsNull)
		{
			return null;
		}

		var buffer = new byte[DataLength];

		if (DataLength == 0)
		{
			return buffer;
		}

		var dataBuffer = FirstBuffer;
		var offset = dataBuffer.Offset + DataStart;

		for (var i = 0; i < DataLength; i++)
		{
			buffer[i] = dataBuffer.Bytes[offset];
			offset++;
			if (offset == dataBuffer.Length)
			{
				offset = 0;
				dataBuffer = dataBuffer.After;
			}
		}

		return buffer;
	}

	/// <summary>
	/// For "string" dataType fields. Allocates a byte array and a ustring object. Avoid unless necessary.
	/// </summary>
	/// <returns></returns>
	public ustring GetString()
	{
		var b = GetBytes();
		if (b == null)
		{
			return null;
		}
		return ustring.Make(b);
	}

	/// <summary>
	/// For "string" dataType fields. Gets a C# string instead of a ustring. Slower but more generally useful.
	/// </summary>
	/// <returns></returns>
	public string GetNativeString()
	{
		var b = GetBytes();
		if (b == null)
		{
			return null;
		}
		return System.Text.Encoding.UTF8.GetString(b);
	}

}