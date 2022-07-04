// From https://github.com/migueldeicaza/NStack
// C# ification by Miguel de Icaza

using System;
using System.Runtime.InteropServices;

namespace Api.Startup.Utf8Helpers {
	
	/// <summary>
	/// Unicode class contains helper methods to support Unicode encoding.
	/// </summary>
	/// <remarks>
	/// <para>
	///    Generally the Unicode class provided methods that can help you classify and
	///    convert Unicode code points.  The word codepoint is considered a mouthful so in
	///    this class, the word "rune" is used instead and is represented by the
	///    uint value type.  
	/// </para>
	/// <para>
	///    Unicode code points can be produced by combining independent characters,
	///    so the rune for a character can be produced by combining one character and
	///    other elements of it.  Runes on the other hand correspond to a specific
	///    character.
	/// </para>
	/// <para>
	///    This class surfaces various methods to classify case of a Rune, like
	///    <see cref="M:Unicode.IsUpper"/>, <see cref="M:Unicode.IsLower"/>, <see cref="M:Unicode.IsDigit"/>,
	///    <see cref="M:Unicode.IsGraphic"/> to convert runes from one case to another using the <see cref="M:Unicode.ToUpper"/>,
	///    <see cref="M:Unicode.ToLower"/>, <see cref="M:Unicode.ToTitle"/> as well as various constants
	///    that are useful when working with Unicode runes.
	/// </para>  
	/// <para>
	///    Unicode defines various character classes which are surfaced as RangeTables
	///    as static properties in this class.   You can probe whether a rune belongs
	///    to a specific range table
	/// </para>
	/// </remarks>
	public partial class Unicode {

		/// <summary>
		/// Special casing rules for Turkish.
		/// </summary>
		public static SpecialCase TurkishCase = new SpecialCase (
			new CaseRange [] {
				new CaseRange (0x0049, 0x0049, 0, 0x131 - 0x49, 0),
				new CaseRange (0x0069, 0x0069, 0x130 - 0x69, 0, 0x130 - 0x69),
				new CaseRange (0x0130, 0x0130, 0, 0x69 - 0x130, 0),
				new CaseRange (0x0131, 0x0131, 0x49 - 0x131, 0, 0x49 - 0x131),
			});
			
		/// <summary>
		/// IsDigit reports whether the rune is a decimal digit.
		/// </summary>
		/// <returns><c>true</c>, if the rune is a mark, <c>false</c> otherwise.</returns>
		/// <param name="rune">The rune to test for.</param>
		public static bool IsDigit (uint rune)
		{
			if (rune < MaxLatin1)
				return '0' <= rune && rune <= '9';
			return Category.Digit.IsExcludingLatin (rune);
		}
		
		[Flags]
		internal enum CharClass : byte {
			pC = 1 << 0, // a control character.
			pP = 1 << 1, // a punctuation character.
			pN = 1 << 2, // a numeral.
			pS = 1 << 3, // a symbolic character.
			pZ = 1 << 4, // a spacing character.
			pLu = 1 << 5, // an upper-case letter.
			pLl = 1 << 6, // a lower-case letter.
			pp = 1 << 7, // a printable character according to Go's definition.
			pg = pp | pZ,   // a graphical character according to the Unicode definition.
			pLo = pLl | pLu, // a letter that is neither upper nor lower case.
			pLmask = pLo
		}

		/// <summary>
		/// The range tables for graphics
		/// </summary>
		public static RangeTable [] GraphicRanges = new [] {
			Category._L, Category._M, Category._N, Category._P, Category._S, Category._Zs
		};

		/// <summary>
		/// The range tables for print
		/// </summary>
		public static RangeTable [] PrintRanges = new [] {
			Category._L, Category._M, Category._N, Category._P, Category._S
		};

