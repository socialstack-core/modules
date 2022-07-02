using System;

namespace Api.SocketServerLibrary.Crypto;


/// <summary>
/// Stateless implementation of SHA1.
/// This object can be instanced once globally and reused by stack based state.
/// </summary>
public class Sha1Digest
	: GeneralDigest
{
	private const int DigestLength = 20;

	/// <summary>
	/// 
	/// </summary>
	public override string AlgorithmName
	{
		get { return "SHA-1"; }
	}

	/// <summary>
	/// 
	/// </summary>
	/// <returns></returns>
	public override int GetDigestSize()
	{
		return DigestLength;
	}
	
	/// <summary>
	/// 
	/// </summary>
	/// <returns></returns>
	public override int GetBufferSize()
	{
		return 86;
	}

	internal override void ProcessWord(
		byte[]  input,
		int     inOff, Span<uint> X)
	{
		X[(int)X[80]] = ((uint)input[inOff] << 24) | ((uint)input[inOff + 1] << 16) | ((uint)input[inOff + 2] << 8) | (uint)input[inOff + 3];

		if (++X[80] == 16)
		{
			ProcessBlock(X);
		}
	}
	
	internal override void ProcessWord(
		Span<byte>  input,
		int     inOff, Span<uint> X)
	{
		X[(int)X[80]] = ((uint)input[inOff] << 24) | ((uint)input[inOff + 1] << 16) | ((uint)input[inOff + 2] << 8) | (uint)input[inOff + 3];

		if (++X[80] == 16)
		{
			ProcessBlock(X);
		}
	}

	internal override void ProcessLength(long    bitLength, Span<uint> X)
	{
		if (X[80] > 14)
		{
			ProcessBlock(X);
		}

		X[14] = (uint)((ulong)bitLength >> 32);
		X[15] = (uint)((ulong)bitLength);
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="output"></param>
	/// <param name="outOff"></param>
	/// <param name="X"></param>
	/// <param name="xBuf"></param>
	/// <param name="xBufOff"></param>
	/// <param name="byteCount"></param>
	/// <returns></returns>
	public override int DoFinalNoReset(
		byte[]  output,
		int     outOff,
		Span<uint> X, Span<byte> xBuf, ref int xBufOff, ref long byteCount)
	{
		Finish(X, xBuf, ref xBufOff, ref byteCount);

		// H1, big endian:
		var h = X[81];
		output[outOff++] = (byte)(h >> 24);
		output[outOff++] = (byte)(h >> 16);
		output[outOff++] = (byte)(h >> 8);
		output[outOff++] = (byte)h;

		// H2, big endian:
		h = X[82];
		output[outOff++] = (byte)(h >> 24);
		output[outOff++] = (byte)(h >> 16);
		output[outOff++] = (byte)(h >> 8);
		output[outOff++] = (byte)h;

		// H3, big endian:
		h = X[83];
		output[outOff++] = (byte)(h >> 24);
		output[outOff++] = (byte)(h >> 16);
		output[outOff++] = (byte)(h >> 8);
		output[outOff++] = (byte)h;

		// H4, big endian:
		h = X[84];
		output[outOff++] = (byte)(h >> 24);
		output[outOff++] = (byte)(h >> 16);
		output[outOff++] = (byte)(h >> 8);
		output[outOff++] = (byte)h;

		// H5, big endian:
		h = X[85];
		output[outOff++] = (byte)(h >> 24);
		output[outOff++] = (byte)(h >> 16);
		output[outOff++] = (byte)(h >> 8);
		output[outOff++] = (byte)h;

		return DigestLength;
	}
	
	/// <summary>
	/// 
	/// </summary>
	/// <param name="output"></param>
	/// <param name="outOff"></param>
	/// <param name="X"></param>
	/// <param name="xBuf"></param>
	/// <param name="xBufOff"></param>
	/// <param name="byteCount"></param>
	/// <returns></returns>
	public override int DoFinalNoReset(
		Span<byte> output,
		int     outOff,
		Span<uint> X, Span<byte> xBuf, ref int xBufOff, ref long byteCount)
	{
		Finish(X, xBuf, ref xBufOff, ref byteCount);

		// H1, big endian:
		var h = X[81];
		output[outOff++] = (byte)(h >> 24);
		output[outOff++] = (byte)(h >> 16);
		output[outOff++] = (byte)(h >> 8);
		output[outOff++] = (byte)h;

		// H2, big endian:
		h = X[82];
		output[outOff++] = (byte)(h >> 24);
		output[outOff++] = (byte)(h >> 16);
		output[outOff++] = (byte)(h >> 8);
		output[outOff++] = (byte)h;

		// H3, big endian:
		h = X[83];
		output[outOff++] = (byte)(h >> 24);
		output[outOff++] = (byte)(h >> 16);
		output[outOff++] = (byte)(h >> 8);
		output[outOff++] = (byte)h;

		// H4, big endian:
		h = X[84];
		output[outOff++] = (byte)(h >> 24);
		output[outOff++] = (byte)(h >> 16);
		output[outOff++] = (byte)(h >> 8);
		output[outOff++] = (byte)h;

		// H5, big endian:
		h = X[85];
		output[outOff++] = (byte)(h >> 24);
		output[outOff++] = (byte)(h >> 16);
		output[outOff++] = (byte)(h >> 8);
		output[outOff++] = (byte)h;

		return DigestLength;
	}

	/**
	 * reset the chaining variables
	 */
	public override void Reset(Span<uint> X)
	{
		X.Clear();
		X[81] = 0x67452301;
		X[82] = 0xefcdab89;
		X[83] = 0x98badcfe;
		X[84] = 0x10325476;
		X[85] = 0xc3d2e1f0;
	}

	//
	// Additive constants
	//
	private const uint Y1 = 0x5a827999;
	private const uint Y2 = 0x6ed9eba1;
	private const uint Y3 = 0x8f1bbcdc;
	private const uint Y4 = 0xca62c1d6;

	private static uint G(uint u, uint v, uint w)
	{
		return (u & v) | (u & w) | (v & w);
	}

	internal override void ProcessBlock(Span<uint> X)
	{
		//
		// expand 16 word block into 80 word block.
		//
		for (int i = 16; i < 80; i++)
		{
			uint t = X[i - 3] ^ X[i - 8] ^ X[i - 14] ^ X[i - 16];
			X[i] = t << 1 | t >> 31;
		}

		//
		// set up working variables.
		//
		uint A = X[81];
		uint B = X[82];
		uint C = X[83];
		uint D = X[84];
		uint E = X[85];

		// Future TODO: dotnet is gaining support for SIMD intrinsics for SHA1.

		//
		// round 1
		//
		int idx = 0;

		for (int j = 0; j < 4; j++)
		{
			// E = rotateLeft(A, 5) + F(B, C, D) + E + X[idx++] + Y1
			// B = rotateLeft(B, 30)
			E += (A << 5 | (A >> 27)) + ((B & C) | (~B & D)) + X[idx++] + Y1;
			B = B << 30 | (B >> 2);

			D += (E << 5 | (E >> 27)) + ((A & B) | (~A & C)) + X[idx++] + Y1;
			A = A << 30 | (A >> 2);

			C += (D << 5 | (D >> 27)) + ((E & A) | (~E & B)) + X[idx++] + Y1;
			E = E << 30 | (E >> 2);

			B += (C << 5 | (C >> 27)) + ((D & E) | (~D & A)) + X[idx++] + Y1;
			D = D << 30 | (D >> 2);

			A += (B << 5 | (B >> 27)) + ((C & D) | (~C & E)) + X[idx++] + Y1;
			C = C << 30 | (C >> 2);
		}

		//
		// round 2
		//
		for (int j = 0; j < 4; j++)
		{
			// E = rotateLeft(A, 5) + H(B, C, D) + E + X[idx++] + Y2
			// B = rotateLeft(B, 30)
			E += (A << 5 | (A >> 27)) + (B ^ C ^ D) + X[idx++] + Y2;
			B = B << 30 | (B >> 2);

			D += (E << 5 | (E >> 27)) + (A ^ B ^ C) + X[idx++] + Y2;
			A = A << 30 | (A >> 2);

			C += (D << 5 | (D >> 27)) + (E ^ A ^ B) + X[idx++] + Y2;
			E = E << 30 | (E >> 2);

			B += (C << 5 | (C >> 27)) + (D ^ E ^ A) + X[idx++] + Y2;
			D = D << 30 | (D >> 2);

			A += (B << 5 | (B >> 27)) + (C ^ D ^ E) + X[idx++] + Y2;
			C = C << 30 | (C >> 2);
		}

		//
		// round 3
		//
		for (int j = 0; j < 4; j++)
		{
			// E = rotateLeft(A, 5) + G(B, C, D) + E + X[idx++] + Y3
			// B = rotateLeft(B, 30)
			E += (A << 5 | (A >> 27)) + G(B, C, D) + X[idx++] + Y3;
			B = B << 30 | (B >> 2);

			D += (E << 5 | (E >> 27)) + G(A, B, C) + X[idx++] + Y3;
			A = A << 30 | (A >> 2);

			C += (D << 5 | (D >> 27)) + G(E, A, B) + X[idx++] + Y3;
			E = E << 30 | (E >> 2);

			B += (C << 5 | (C >> 27)) + G(D, E, A) + X[idx++] + Y3;
			D = D << 30 | (D >> 2);

			A += (B << 5 | (B >> 27)) + G(C, D, E) + X[idx++] + Y3;
			C = C << 30 | (C >> 2);
		}

		//
		// round 4
		//
		for (int j = 0; j < 4; j++)
		{
			// E = rotateLeft(A, 5) + H(B, C, D) + E + X[idx++] + Y4
			// B = rotateLeft(B, 30)
			E += (A << 5 | (A >> 27)) + (B ^ C ^ D) + X[idx++] + Y4;
			B = B << 30 | (B >> 2);

			D += (E << 5 | (E >> 27)) + (A ^ B ^ C) + X[idx++] + Y4;
			A = A << 30 | (A >> 2);

			C += (D << 5 | (D >> 27)) + (E ^ A ^ B) + X[idx++] + Y4;
			E = E << 30 | (E >> 2);

			B += (C << 5 | (C >> 27)) + (D ^ E ^ A) + X[idx++] + Y4;
			D = D << 30 | (D >> 2);

			A += (B << 5 | (B >> 27)) + (C ^ D ^ E) + X[idx++] + Y4;
			C = C << 30 | (C >> 2);
		}

		X[81] += A;
		X[82] += B;
		X[83] += C;
		X[84] += D;
		X[85] += E;

		//
		// reset start of the buffer.
		//
		X[80] = 0;
		X.Slice(0, 16).Clear();
	}
}
