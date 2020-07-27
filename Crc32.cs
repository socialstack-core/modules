using System;

namespace Api.SocketServerLibrary
{
	/// <summary>
	/// Computes the CRC32 of blocks of bytes.
	/// </summary>
	public static class Crc32{
		
		private static uint[] Table;
		/// <summary>Seed</summary>
		public const uint DefaultSeed=0xffffffffu;
		/// <summary>Polynomial</summary>
		public const uint DefaultPolynomial=0xedb88320u;
		
		/// <summary>Gets the raw table</summary>
		public static uint[] GetTable(){
			
			if(Table == null){
				Table = InitializeTable();
			}
			
			return Table;
		}
		
		private static uint[] InitializeTable(){
			
			uint[] table = new uint[256];
			
			for(int i = 0; i < 256; i++){
				
				uint entry = (uint)i;
				
				for(int j = 0; j < 8; j++){
					if ((entry & 1) == 1){
						entry = (entry >> 1) ^ DefaultPolynomial;
					}else{
						entry = entry >> 1;
					}
				}
				
				table[i] = entry;
				
			}
			
			return table;
		}
		
		/// <summary>Computes the default CRC32 for the given input data and 
		/// writes it into the given block of bytes (little endian).</summary>
		public static void Compute(byte[] input,byte[] into,int offset){
			
			// Compute it:
			uint crc=Compute(input);
			
			// Copy into result:
			GetBytes(crc,into,offset);
		}

		/// <summary>
		/// Gets the bytes of the given uint.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="result"></param>
		/// <param name="offset"></param>
		public static void GetBytes(uint value, byte[] result, int offset)
		{
			result[offset] = (byte)value;
			result[offset + 1] = (byte)(value >> 8);
			result[offset + 2] = (byte)(value >> 16);
			result[offset + 3] = (byte)(value >> 24);
		}

		/// <summary>Computes the default CRC32 for the given input data.</summary>
		public static uint Compute(byte[] input){
			
			return Compute(input,0,input.Length);
			
		}
		
		/// <summary>Computes the default CRC32 for the given input data.</summary>
		public static uint Compute(byte[] input,int start,int end){
			
			if(Table==null){
				Table = InitializeTable();
			}
			
			uint crc = DefaultSeed;
			
			for(int i=start; i < end; i++){
				
				crc = (crc >> 8) ^ Table[ (crc ^ input[i]) & 0xFF ];
				
			}
			
			return crc;
			
		}
		
		/// <summary>Computes the CRC32, applying it to the given one, for the given input data.</summary>
		public static uint Compute(byte[] input,int start,int end,uint crc){
			
			if(Table==null){
				Table = InitializeTable();
			}
			
			for(int i=start; i < end; i++){
				
				crc = (crc >> 8) ^ Table[ (crc ^ input[i]) & 0xFF ];
				
			}
			
			return crc;
			
		}
		
	}
}