		/// <summary>
		/// Determines if a rune is on a set of ranges.
		/// </summary>
		/// <returns><c>true</c>, if rune in ranges was used, <c>false</c> otherwise.</returns>
		/// <param name="rune">Rune.</param>
		/// <param name="inRanges">In ranges.</param>
		public static bool IsRuneInRanges (uint rune, params RangeTable [] inRanges)
		{
			foreach (var range in inRanges)
				if (range.InRange (rune))
					return true;
			return false;
		}

		/// <summary>
		/// IsGraphic reports whether the rune is defined as a Graphic by Unicode.
		/// </summary>
		/// <returns><c>true</c>, if the rune is a lower case letter, <c>false</c> otherwise.</returns>
		/// <param name="rune">The rune to test for.</param>
		/// <remarks>
		/// Such characters include letters, marks, numbers, punctuation, symbols, and
		/// spaces, from categories L, M, N, P, S, Zs.
		/// </remarks>
		public static bool IsGraphic (uint rune)
		{
			if (rune < MaxLatin1)
				return (properties [rune] & CharClass.pg) != 0;
			return IsRuneInRanges (rune, GraphicRanges);
		}

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
		public static bool IsPrint (uint rune)
		{
			if (rune < MaxLatin1)
				return (properties [rune] & CharClass.pp) != 0;
			return IsRuneInRanges (rune, PrintRanges);
		}

		/// <summary>
		/// IsControl reports whether the rune is a control character.
		/// </summary>
		/// <returns><c>true</c>, if the rune is a lower case letter, <c>false</c> otherwise.</returns>
		/// <param name="rune">The rune to test for.</param>
		/// <remarks>
		/// The C (Other) Unicode category includes more code points such as surrogates; use C.InRange (r) to test for them.
		/// </remarks>
		public static bool IsControl (uint rune)
		{
			if (rune < MaxLatin1)
				return (properties [rune] & CharClass.pC) != 0;
			// All control characters are < MaxLatin1.
			return false;
		}

		/// <summary>
		/// IsLetter reports whether the rune is a letter (category L).
		/// </summary>
		/// <returns><c>true</c>, if the rune is a letter, <c>false</c> otherwise.</returns>
		/// <param name="rune">The rune to test for.</param>
		/// <remarks>
		/// </remarks>
		public static bool IsLetter (uint rune)
		{
			if (rune < MaxLatin1)
				return (properties [rune] & CharClass.pLmask) != 0;
			return Category.L.IsExcludingLatin (rune);
		}

		/// <summary>
		/// IsMark reports whether the rune is a letter (category M).
		/// </summary>
		/// <returns><c>true</c>, if the rune is a mark, <c>false</c> otherwise.</returns>
		/// <param name="rune">The rune to test for.</param>
		/// <remarks>
		/// Reports whether the rune is a mark character (category M).
		/// </remarks>
		public static bool IsMark (uint rune)
		{
			// There are no mark characters in Latin-1.
			return Category.M.IsExcludingLatin (rune);
		}

		/// <summary>
		/// IsNumber reports whether the rune is a letter (category N).
		/// </summary>
		/// <returns><c>true</c>, if the rune is a mark, <c>false</c> otherwise.</returns>
		/// <param name="rune">The rune to test for.</param>
		/// <remarks>
		/// Reports whether the rune is a mark character (category N).
		/// </remarks>
		public static bool IsNumber (uint rune)
		{
			if (rune < MaxLatin1)
				return (properties [rune] & CharClass.pN) != 0;
			return Category.N.IsExcludingLatin (rune);
		}

