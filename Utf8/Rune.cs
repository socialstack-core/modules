// From https://github.com/migueldeicaza/NStack
// C# ification by Miguel de Icaza

using System;
using System.Runtime.InteropServices;

namespace Api.Startup.Utf8Helpers {
	/// <summary>
	/// A Rune represents a Unicode CodePoint storing the contents in a 32-bit value
	/// </summary>
	/// <remarks>
	/// 
	/// </remarks>
	[StructLayout(LayoutKind.Sequential)]
	public partial struct Rune {
		// Stores the rune
		uint value;

		/// <summary>
		/// The "error" Rune or "Unicode replacement character"
		/// </summary>
		public static Rune Error = new Rune (0xfffd);

		/// <summary>
		/// Maximum valid Unicode code point.
		/// </summary>
		public static Rune MaxRune = new Rune (0x10ffff);

		/// <summary>
		/// Characters below RuneSelf are represented as themselves in a single byte
		/// </summary>
		public const byte RuneSelf = 0x80;

		/// <summary>
		/// Represents invalid code points.
		/// </summary>
		public static Rune ReplacementChar = new Rune (0xfffd);

		/// <summary>
		/// Maximum number of bytes required to encode every unicode code point.
		/// </summary>
		public const int Utf8Max = 4;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:System.Rune"/> from a unsigned integer.
		/// </summary>
		/// <param name="rune">Unsigned integer.</param>
		/// <remarks>
		/// The value does not have to be a valid Unicode code point, this API
		/// will create an instance of Rune regardless of the whether it is in 
		/// range or not.
		/// </remarks>
		public Rune (uint rune)
		{
			if (rune > maxRune)
			{
				throw new ArgumentOutOfRangeException("Value is beyond the supplementary range!");
			}
			this.value = rune;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:System.Rune"/> from a character value.
		/// </summary>
		/// <param name="ch">C# characters.</param>
		public Rune (char ch)
		{
			if (ch >= surrogateMin && ch <= surrogateMax)
			{
				throw new ArgumentException("Value in the surrogate range and isn't part of a surrogate pair!");
			}
			this.value = (uint)ch;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:System.Rune"/> from a surrogate pair value.
		/// </summary>
		/// <param name="sgateMin">The high surrogate code points minimum value.</param>
		/// <param name="sgateMax">The low surrogate code points maximum value.</param>
		public Rune (uint sgateMin, uint sgateMax)
		{
			var rune = DecodeSurrogatePair (sgateMin, sgateMax);
			if (rune > 0)
			{
				this.value = rune;
			}
			else
			{
				throw new ArgumentOutOfRangeException($"Must be between {surrogateMin:x} and {surrogateMax:x} inclusive!");
			}
		}

		/// <summary>
		/// Gets a value indicating whether this <see cref="T:System.Rune"/> can be encoded as UTF-8 from a surrogate pair or zero otherwise.
		/// </summary>
		/// <param name="sgateMin">The high surrogate code points minimum value.</param>
		/// <param name="sgateMax">The low surrogate code points maximum value.</param>
		public static uint DecodeSurrogatePair (uint sgateMin, uint sgateMax)
		{
			if (sgateMin < surrogateMin || sgateMax > surrogateMax)
			{
				return 0;
			}
			else
			{
				return 0x10000 + ((sgateMin - surrogateMin) * 0x0400) + (sgateMax - lowSurrogateMin);
			}
		}

		/// <summary>
		/// Gets a value indicating whether this <see cref="T:System.Rune"/> can be encoded as UTF-8
		/// </summary>
		/// <value><c>true</c> if is valid; otherwise, <c>false</c>.</value>
		public bool IsValid {
			get {
				if (0 <= value && value <= surrogateMin)
					return true;
				if (surrogateMax <= value && value <= MaxRune)
					return true;
				return false;
			}
		}

		// Code points in the surrogate range are not valid for UTF-8.
		const uint surrogateMin = 0xd800;
		const uint surrogateMax = 0xdfff;

		const uint highSurrogateMax = 0xdbff;
		const uint lowSurrogateMin = 0xdc00;
		const uint maxRune = 0x10ffff;

		const byte t1 = 0x00; // 0000 0000
		const byte tx = 0x80; // 1000 0000
		const byte t2 = 0xC0; // 1100 0000
		const byte t3 = 0xE0; // 1110 0000
		const byte t4 = 0xF0; // 1111 0000
		const byte t5 = 0xF8; // 1111 1000

		const byte maskx = 0x3F; // 0011 1111
		const byte mask2 = 0x1F; // 0001 1111
		const byte mask3 = 0x0F; // 0000 1111
		const byte mask4 = 0x07; // 0000 0111

		const uint rune1Max = (1 << 7) - 1;
		const uint rune2Max = (1 << 11) - 1;
		const uint rune3Max = (1 << 16) - 1;

		// The default lowest and highest continuation byte.
		const byte locb = 0x80; // 1000 0000
		const byte hicb = 0xBF; // 1011 1111

		// These names of these constants are chosen to give nice alignment in the
		// table below. The first nibble is an index into acceptRanges or F for
		// special one-byte ca1es. The second nibble is the Rune length or the
		// Status for the special one-byte ca1e.
		const byte xx = 0xF1; // invalid: size 1
		const byte a1 = 0xF0; // a1CII: size 1
		const byte s1 = 0x02; // accept 0, size 2
		const byte s2 = 0x13; // accept 1, size 3
		const byte s3 = 0x03; // accept 0, size 3
		const byte s4 = 0x23; // accept 2, size 3
		const byte s5 = 0x34; // accept 3, size 4
		const byte s6 = 0x04; // accept 0, size 4
		const byte s7 = 0x44; // accept 4, size 4

		static byte [] first = new byte [256]{
			//   1   2   3   4   5   6   7   8   9   A   B   C   D   E   F
			a1, a1, a1, a1, a1, a1, a1, a1, a1, a1, a1, a1, a1, a1, a1, a1, // 0x00-0x0F
			a1, a1, a1, a1, a1, a1, a1, a1, a1, a1, a1,a1, a1, a1, a1, a1, // 0x10-0x1F
			a1, a1, a1, a1, a1, a1, a1, a1, a1, a1, a1, a1, a1, a1, a1, a1, // 0x20-0x2F
			a1, a1, a1, a1, a1, a1, a1, a1, a1, a1, a1, a1, a1, a1, a1, a1, // 0x30-0x3F
			a1, a1, a1, a1, a1, a1, a1, a1, a1, a1, a1, a1, a1, a1, a1, a1, // 0x40-0x4F
			a1, a1, a1, a1, a1, a1, a1, a1, a1, a1, a1, a1, a1, a1, a1, a1, // 0x50-0x5F
			a1, a1, a1, a1, a1, a1, a1, a1, a1, a1, a1, a1, a1, a1, a1, a1, // 0x60-0x6F
			a1, a1, a1, a1, a1, a1, a1, a1, a1, a1, a1, a1, a1, a1, a1, a1, // 0x70-0x7F

			//   1   2   3   4   5   6   7   8   9   A   B   C   D   E   F
			xx, xx, xx, xx, xx, xx, xx, xx, xx, xx, xx, xx, xx, xx, xx, xx, // 0x80-0x8F
			xx, xx, xx, xx, xx, xx, xx, xx, xx, xx, xx, xx, xx, xx, xx, xx, // 0x90-0x9F
			xx, xx, xx, xx, xx, xx, xx, xx, xx, xx, xx, xx, xx, xx, xx, xx, // 0xA0-0xAF
			xx, xx, xx, xx, xx, xx, xx, xx, xx, xx, xx, xx, xx, xx, xx, xx, // 0xB0-0xBF
			xx, xx, s1, s1, s1, s1, s1, s1, s1, s1, s1, s1, s1, s1, s1, s1, // 0xC0-0xCF
			s1, s1, s1, s1, s1, s1, s1, s1, s1, s1, s1, s1, s1, s1, s1, s1, // 0xD0-0xDF
			s2, s3, s3, s3, s3, s3, s3, s3, s3, s3, s3, s3, s3, s4, s3, s3, // 0xE0-0xEF
			s5, s6, s6, s6, s7, xx, xx, xx, xx, xx, xx, xx, xx, xx, xx, xx, // 0xF0-0xFF
		};

		struct AcceptRange {
			public byte Lo, Hi;
			public AcceptRange (byte lo, byte hi)
			{
				Lo = lo;
				Hi = hi;
			}
		}

		static AcceptRange [] AcceptRanges = new AcceptRange [] {
			new AcceptRange (locb, hicb),
			new AcceptRange (0xa0, hicb),
			new AcceptRange (locb, 0x9f),
			new AcceptRange (0x90, hicb),
			new AcceptRange (locb, 0x8f),
		};

		/// <summary>
		/// FullRune reports whether the bytes in p begin with a full UTF-8 encoding of a rune.
		/// An invalid encoding is considered a full Rune since it will convert as a width-1 error rune.
		/// </summary>
		/// <returns><c>true</c>, if the bytes in p begin with a full UTF-8 encoding of a rune, <c>false</c> otherwise.</returns>
		/// <param name="p">byte array.</param>
		public static bool FullRune (byte [] p)
		{
			if (p == null)
				throw new ArgumentNullException (nameof (p));
			var n = p.Length;

			if (n == 0)
				return false;
			var x = first [p [0]];
			if (n >= (x & 7)) {
				// ascii, invalid or valid
				return true;
			}
			// must be short or invalid
			if (n > 1) {
				var accept = AcceptRanges [x >> 4];
				var c = p [1];
				if (c < accept.Lo || accept.Hi < c)
					return true;
				else if (n > 2 && (p [2] < locb || hicb < p [2]))
					return true;
			}
			return false;
		}

		/// <summary>
		/// DecodeRune unpacks the first UTF-8 encoding in p and returns the rune and
		/// its width in bytes. 
		/// </summary>
		/// <returns>If p is empty it returns (RuneError, 0). Otherwise, if
		/// the encoding is invalid, it returns (RuneError, 1). Both are impossible
		/// results for correct, non-empty UTF-8.
		/// </returns>
		/// <param name="buffer">Byte buffer containing the utf8 string.</param>
		/// <param name="start">Starting offset to look into..</param>
		/// <param name="n">Number of bytes valid in the buffer, or -1 to make it the length of the buffer.</param>
		public static (Rune rune, int Size) DecodeRune (byte [] buffer, int start = 0, int n = -1)
		{
			if (buffer == null)
				throw new ArgumentNullException (nameof (buffer));
			if (start < 0)
				throw new ArgumentException ("invalid offset", nameof (start));
			if (n < 0)
				n = buffer.Length - start;
			if (start > buffer.Length - n)
				throw new ArgumentException ("Out of bounds");

			if (n < 1)
				return (Error, 0);

			var p0 = buffer [start];
			var x = first [p0];
			if (x >= a1) {
				// The following code simulates an additional check for x == xx and
				// handling the ASCII and invalid cases accordingly. This mask-and-or
				// approach prevents an additional branch.
				uint mask = (uint)((((byte)x) << 31) >> 31); // Create 0x0000 or 0xFFFF.
				return (new Rune ((buffer [start]) & ~mask | Error.value & mask), 1);
			}

			var sz = x & 7;
			var accept = AcceptRanges [x >> 4];
			if (n < (int)sz)
				return (Error, 1);

			var b1 = buffer [start + 1];
			if (b1 < accept.Lo || accept.Hi < b1)
				return (Error, 1);

			if (sz == 2)
				return (new Rune ((uint)((p0 & mask2)) << 6 | (uint)((b1 & maskx))), 2);

			var b2 = buffer [start + 2];
			if (b2 < locb || hicb < b2)
				return (Error, 1);

			if (sz == 3)
				return (new Rune ((uint)((p0 & mask3)) << 12 | (uint)((b1 & maskx)) << 6 | (uint)((b2 & maskx))), 3);

			var b3 = buffer [start + 3];
			if (b3 < locb || hicb < b3) {
				return (Error, 1);
			}
			return (new Rune ((uint)(p0 & mask4) << 18 | (uint)(b1 & maskx) << 12 | (uint)(b2 & maskx) << 6 | (uint)(b3 & maskx)), 4);
		}


		// RuneStart reports whether the byte could be the first byte of an encoded,
		// possibly invalid rune. Second and subsequent bytes always have the top two
		// bits set to 10.
		static bool RuneStart (byte b) => (b & 0xc0) != 0x80;

		/// <summary>
		/// DecodeLastRune unpacks the last UTF-8 encoding in buffer
		/// </summary>
		/// <returns>The last rune and its width in bytes.</returns>
		/// <param name="buffer">Buffer to decode rune from;   if it is empty,
		/// it returns (RuneError, 0). Otherwise, if
		/// the encoding is invalid, it returns (RuneError, 1). Both are impossible
		/// results for correct, non-empty UTF-8.</param>
		/// <param name="end">Scan up to that point, if the value is -1, it sets the value to the length of the buffer.</param>
		/// <remarks>
		/// An encoding is invalid if it is incorrect UTF-8, encodes a rune that is
		/// out of range, or is not the shortest possible UTF-8 encoding for the
		/// value. No other validation is performed.</remarks> 
		public static (Rune rune, int size) DecodeLastRune (byte [] buffer, int end = -1)
		{
			if (buffer == null)
				throw new ArgumentNullException (nameof (buffer));
			if (buffer.Length == 0)
				return (Error, 0);
			if (end == -1)
				end = buffer.Length;
			else if (end > buffer.Length)
				throw new ArgumentException ("The end goes beyond the size of the buffer");

			var start = end - 1;
			uint r = buffer [start];
			if (r < RuneSelf)
				return (new Rune (r), 1);
			// guard against O(n^2) behavior when traversing
			// backwards through strings with long sequences of
			// invalid UTF-8.
			var lim = end - Utf8Max;

			if (lim < 0)
				lim = 0;

			for (start--; start >= lim; start--) {
				if (RuneStart (buffer [start])) {
					break;
				}
			}
			if (start < 0)
				start = 0;
			int size;
			Rune r2;
			(r2, size) = DecodeRune (buffer, start, end - start);
			if (start + size != end)
				return (Error, 1);
			return (r2, size);
		}

		/// <summary>
		/// number of bytes required to encode the rune.
		/// </summary>
		/// <returns>The length, or -1 if the rune is not a valid value to encode in UTF-8.</returns>
		/// <param name="rune">Rune to probe.</param>
		public static int RuneLen (Rune rune)
		{
			var rvalue = rune.value;
			if (rvalue <= rune1Max)
				return 1;
			if (rvalue <= rune2Max)
				return 2;
			if (surrogateMin <= rvalue && rvalue <= surrogateMax)
				return -1;
			if (rvalue <= rune3Max)
				return 3;
			if (rvalue <= MaxRune.value)
				return 4;
			return -1;
		}

		/// <summary>
		/// Writes into the destination buffer starting at offset the UTF8 encoded version of the rune
		/// </summary>
		/// <returns>The number of bytes written into the destination buffer.</returns>
		/// <param name="rune">Rune to encode.</param>
		/// <param name="dest">Destination buffer.</param>
		/// <param name="offset">Offset into the destination buffer.</param>
		public static int EncodeRune (Rune rune, byte [] dest, int offset = 0)
		{
			if (dest == null)
				throw new ArgumentNullException (nameof (dest));
			var runeValue = rune.value;
			if (runeValue <= rune1Max) {
				dest [offset] = (byte)runeValue;
				return 1;
			}
			if (runeValue <= rune2Max) {
				dest [offset++] = (byte)(t2 | (byte)(runeValue >> 6));
				dest [offset] = (byte)(tx | (byte)(runeValue & maskx));
				return 2;
			}
			if ((runeValue > MaxRune.value) || (surrogateMin <= runeValue && runeValue <= surrogateMax)) {
				// error
				dest [offset++] = 0xef;
				dest [offset++] = 0x3f;
				dest [offset] = 0x3d;
				return 3;
			}
			if (runeValue <= rune3Max) {
				dest [offset++] = (byte)(t3 | (byte)(runeValue >> 12));
				dest [offset++] = (byte)(tx | (byte)(runeValue >> 6) & maskx);
				dest [offset] = (byte)(tx | (byte)(runeValue) & maskx);
				return 3;
			}
			dest [offset++] = (byte)(t4 | (byte)(runeValue >> 18));
			dest [offset++] = (byte)(tx | (byte)(runeValue >> 12) & maskx);
			dest [offset++] = (byte)(tx | (byte)(runeValue >> 6) & maskx);
			dest [offset++] = (byte)(tx | (byte)(runeValue) & maskx);
			return 4;
		}

		/// <summary>
		/// Returns the number of runes in a utf8 encoded buffer
		/// </summary>
		/// <returns>Number of runes.</returns>
		/// <param name="buffer">Byte buffer containing a utf8 string.</param>
		/// <param name="offset">Starting offset in the buffer.</param>
		/// <param name="count">Number of bytes to process in buffer, or -1 to process until the end of the buffer.</param>
		public static int RuneCount (byte [] buffer, int offset = 0, int count = -1)
		{
			if (buffer == null)
				throw new ArgumentNullException (nameof (buffer));
			if (count == -1)
				count = buffer.Length;
			int n = 0;
			for (int i = offset; i < count;) {
				n++;
				var c = buffer [i];

				if (c < RuneSelf) {
					// ASCII fast path
					i++;
					continue;
				}
				var x = first [c];
				if (x == xx) {
					i++; // invalid.
					continue;
				}

				var size = (int)(x & 7);

				if (i + size > count) {
					i++; // Short or invalid.
					continue;
				}
				var accept = AcceptRanges [x >> 4];
				c = buffer [i + 1];
				if (c < accept.Lo || accept.Hi < c) {
					i++;
					continue;
				}
				if (size == 2) {
					i += 2;
					continue;
				}
				c = buffer [i + 2];
				if (c < locb || hicb < c) {
					i++;
					continue;
				}
				if (size == 3) {
					i += 3;
					continue;
				}
				c = buffer [i + 3];
				if (c < locb || hicb < c) {
					i++;
					continue;
				}
				i += size;

			}
			return n;
		}

		/// <summary>
		/// Reports whether p consists entirely of valid UTF-8-encoded runes.
		/// </summary>
		/// <param name="buffer">Byte buffer containing a utf8 string.</param>
		public static bool Valid (byte [] buffer)
		{
			return InvalidIndex (buffer) == -1;
		}

		/// <summary>
		/// Use to find the index of the first invalid utf8 byte sequence in a buffer
		/// </summary>
		/// <returns>The index of the first invalid byte sequence or -1 if the entire buffer is valid.</returns>
		/// <param name="buffer">Buffer containing the utf8 buffer.</param>
		public static int InvalidIndex (byte [] buffer)
		{
			if (buffer == null)
				throw new ArgumentNullException (nameof (buffer));
			var n = buffer.Length;

			for (int i = 0; i < n;) {
				var pi = buffer [i];

				if (pi < RuneSelf) {
					i++;
					continue;
				}
				var x = first [pi];
				if (x == xx)
					return i; // Illegal starter byte.
				var size = (int)(x & 7);
				if (i + size > n)
					return i; // Short or invalid.
				var accept = AcceptRanges [x >> 4];

				var c = buffer [i + 1];

				if (c < accept.Lo || accept.Hi < c)
					return i;

				if (size == 2) {
					i += 2;
					continue;
				}
				c = buffer [i + 2];
				if (c < locb || hicb < c)
					return i;
				if (size == 3) {
					i += 3;
					continue;
				}
				c = buffer [i + 3];
				if (c < locb || hicb < c)
					return i;
				i += size;
			}
			return -1;
		}

		/// <summary>
		///  ValidRune reports whether a rune can be legally encoded as UTF-8.
		/// </summary>
		/// <returns><c>true</c>, if rune was validated, <c>false</c> otherwise.</returns>
		/// <param name="rune">The rune to test.</param>
		public static bool ValidRune (Rune rune)
		{
			if (0 <= rune.value && rune.value < surrogateMin)
				return true;
			if (surrogateMax < rune.value && rune.value <= MaxRune.value)
				return true;
			return false;
		}

		/// <summary>
		/// IsDigit reports whether the rune is a decimal digit.
		/// </summary>
		/// <returns><c>true</c>, if the rune is a mark, <c>false</c> otherwise.</returns>
		/// <param name="rune">The rune to test for.</param>
		public static bool IsDigit (Rune rune) => Unicode.IsDigit (rune.value);

		/// <summary>
		/// IsGraphic reports whether the rune is defined as a Graphic by Unicode.
		/// </summary>
		/// <returns><c>true</c>, if the rune is a lower case letter, <c>false</c> otherwise.</returns>
		/// <param name="rune">The rune to test for.</param>
		/// <remarks>
		/// Such characters include letters, marks, numbers, punctuation, symbols, and
		/// spaces, from categories L, M, N, P, S, Zs.
		/// </remarks>
		public static bool IsGraphic (Rune rune) => Unicode.IsGraphic (rune.value);

		/// <summary>
		/// IsPrint reports whether the rune is defined as printable.
		/// </summary>
		/// <returns><c>true</c>, if the rune is a lower case letter, <c>false</c> otherwise.</returns>
		/// <param name="rune">The rune to test for.</param>
		/// <remarks>
		/// Such characters include letters, marks, numbers, punctuation, symbols, and the
		/// ASCII space character, from categories L, M, N, P, S and the ASCII space
		/// character. This categorization is the same as IsGraphic except that the
		/// only spacing character is ASCII space, U+0020.
		/// </remarks>
		public static bool IsPrint (Rune rune) => Unicode.IsPrint (rune.value);


		/// <summary>
		/// IsControl reports whether the rune is a control character.
		/// </summary>
		/// <returns><c>true</c>, if the rune is a lower case letter, <c>false</c> otherwise.</returns>
		/// <param name="rune">The rune to test for.</param>
		/// <remarks>
		/// The C (Other) Unicode category includes more code points such as surrogates; use C.InRange (r) to test for them.
		/// </remarks>
		public static bool IsControl (Rune rune) => Unicode.IsControl (rune.value);

		/// <summary>
		/// IsLetter reports whether the rune is a letter (category L).
		/// </summary>
		/// <returns><c>true</c>, if the rune is a letter, <c>false</c> otherwise.</returns>
		/// <param name="rune">The rune to test for.</param>
		/// <remarks>
		/// </remarks>
		public static bool IsLetter (Rune rune) => Unicode.IsLetter (rune.value);

		/// <summary>
		/// IsLetterOrDigit reports whether the rune is a letter (category L) or a digit.
		/// </summary>
		/// <returns><c>true</c>, if the rune is a letter or digit, <c>false</c> otherwise.</returns>
		/// <param name="rune">The rune to test for.</param>
		/// <remarks>
		/// </remarks>
		public static bool IsLetterOrDigit (Rune rune) => Unicode.IsLetter (rune.value) || Unicode.IsDigit (rune.value);

		/// <summary>
		/// IsLetterOrDigit reports whether the rune is a letter (category L) or a number (category N).
		/// </summary>
		/// <returns><c>true</c>, if the rune is a letter or number, <c>false</c> otherwise.</returns>
		/// <param name="rune">The rune to test for.</param>
		/// <remarks>
		/// </remarks>
		public static bool IsLetterOrNumber (Rune rune) => Unicode.IsLetter (rune.value) || Unicode.IsNumber (rune.value);

		/// <summary>
		/// IsMark reports whether the rune is a letter (category M).
		/// </summary>
		/// <returns><c>true</c>, if the rune is a mark, <c>false</c> otherwise.</returns>
		/// <param name="rune">The rune to test for.</param>
		/// <remarks>
		/// Reports whether the rune is a mark character (category M).
		/// </remarks>
		public static bool IsMark (Rune rune) => Unicode.IsMark (rune.value);

		/// <summary>
		/// IsNumber reports whether the rune is a letter (category N).
		/// </summary>
		/// <returns><c>true</c>, if the rune is a mark, <c>false</c> otherwise.</returns>
		/// <param name="rune">The rune to test for.</param>
		/// <remarks>
		/// Reports whether the rune is a mark character (category N).
		/// </remarks>
		public static bool IsNumber (Rune rune) => Unicode.IsNumber (rune.value);

		/// <summary>
		/// IsPunct reports whether the rune is a letter (category P).
		/// </summary>
		/// <returns><c>true</c>, if the rune is a mark, <c>false</c> otherwise.</returns>
		/// <param name="rune">The rune to test for.</param>
		/// <remarks>
		/// Reports whether the rune is a mark character (category P).
		/// </remarks>
		public static bool IsPunctuation (Rune rune) => Unicode.IsPunct (rune.value);

		/// <summary>
		/// IsSpace reports whether the rune is a space character as defined by Unicode's White Space property.
		/// </summary>
		/// <returns><c>true</c>, if the rune is a mark, <c>false</c> otherwise.</returns>
		/// <param name="rune">The rune to test for.</param>
		/// <remarks>
		/// In the Latin-1 space, white space includes '\t', '\n', '\v', '\f', '\r', ' ', 
		/// U+0085 (NEL), U+00A0 (NBSP).
		/// Other definitions of spacing characters are set by category  Z and property Pattern_White_Space.
		/// </remarks>
		public static bool IsWhiteSpace (Rune rune) => Unicode.IsSpace (rune.value);

		/// <summary>
		/// IsSymbol reports whether the rune is a symbolic character.
		/// </summary>
		/// <returns><c>true</c>, if the rune is a mark, <c>false</c> otherwise.</returns>
		/// <param name="rune">The rune to test for.</param>
		public static bool IsSymbol (Rune rune) => Unicode.IsSymbol (rune.value);

		/// <summary>
		/// Reports whether the rune is an upper case letter.
		/// </summary>
		/// <returns><c>true</c>, if the rune is an upper case letter, <c>false</c> otherwise.</returns>
		/// <param name="rune">The rune to test for.</param>
		public static bool IsUpper (Rune rune) => Unicode.IsUpper (rune.value);

		/// <summary>
		/// Reports whether the rune is a lower case letter.
		/// </summary>
		/// <returns><c>true</c>, if the rune is a lower case letter, <c>false</c> otherwise.</returns>
		/// <param name="rune">The rune to test for.</param>
		public static bool IsLower (Rune rune) => Unicode.IsLower (rune.value);

		/// <summary>
		/// Reports whether the rune is a title case letter.
		/// </summary>
		/// <returns><c>true</c>, if the rune is a lower case letter, <c>false</c> otherwise.</returns>
		/// <param name="rune">The rune to test for.</param>
		public static bool IsTitle (Rune rune) => Unicode.IsTitle (rune.value);

		/// <summary>
		/// The types of cases supported.
		/// </summary>
		public enum Case {
			/// <summary>
			/// Upper case
			/// </summary>
			Upper = 0,

			/// <summary>
			/// Lower case
			/// </summary>
			Lower = 1,

			/// <summary>
			/// Title case capitalizes the first letter, and keeps the rest in lowercase.
			/// Sometimes it is not as straight forward as the uppercase, some characters require special handling, like
			/// certain ligatures and Greek characters.
			/// </summary>
			Title = 2
		};

		// To maps the rune to the specified case: Case.Upper, Case.Lower, or Case.Title
		/// <summary>
		/// To maps the rune to the specified case: Case.Upper, Case.Lower, or Case.Title
		/// </summary>
		/// <returns>The cased character.</returns>
		/// <param name="toCase">The destination case.</param>
		/// <param name="rune">Rune to convert.</param>
		public static Rune To (Case toCase, Rune rune)
		{
			uint rval = rune.value;
			switch (toCase) {
			case Case.Lower: 
				return new Rune (Unicode.To (Unicode.Case.Lower, rval));
			case Case.Title:
				return new Rune (Unicode.To (Unicode.Case.Title, rval));
			case Case.Upper:
				return new Rune (Unicode.To (Unicode.Case.Upper, rval));
			}
			return ReplacementChar;
		}


		/// <summary>
		/// ToUpper maps the rune to upper case.
		/// </summary>
		/// <returns>The upper cased rune if it can be.</returns>
		/// <param name="rune">Rune.</param>
		public static Rune ToUpper (Rune rune) => Unicode.ToUpper (rune.value);

		/// <summary>
		/// ToLower maps the rune to lower case.
		/// </summary>
		/// <returns>The lower cased rune if it can be.</returns>
		/// <param name="rune">Rune.</param>
		public static Rune ToLower (Rune rune) => Unicode.ToLower (rune.value);

		/// <summary>
		/// ToLower maps the rune to title case.
		/// </summary>
		/// <returns>The lower cased rune if it can be.</returns>
		/// <param name="rune">Rune.</param>
		public static Rune ToTitle (Rune rune) => Unicode.ToTitle (rune.value);

		/// <summary>
		/// SimpleFold iterates over Unicode code points equivalent under
		/// the Unicode-defined simple case folding.
		/// </summary>
		/// <returns>The simple-case folded rune.</returns>
		/// <param name="rune">Rune.</param>
		/// <remarks>
		/// SimpleFold iterates over Unicode code points equivalent under
		/// the Unicode-defined simple case folding. Among the code points
		/// equivalent to rune (including rune itself), SimpleFold returns the
		/// smallest rune > r if one exists, or else the smallest rune >= 0.
		/// If r is not a valid Unicode code point, SimpleFold(r) returns r.
		///
		/// For example:
		/// <code>
		///      SimpleFold('A') = 'a'
		///      SimpleFold('a') = 'A'
		///
		///      SimpleFold('K') = 'k'
		///      SimpleFold('k') = '\u212A' (Kelvin symbol, K)
		///      SimpleFold('\u212A') = 'K'
		///
		///      SimpleFold('1') = '1'
		///
		///      SimpleFold(-2) = -2
		/// </code>
		/// </remarks>
		public static Rune SimpleFold (Rune rune) => Unicode.SimpleFold (rune.value);

		/// <summary>
		/// Implicit operator conversion from a rune to an unsigned integer
		/// </summary>
		/// <returns>The unsigned integer representation.</returns>
		/// <param name="rune">Rune.</param>
		public static implicit operator uint (Rune rune) => rune.value;

		/// <summary>
		/// Implicit operator conversion from a C# char into a rune.
		/// </summary>
		/// <returns>Rune representing the C# character</returns>
		/// <param name="ch">16-bit Character.</param>
		public static implicit operator Rune (char ch) => new Rune (ch);

		/// <summary>
		/// Implicit operator conversion from an unsigned integer into a rune.
		/// </summary>
		/// <returns>Rune representing the C# character</returns>
		/// <param name="value">32-bit unsigned integer.</param>
		public static implicit operator Rune (uint value) => new Rune (value);

		/// <summary>
		/// Serves as a hash function for a <see cref="T:System.Rune"/> object.
		/// </summary>
		/// <returns>A hash code for this instance that is suitable for use in hashing algorithms and data structures such as a hash table.</returns>
		public override int GetHashCode ()
		{
			return (int)value;
		}

		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:System.Rune"/>.
		/// </summary>
		/// <returns>A <see cref="T:System.String"/> that represents the current <see cref="T:System.Rune"/>.</returns>
		public override string ToString ()
		{
			var buff = new byte [4];
			var size = EncodeRune (this, buff, 0);
			return System.Text.Encoding.UTF8.GetString(buff, 0, size);
		}

		/// <summary>
		/// Determines whether the specified <see cref="object"/> is equal to the current <see cref="T:System.Rune"/>.
		/// </summary>
		/// <param name="obj">The <see cref="object"/> to compare with the current <see cref="T:System.Rune"/>.</param>
		/// <returns><c>true</c> if the specified <see cref="object"/> is equal to the current <see cref="T:System.Rune"/>; otherwise, <c>false</c>.</returns>
		public override bool Equals (Object obj)
		{
			// Check for null values and compare run-time types.
			if (obj == null)
				return false;

			Rune p = (Rune)obj;
			return p.value == value;
		}
		
		static uint [,] combining = new uint [,] {
			{ 0x0300, 0x036F }, { 0x0483, 0x0486 }, { 0x0488, 0x0489 },
			{ 0x0591, 0x05BD }, { 0x05BF, 0x05BF }, { 0x05C1, 0x05C2 },
			{ 0x05C4, 0x05C5 }, { 0x05C7, 0x05C7 }, { 0x0600, 0x0603 },
			{ 0x0610, 0x0615 }, { 0x064B, 0x065E }, { 0x0670, 0x0670 },
			{ 0x06D6, 0x06E4 }, { 0x06E7, 0x06E8 }, { 0x06EA, 0x06ED },
			{ 0x070F, 0x070F }, { 0x0711, 0x0711 }, { 0x0730, 0x074A },
			{ 0x07A6, 0x07B0 }, { 0x07EB, 0x07F3 }, { 0x0901, 0x0902 },
			{ 0x093C, 0x093C }, { 0x0941, 0x0948 }, { 0x094D, 0x094D },
			{ 0x0951, 0x0954 }, { 0x0962, 0x0963 }, { 0x0981, 0x0981 },
			{ 0x09BC, 0x09BC }, { 0x09C1, 0x09C4 }, { 0x09CD, 0x09CD },
			{ 0x09E2, 0x09E3 }, { 0x0A01, 0x0A02 }, { 0x0A3C, 0x0A3C },
			{ 0x0A41, 0x0A42 }, { 0x0A47, 0x0A48 }, { 0x0A4B, 0x0A4D },
			{ 0x0A70, 0x0A71 }, { 0x0A81, 0x0A82 }, { 0x0ABC, 0x0ABC },
			{ 0x0AC1, 0x0AC5 }, { 0x0AC7, 0x0AC8 }, { 0x0ACD, 0x0ACD },
			{ 0x0AE2, 0x0AE3 }, { 0x0B01, 0x0B01 }, { 0x0B3C, 0x0B3C },
			{ 0x0B3F, 0x0B3F }, { 0x0B41, 0x0B43 }, { 0x0B4D, 0x0B4D },
			{ 0x0B56, 0x0B56 }, { 0x0B82, 0x0B82 }, { 0x0BC0, 0x0BC0 },
			{ 0x0BCD, 0x0BCD }, { 0x0C3E, 0x0C40 }, { 0x0C46, 0x0C48 },
			{ 0x0C4A, 0x0C4D }, { 0x0C55, 0x0C56 }, { 0x0CBC, 0x0CBC },
			{ 0x0CBF, 0x0CBF }, { 0x0CC6, 0x0CC6 }, { 0x0CCC, 0x0CCD },
			{ 0x0CE2, 0x0CE3 }, { 0x0D41, 0x0D43 }, { 0x0D4D, 0x0D4D },
			{ 0x0DCA, 0x0DCA }, { 0x0DD2, 0x0DD4 }, { 0x0DD6, 0x0DD6 },
			{ 0x0E31, 0x0E31 }, { 0x0E34, 0x0E3A }, { 0x0E47, 0x0E4E },
			{ 0x0EB1, 0x0EB1 }, { 0x0EB4, 0x0EB9 }, { 0x0EBB, 0x0EBC },
			{ 0x0EC8, 0x0ECD }, { 0x0F18, 0x0F19 }, { 0x0F35, 0x0F35 },
			{ 0x0F37, 0x0F37 }, { 0x0F39, 0x0F39 }, { 0x0F71, 0x0F7E },
			{ 0x0F80, 0x0F84 }, { 0x0F86, 0x0F87 }, { 0x0F90, 0x0F97 },
			{ 0x0F99, 0x0FBC }, { 0x0FC6, 0x0FC6 }, { 0x102D, 0x1030 },
			{ 0x1032, 0x1032 }, { 0x1036, 0x1037 }, { 0x1039, 0x1039 },
			{ 0x1058, 0x1059 }, { 0x1160, 0x11FF }, { 0x135F, 0x135F },
			{ 0x1712, 0x1714 }, { 0x1732, 0x1734 }, { 0x1752, 0x1753 },
			{ 0x1772, 0x1773 }, { 0x17B4, 0x17B5 }, { 0x17B7, 0x17BD },
			{ 0x17C6, 0x17C6 }, { 0x17C9, 0x17D3 }, { 0x17DD, 0x17DD },
			{ 0x180B, 0x180D }, { 0x18A9, 0x18A9 }, { 0x1920, 0x1922 },
			{ 0x1927, 0x1928 }, { 0x1932, 0x1932 }, { 0x1939, 0x193B },
			{ 0x1A17, 0x1A18 }, { 0x1B00, 0x1B03 }, { 0x1B34, 0x1B34 },
			{ 0x1B36, 0x1B3A }, { 0x1B3C, 0x1B3C }, { 0x1B42, 0x1B42 },
			{ 0x1B6B, 0x1B73 }, { 0x1DC0, 0x1DCA }, { 0x1DFE, 0x1DFF },
			{ 0x200B, 0x200F }, { 0x202A, 0x202E }, { 0x2060, 0x2063 },
			{ 0x206A, 0x206F }, { 0x20D0, 0x20EF }, { 0x302A, 0x302F },
			{ 0x3099, 0x309A }, { 0xA806, 0xA806 }, { 0xA80B, 0xA80B },
			{ 0xA825, 0xA826 }, { 0xFB1E, 0xFB1E }, { 0xFE00, 0xFE0F },
			{ 0xFE20, 0xFE23 }, { 0xFEFF, 0xFEFF }, { 0xFFF9, 0xFFFB },
			{ 0x10A01, 0x10A03 }, { 0x10A05, 0x10A06 }, { 0x10A0C, 0x10A0F },
			{ 0x10A38, 0x10A3A }, { 0x10A3F, 0x10A3F }, { 0x1D167, 0x1D169 },
			{ 0x1D173, 0x1D182 }, { 0x1D185, 0x1D18B }, { 0x1D1AA, 0x1D1AD },
			{ 0x1D242, 0x1D244 }, { 0xE0001, 0xE0001 }, { 0xE0020, 0xE007F },
			{ 0xE0100, 0xE01EF }
		};

		static int bisearch (uint rune, uint [,] table, int max)
		{
			int min = 0;
			int mid;

			if (rune < table [0, 0] || rune > table [max, 1])
				return 0;
			while (max >= min) {
				mid = (min + max) / 2;
				if (rune > table [mid, 1])
					min = mid + 1;
				else if (rune < table [mid, 0])
					max = mid - 1;
				else
					return 1;
			}

			return 0;
		}
		
		/// <summary>
		/// Number of column positions of a wide-character code.   This is used to measure runes as displayed by text-based terminals.
		/// </summary>
		/// <returns>The width in columns, 0 if the argument is the null character, -1 if the value is not printable, otherwise the number of columns that the rune occupies.</returns>
		/// <param name="rune">The rune.</param>
		public static int ColumnWidth (Rune rune)
		{
			uint irune = (uint)rune;
			if (irune < 32 || (irune >= 0x7f && irune <= 0xa0))
				return -1;
			if (irune < 127)
				return 1;
			/* binary search in table of non-spacing characters */
			if (bisearch (irune, combining, combining.GetLength (0)-1) != 0)
				return 0;
			/* if we arrive here, ucs is not a combining or C0/C1 control character */
			return 1 +
				((irune >= 0x1100 &&
				 (irune <= 0x115f ||                    /* Hangul Jamo init. consonants */
				 irune == 0x2329 || irune == 0x232a ||  /* Miscellaneous Technical */
				(irune >= 0x2e80 && irune <= 0xa4cf &&
				irune != 0x303f) ||						/* CJK ... Yi */
				(irune >= 0xac00 && irune <= 0xd7a3) || /* Hangul Syllables */
				(irune >= 0xf900 && irune <= 0xfaff) || /* CJK Compatibility Ideographs */
				(irune >= 0xfe10 && irune <= 0xfe19) || /* Vertical forms */
				(irune >= 0xfe30 && irune <= 0xfe6f) || /* CJK Compatibility Forms */
				(irune >= 0xff00 && irune <= 0xff60) || /* Fullwidth Forms */
				(irune >= 0xffe0 && irune <= 0xffe6) ||	/* Alphabetic Presentation Forms*/
				(irune >= 0x1fa00 && irune <= 0x1facf) || /* Chess Symbols*/
				(irune >= 0x20000 && irune <= 0x2fffd) ||
				  (irune >= 0x30000 && irune <= 0x3fffd))) ? 1 : 0);
		}
		
		/// <summary>
		/// FullRune reports whether the ustring begins with a full UTF-8 encoding of a rune.
		/// An invalid encoding is considered a full Rune since it will convert as a width-1 error rune.
		/// </summary>
		/// <returns><c>true</c>, if the bytes in p begin with a full UTF-8 encoding of a rune, <c>false</c> otherwise.</returns>
		/// <param name="str">The string to check.</param>
		public static bool FullRune (ustring str)
		{
			if ((object)str == null)
				throw new ArgumentNullException (nameof (str));
			var n = str.Length;

			if (n == 0)
				return false;
			var x = first [str [0]];
			if (n >= (x & 7)) {
				// ascii, invalid or valid
				return true;
			}
			// must be short or invalid
			if (n > 1) {
				var accept = AcceptRanges [x >> 4];
				var c = str [1];
				if (c < accept.Lo || accept.Hi < c)
					return true;
				else if (n > 2 && (str [2] < locb || hicb < str [2]))
					return true;
			}
			return false;
		}

		/// <summary>
		/// DecodeRune unpacks the first UTF-8 encoding in the ustring returns the rune and
		/// its width in bytes. 
		/// </summary>
		/// <returns>If p is empty it returns (RuneError, 0). Otherwise, if
		/// the encoding is invalid, it returns (RuneError, 1). Both are impossible
		/// results for correct, non-empty UTF-8.
		/// </returns>
		/// <param name="str">ustring to decode.</param>
		/// <param name="start">Starting offset to look into..</param>
		/// <param name="n">Number of bytes valid in the buffer, or -1 to make it the length of the buffer.</param>
		public static (Rune rune, int size) DecodeRune (ustring str, int start = 0, int n = -1)
		{
			if ((object)str == null)
				throw new ArgumentNullException (nameof (str));
			if (start < 0)
				throw new ArgumentException ("invalid offset", nameof (start));
			if (n < 0)
				n = str.Length - start;
			if (start > str.Length - n)
				throw new ArgumentException ("Out of bounds");

			if (n < 1)
				return (Error, 0);

			var p0 = str [start];
			var x = first [p0];
			if (x >= a1) {
				// The following code simulates an additional check for x == xx and
				// handling the ASCII and invalid cases accordingly. This mask-and-or
				// approach prevents an additional branch.
				uint mask = (uint)((((byte)x) << 31) >> 31); // Create 0x0000 or 0xFFFF.
				return (new Rune ((str [start]) & ~mask | Error.value & mask), 1);
			}

			var sz = x & 7;
			var accept = AcceptRanges [x >> 4];
			if (n < (int)sz)
				return (Error, 1);

			var b1 = str [start + 1];
			if (b1 < accept.Lo || accept.Hi < b1)
				return (Error, 1);

			if (sz == 2)
				return (new Rune ((uint)((p0 & mask2)) << 6 | (uint)((b1 & maskx))), 2);

			var b2 = str [start + 2];
			if (b2 < locb || hicb < b2)
				return (Error, 1);

			if (sz == 3)
				return (new Rune ((uint)((p0 & mask3)) << 12 | (uint)((b1 & maskx)) << 6 | (uint)((b2 & maskx))), 3);

			var b3 = str [start + 3];
			if (b3 < locb || hicb < b3) {
				return (Error, 1);
			}
			return (new Rune ((uint)(p0 & mask4) << 18 | (uint)(b1 & maskx) << 12 | (uint)(b2 & maskx) << 6 | (uint)(b3 & maskx)), 4);
		}


		/// <summary>
		/// DecodeLastRune unpacks the last UTF-8 encoding in the ustring.
		/// </summary>
		/// <returns>The last rune and its width in bytes.</returns>
		/// <param name="str">String to decode rune from;   if it is empty,
		/// it returns (RuneError, 0). Otherwise, if
		/// the encoding is invalid, it returns (RuneError, 1). Both are impossible
		/// results for correct, non-empty UTF-8.</param>
		/// <param name="end">Scan up to that point, if the value is -1, it sets the value to the length of the buffer.</param>
		/// <remarks>
		/// An encoding is invalid if it is incorrect UTF-8, encodes a rune that is
		/// out of range, or is not the shortest possible UTF-8 encoding for the
		/// value. No other validation is performed.</remarks> 
		public static (Rune rune, int size) DecodeLastRune (ustring str, int end = -1)
		{
			if ((object)str == null)
				throw new ArgumentNullException (nameof (str));
			if (str.Length == 0)
				return (Error, 0);
			if (end == -1)
				end = str.Length;
			else if (end > str.Length)
				throw new ArgumentException ("The end goes beyond the size of the buffer");

			var start = end - 1;
			uint r = str [start];
			if (r < RuneSelf)
				return (new Rune (r), 1);
			// guard against O(n^2) behavior when traversing
			// backwards through strings with long sequences of
			// invalid UTF-8.
			var lim = end - Utf8Max;

			if (lim < 0)
				lim = 0;

			for (start--; start >= lim; start--) {
				if (RuneStart (str [start])) {
					break;
				}
			}
			if (start < 0)
				start = 0;
			int size;
			Rune r2;
			(r2, size) = DecodeRune (str, start, end - start);
			if (start + size != end)
				return (Error, 1);
			return (r2, size);
		}


		/// <summary>
		/// Returns the number of runes in a ustring.
		/// </summary>
		/// <returns>Number of runes.</returns>
		/// <param name="str">utf8 string.</param>
		public static int RuneCount (ustring str)
		{
			if ((object)str == null)
				throw new ArgumentNullException (nameof (str));
			var count = str.Length;
			int n = 0;
			for (int i = 0; i < count;) {
				n++;
				var c = str [i];

				if (c < RuneSelf) {
					// ASCII fast path
					i++;
					continue;
				}
				var x = first [c];
				if (x == xx) {
					i++; // invalid.
					continue;
				}

				var size = (int)(x & 7);

				if (i + size > count) {
					i++; // Short or invalid.
					continue;
				}
				var accept = AcceptRanges [x >> 4];
				c = str [i + 1];
				if (c < accept.Lo || accept.Hi < c) {
					i++;
					continue;
				}
				if (size == 2) {
					i += 2;
					continue;
				}
				c = str [i + 2];
				if (c < locb || hicb < c) {
					i++;
					continue;
				}
				if (size == 3) {
					i += 3;
					continue;
				}
				c = str [i + 3];
				if (c < locb || hicb < c) {
					i++;
					continue;
				}
				i += size;

			}
			return n;
		}


		/// <summary>
		/// Use to find the index of the first invalid utf8 byte sequence in a buffer
		/// </summary>
		/// <returns>The index of the first invalid byte sequence or -1 if the entire buffer is valid.</returns>
		/// <param name="str">String containing the utf8 buffer.</param>
		public static int InvalidIndex (ustring str)
		{
			if ((object)str == null)
				throw new ArgumentNullException (nameof (str));
			var n = str.Length;

			for (int i = 0; i < n;) {
				var pi = str [i];

				if (pi < RuneSelf) {
					i++;
					continue;
				}
				var x = first [pi];
				if (x == xx)
					return i; // Illegal starter byte.
				var size = (int)(x & 7);
				if (i + size > n)
					return i; // Short or invalid.
				var accept = AcceptRanges [x >> 4];

				var c = str [i + 1];

				if (c < accept.Lo || accept.Hi < c)
					return i;

				if (size == 2) {
					i += 2;
					continue;
				}
				c = str [i + 2];
				if (c < locb || hicb < c)
					return i;
				if (size == 3) {
					i += 3;
					continue;
				}
				c = str [i + 3];
				if (c < locb || hicb < c)
					return i;
				i += size;
			}
			return -1;
		}

		/// <summary>
		/// Reports whether the ustring consists entirely of valid UTF-8-encoded runes.
		/// </summary>
		/// <param name="str">String to validate.</param>
		public static bool Valid (ustring str)
		{
			return InvalidIndex (str) == -1;
		}

		/// <summary>
		/// Given one byte from a utf8 string, return the number of expected bytes that make up the sequence.
		/// </summary>
		/// <returns>The number of UTF8 bytes expected given the first prefix.</returns>
		/// <param name="firstByte">Is the first byte of a UTF8 sequence.</param>
		public static int ExpectedSizeFromFirstByte (byte firstByte)
		{
			var x = first [firstByte];

			// Invalid runes, just return 1 for byte, and let higher level pass to print
			if (x == xx)
				return -1;
			if (x == a1)
				return 1;
			return x & 0xf;
		}
	}

}
