namespace Api.SocketServerLibrary {

	/// <summary>
	/// Used by the probe when defining what raw bytes on the line mean.
	/// </summary>
	public enum MetaFieldType : int{
		/// <summary>
		/// No type.
		/// </summary>
		Nothing = 0,
		/// <summary>
		/// Indicates the end of a message.
		/// </summary>
		Done = 1,
		/// <summary>
		/// Indicates a signed number.
		/// </summary>
		Signed = 2,
		/// <summary>
		/// Indicates a raw buffer, but also strings.
		/// </summary>
		Buffer = 3,
		/// <summary>
		/// Indicates nul strings.
		/// </summary>
		NulString = 4,
		/// <summary>
		/// Indicates a compressed number.
		/// </summary>
		Compressed = 5,
		/// <summary>
		/// Indicates a packed number.
		/// </summary>
		Packed = 6,
		/// <summary>
		/// Indicates a float.
		/// </summary>
		Float = 7,
		/// <summary>
		/// Indicates a block of bytes.
		/// </summary>
		Bytes = 8,
		/// <summary>
		/// Indicates an unsigned number.
		/// </summary>
		Unsigned = 9,
		/// <summary>
		/// Indicates an opcode.
		/// </summary>
		Opcode = 10,
		/// <summary>
		/// Indicates a datetime.
		/// </summary>
		Date = 11
	}
	
}