		/// <summary>
		/// IsPunct reports whether the rune is a letter (category P).
		/// </summary>
		/// <returns><c>true</c>, if the rune is a mark, <c>false</c> otherwise.</returns>
		/// <param name="rune">The rune to test for.</param>
		/// <remarks>
		/// Reports whether the rune is a mark character (category P).
		/// </remarks>
		public static bool IsPunct (uint rune)
		{
			if (rune < MaxLatin1)
				return (properties [rune] & CharClass.pP) != 0;
			return Category.P.IsExcludingLatin (rune);
		}

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
		public static bool IsSpace (uint rune)
		{
			if (rune < MaxLatin1) {
				if (rune == '\t' || rune == '\n' || rune == '\v' || rune == '\f' || rune == '\r' || rune == ' ' || rune == 0x85 || rune == 0xa0)
					return true;
				return false;
			}
			return Property.White_Space.IsExcludingLatin (rune);
		}

		/// <summary>
		/// IsSymbol reports whether the rune is a symbolic character.
		/// </summary>
		/// <returns><c>true</c>, if the rune is a mark, <c>false</c> otherwise.</returns>
		/// <param name="rune">The rune to test for.</param>
		public static bool IsSymbol (uint rune)
		{
			if (rune < MaxLatin1)
				return (properties [rune] & CharClass.pS) != 0;
			return Category.S.IsExcludingLatin (rune);
		}

		/// <summary>
		/// Maximum valid Unicode code point.
		/// </summary> 
		public const int MaxRune = 0x0010FFFF;

		/// <summary>
		/// Represents invalid code points.
		/// </summary>
		public const uint ReplacementChar = 0xfffd;     // 

		/// <summary>
		/// The maximum ASCII value.
		/// </summary>
		public const uint MaxAscii = 0x7f;

		/// <summary>
		/// The maximum latin1 value.
		/// </summary>
		public const uint MaxLatin1 = 0xff;

		// Range16 represents of a range of 16-bit Unicode code points. The range runs from Lo to Hi
		// inclusive and has the specified stride.
		internal struct Range16 {
			public ushort Lo, Hi, Stride;

			public Range16 (ushort lo, ushort hi, ushort stride)
			{
				Lo = lo;
				Hi = hi;
				Stride = stride;
			}
		}

		// Range32 represents of a range of Unicode code points and is used when one or
		// more of the values will not fit in 16 bits. The range runs from Lo to Hi
		// inclusive and has the specified stride. Lo and Hi must always be >= 1<<16.
		internal struct Range32 {
			public int Lo, Hi, Stride;

			public Range32 (int lo, int hi, int stride)
			{
				Lo = lo;
				Hi = hi;
				Stride = stride;
			}

		}

		/// <summary>
		/// Range tables describe classes of unicode code points.
		/// </summary>
		/// 
		// RangeTable defines a set of Unicode code points by listing the ranges of
		// code points within the set. The ranges are listed in two slices
		// to save space: a slice of 16-bit ranges and a slice of 32-bit ranges.
		// The two slices must be in sorted order and non-overlapping.
		// Also, R32 should contain only values >= 0x10000 (1<<16).
		public struct RangeTable {
			readonly Range16 []R16;
			readonly Range32 []R32;

			/// <summary>
			/// The number of entries in the short range table (R16) with with Hi being less than MaxLatin1
			/// </summary>
			public readonly int LatinOffset;

			internal RangeTable (Range16 [] r16 = null, Range32 [] r32 = null, int latinOffset = 0)
			{
				R16 = r16;
				R32 = r32;
				LatinOffset = latinOffset;
			}

			/// <summary>
			/// Used to determine if a given rune is in the range of this RangeTable.
			/// </summary>
			/// <returns><c>true</c>, if the rune is in this RangeTable, <c>false</c> otherwise.</returns>
			/// <param name="rune">Rune.</param>
			public bool InRange (uint rune)
			{
				if (R16 != null) {
					var r16l = R16.Length;

					if (rune <= R16 [r16l - 1].Hi)
						return Is16 (R16, (ushort)rune);
				}
				if (R32 != null) {
					var r32l = R32.Length;
					if (rune >= R32 [0].Lo)
						return Is32 (R32, rune);
				}
				return false;
			}

			/// <summary>
			/// Used to determine if a given rune is in the range of this RangeTable, excluding latin1 characters.
			/// </summary>
			/// <returns><c>true</c>, if the rune is part of the range (not including latin), <c>false</c> otherwise.</returns>
			/// <param name="rune">Rune.</param>
			public bool IsExcludingLatin (uint rune)
			{
				var off = LatinOffset;

				if (R16 != null) {
					var r16l = R16.Length;

					if (r16l > off && rune <= R16 [r16l - 1].Hi)
						return Is16 (R16, (ushort)rune, off);
				}
				if (R32 != null) {
					if (R32.Length > 0 && rune >= R32 [0].Lo)
						return Is32 (R32, rune);
				}
				return false;
			}
		}

		// CaseRange represents a range of Unicode code points for simple (one
		// code point to one code point) case conversion.
		// The range runs from Lo to Hi inclusive, with a fixed stride of 1.  Deltas
		// are the number to add to the code point to reach the code point for a
		// different case for that character. They may be negative. If zero, it
		// means the character is in the corresponding case. There is a special
		// case representing sequences of alternating corresponding Upper and Lower
		// pairs. It appears with a fixed Delta of
		//      {UpperLower, UpperLower, UpperLower}
		// The constant UpperLower has an otherwise impossible delta value.
		[StructLayout(LayoutKind.Explicit, Size = 20)]
		internal struct CaseRange {
			[FieldOffset(0)]
			public int Upper;
			[FieldOffset(4)]
			public int Lower;
			[FieldOffset(8)]
			public int Title;
			[FieldOffset(12)]
			public int Lo;
			[FieldOffset(16)]
			public int Hi;

			public CaseRange (int lo, int hi, int d1, int d2, int d3)
			{
				Lo = lo;
				Hi = hi;
				Upper = d1;
				Lower = d2;
				Title = d3;
			}
		}

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
			/// Titlecase capitalizes the first letter, and keeps the rest in lowercase.
			/// Sometimes it is not as straight forward as the uppercase, some characters require special handling, like
			/// certain ligatures and greek characters.
			/// </summary>
			Title = 2
		};

		// If the Delta field of a CaseRange is UpperLower, it means
		// this CaseRange represents a sequence of the form (say)
		// Upper Lower Upper Lower.
		const int UpperLower = MaxRune + 1;

		// linearMax is the maximum size table for linear search for non-Latin1 rune.
		const int linearMax = 18;

		static bool Is16 (Range16 [] ranges, ushort r, int lo = 0)
		{
			if (ranges.Length -lo < linearMax || r <= MaxLatin1) {
				for (int i = lo; i < ranges.Length; i++){
					var range = ranges [i];
				
					if (r < range.Lo)
						return false;
					if (r <= range.Hi)
						return (r - range.Lo) % range.Stride == 0;
				}
				return false;
			}
			var hi = ranges.Length;
			// binary search over ranges
			while (lo < hi) {
				var m = lo + (hi - lo) / 2;
				var range = ranges [m];
				if (range.Lo <= r && r <= range.Hi) 
					return (r - range.Lo) % range.Stride == 0;
				if (r < range.Lo)
					hi = m;
				else
					lo = m + 1;
			}
			return false;
		}

		static bool Is32 (Range32 [] ranges, uint r)
		{
			var hi = ranges.Length;
			if (hi < linearMax || r <= MaxLatin1) {
				foreach (var range in ranges) {
					if (r < range.Lo)
						return false;
					if (r <= range.Hi)
						return (r - range.Lo) % range.Stride == 0;
				}
				return false;
			}
			// binary search over ranges
			var lo = 0;
			while (lo < hi) {
				var m = lo + (hi - lo) / 2;
				var range = ranges [m];
				if (range.Lo <= r && r <= range.Hi)
					return (r - range.Lo) % range.Stride == 0;
				if (r < range.Lo)
					hi = m;
				else
					lo = m + 1;
			}
			return false;
		}

		/// <summary>
		/// Reports whether the rune is an upper case letter.
		/// </summary>
		/// <returns><c>true</c>, if the rune is an upper case lette, <c>false</c> otherwise.</returns>
		/// <param name="rune">The rune to test for.</param>
		public static bool IsUpper (uint rune)
		{
			if (rune <= MaxLatin1)
				return (properties [(byte)rune] & CharClass.pLmask) == CharClass.pLu;
			return Category.Upper.IsExcludingLatin (rune);
		}

		/// <summary>
		/// Reports whether the rune is a lower case letter.
		/// </summary>
		/// <returns><c>true</c>, if the rune is a lower case lette, <c>false</c> otherwise.</returns>
		/// <param name="rune">The rune to test for.</param>
		public static bool IsLower (uint rune)
		{
			if (rune <= MaxLatin1)
				return (properties [(byte)rune] & CharClass.pLmask) == CharClass.pLl;
			return Category.Lower.IsExcludingLatin (rune);
		}

		/// <summary>
		/// Reports whether the rune is a title case letter.
		/// </summary>
		/// <returns><c>true</c>, if the rune is a lower case lette, <c>false</c> otherwise.</returns>
		/// <param name="rune">The rune to test for.</param>
		public static bool IsTitle (uint rune)
		{
			if (rune <= MaxLatin1)
				return false;
			return Category.Title.IsExcludingLatin (rune);
		}

		// to maps the rune using the specified case mapping.
		static uint to (Case toCase, uint rune, CaseRange [] caseRange)
		{
			if (toCase < 0 || toCase > Case.Title)
				return ReplacementChar;
			
			// binary search over ranges
			var lo = 0;
			var hi = caseRange.Length;

			while (lo < hi) {
				var m = lo + (hi - lo) / 2;
				var cr = caseRange [m];
				if (cr.Lo <= rune && rune <= cr.Hi) {
					int delta = 0;

					switch (toCase)
					{
						case Case.Upper:
							delta = cr.Upper;
							break;
						case Case.Lower:
							delta = cr.Lower;
							break;
						case Case.Title:
							delta = cr.Title;
							break;
					}

					if (delta > MaxRune) {
						// In an Upper-Lower sequence, which always starts with
						// an UpperCase letter, the real deltas always look like:
						//      {0, 1, 0}    UpperCase (Lower is next)
						//      {-1, 0, -1}  LowerCase (Upper, Title are previous)
						// The characters at even offsets from the beginning of the
						// sequence are upper case; the ones at odd offsets are lower.
						// The correct mapping can be done by clearing or setting the low
						// bit in the sequence offset.
						// The constants UpperCase and TitleCase are even while LowerCase
						// is odd so we take the low bit from _case.

						return ((uint)cr.Lo) + (((rune - ((uint)(cr.Lo))) & 0xfffffffe) | ((uint)((uint)toCase) & 1));      
					}
					return (uint) ((int)rune + delta);
				}
				if (rune < cr.Lo)
					hi = m;
				else
					lo = m + 1;
			}
			return rune;
		}

		// To maps the rune to the specified case: Case.Upper, Case.Lower, or Case.Title
		/// <summary>
		/// To maps the rune to the specified case: Case.Upper, Case.Lower, or Case.Title
		/// </summary>
		/// <returns>The cased character.</returns>
		/// <param name="toCase">The destination case.</param>
		/// <param name="rune">Rune to convert.</param>
		public static uint To (Case toCase, uint rune)
		{
			return to (toCase, rune, CaseRanges);
		}

		/// <summary>
		/// ToUpper maps the rune to upper case.
		/// </summary>
		/// <returns>The upper cased rune if it can be.</returns>
		/// <param name="rune">Rune.</param>
		public static uint ToUpper (uint rune)
		{
			if (rune <= MaxAscii) {
				if ('a' <= rune && rune <= 'z')
					rune -= 'a' - 'A';
				return rune;
			}
			return To (Case.Upper, rune);
		}

		/// <summary>
		/// ToLower maps the rune to lower case.
		/// </summary>
		/// <returns>The lower cased rune if it can be.</returns>
		/// <param name="rune">Rune.</param>
		public static uint ToLower (uint rune)
		{
			if (rune <= MaxAscii) {
				if ('A' <= rune && rune <= 'Z')
					rune += 'a' - 'A';
				return rune;
			}
			return To (Case.Lower, rune);
		}

		/// <summary>
		/// ToLower maps the rune to title case.
		/// </summary>
		/// <returns>The lower cased rune if it can be.</returns>
		/// <param name="rune">Rune.</param>
		public static uint ToTitle (uint rune)
		{
			if (rune <= MaxAscii) {
				if ('a' <= rune && rune <= 'z')
					rune -= 'a' - 'A';
				return rune;
			}
			return To (Case.Title, rune);
		}

		/// <summary>
		/// SpecialCase represents language-specific case mappings such as Turkish.
		/// </summary>
		/// <remarks>
		/// Methods of SpecialCase customize (by overriding) the standard mappings.
		/// </remarks>
		public struct SpecialCase {
			Unicode.CaseRange [] Special;
			internal SpecialCase (CaseRange [] special)
			{
				Special = special;
			}

			/// <summary>
			/// ToUpper maps the rune to upper case giving priority to the special mapping.
			/// </summary>
			/// <returns>The upper cased rune if it can be.</returns>
			/// <param name="rune">Rune.</param>
			public uint ToUpper (uint rune)
			{
				var result = to (Case.Upper, rune, Special);
				if (result == rune)
					result = Unicode.ToUpper (rune);
				return result;
			}

			/// <summary>
			/// ToTitle maps the rune to title case giving priority to the special mapping.
			/// </summary>
			/// <returns>The title cased rune if it can be.</returns>
			/// <param name="rune">Rune.</param>
			public uint ToTitle (uint rune)
			{
				var result = to (Case.Title, rune, Special);
				if (result == rune)
					result = Unicode.ToTitle (rune);
				return result;
			}

			/// <summary>
			/// ToLower maps the rune to lower case giving priority to the special mapping.
			/// </summary>
			/// <returns>The lower cased rune if it can be.</returns>
			/// <param name="rune">Rune.</param>
			public uint ToLower (uint rune)
			{
				var result = to (Case.Lower, rune, Special);
				if (result == rune)
					result = Unicode.ToLower (rune);
				return result;
			}
		}

		// CaseOrbit is defined in tables.cs as foldPair []. Right now all the
		// entries fit in ushort, so use ushort.  If that changes, compilation
		// will fail (the constants in the composite literal will not fit in ushort)
		// and the types here can change to uint.
		struct FoldPair {
			public ushort From, To;

			public FoldPair (ushort from, ushort to)
			{
				From = from;
				To = to;
			}
		}

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
		public static uint SimpleFold (uint rune)
		{
			if (rune >= MaxRune)
				return rune;
			if (rune < asciiFold.Length)
				return (uint)asciiFold [rune];
			// Consult caseOrbit table for special cases.
			var lo = 0;
			var hi = CaseOrbit.Length;
			while (lo < hi) {
				var m = lo + (hi - lo) / 2;
				if (CaseOrbit [m].From < rune)
					lo = m + 1;
				else
					hi = m;
			}
			if (lo < CaseOrbit.Length && CaseOrbit [lo].From == rune)
				return CaseOrbit [lo].To;
			// No folding specified. This is a one- or two-element
			// equivalence class containing rune and ToLower(rune)
			// and ToUpper(rune) if they are different from rune.
			var l = ToLower (rune);
			if (l != rune)
				return l;
			return ToUpper (rune);
		}
	}

}
