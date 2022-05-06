using System;
namespace Api.SocketServerLibrary.Crypto;

/**
* HMAC implementation based on RFC2104
*
* H(K XOR opad, H(K XOR ipad, text))
*/
public class HMac
{
	private const byte IPAD = (byte)0x36;
	private const byte OPAD = (byte)0x5C;
	private const int BYTE_LENGTH = 64;

	private readonly GeneralDigest digest;
	private readonly int digestSize;
	private readonly int bufferSize;
	private DigestState ipadState;
	private DigestState opadState;

	/// <summary>
	/// 
	/// </summary>
	/// <param name="digest"></param>
	public HMac(GeneralDigest digest)
	{
		this.digest = digest;
		this.digestSize = digest.GetDigestSize();
		this.bufferSize = digest.GetBufferSize();
	}

	/// <summary>
	/// Can be called by multiple threads simultaneously. This generates a hash exclusively on the stack using the local keying material as readonly.
	/// </summary>
	/// <param name="packetBuffer"></param>
	/// <param name="startIndex"></param>
	/// <param name="size"></param>
	/// <param name="rocIn"></param>
	/// <param name="outputBuffer"></param>
	public void StatelessOutput(byte[] packetBuffer, int startIndex, int size, uint rocIn, Span<byte> outputBuffer)
	{
		var d = digest;

		// This usually semi-large (a few hundred bytes) buffer tracks the state but on the stack:
		Span<uint> state = stackalloc uint[bufferSize];
		Span<byte> xBuf = stackalloc byte[4];
		var xBufOff = ipadState.XBufOffset;
		long byteCount = ipadState.ByteCount;
		ipadState.CopyTo(state, xBuf); // Reset

		// Run the block update followed by the single word extension:
		d.BlockUpdate(packetBuffer, startIndex, size, state, xBuf, ref xBufOff, ref byteCount);
		d.WordUpdate(rocIn, state, xBuf, ref xBufOff, ref byteCount);

		// Output the digest:
		Span<byte> digestBuffer = stackalloc byte[digestSize];
		d.DoFinalNoReset(digestBuffer, 0, state, xBuf, ref xBufOff, ref byteCount);

		xBufOff = opadState.XBufOffset;
		byteCount = opadState.ByteCount;
		opadState.CopyTo(state, xBuf);

		d.BlockUpdate(digestBuffer, 0, digestSize, state, xBuf, ref xBufOff, ref byteCount);

		d.DoFinalNoReset(outputBuffer, 0, state, xBuf, ref xBufOff, ref byteCount);
	}

	/// <summary>
	/// Can be called by multiple threads simultaneously. This generates a hash exclusively on the stack using the local keying material as readonly.
	/// </summary>
	/// <param name="packetBuffer"></param>
	/// <param name="startIndex"></param>
	/// <param name="size"></param>
	/// <param name="outputBuffer"></param>
	public void StatelessOutputSingleBlock(byte[] packetBuffer, int startIndex, int size, Span<byte> outputBuffer)
	{
		var d = digest;

		// This usually semi-large (a few hundred bytes) buffer tracks the state but on the stack:
		Span<uint> state = stackalloc uint[bufferSize];
		Span<byte> xBuf = stackalloc byte[4];
		var xBufOff = ipadState.XBufOffset;
		long byteCount = ipadState.ByteCount;
		ipadState.CopyTo(state, xBuf); // Reset

		// Run the single block update:
		d.BlockUpdate(packetBuffer, startIndex, size, state, xBuf, ref xBufOff, ref byteCount);

		// Write result hmac:
		Span<byte> digestBuffer = stackalloc byte[digestSize];
		d.DoFinalNoReset(digestBuffer, 0, state, xBuf, ref xBufOff, ref byteCount);

		xBufOff = opadState.XBufOffset;
		byteCount = opadState.ByteCount;
		opadState.CopyTo(state, xBuf);

		d.BlockUpdate(digestBuffer, 0, digestSize, state, xBuf, ref xBufOff, ref byteCount);

		d.DoFinalNoReset(outputBuffer, 0, state, xBuf, ref xBufOff, ref byteCount);
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="key"></param>
	/// <param name="keyOffset"></param>
	/// <param name="keyLength"></param>
	public virtual void Init(byte[] key, int keyOffset, int keyLength)
	{
		Span<uint> state = stackalloc uint[bufferSize];
		Span<byte> xBuf = stackalloc byte[4];
		var xBufOff = 0;
		long byteCount = 0;

		digest.Reset(state);

		Span<byte> inputPad = stackalloc byte[BYTE_LENGTH];
		Span<byte> outputBuf = stackalloc byte[BYTE_LENGTH];

		if (keyLength > BYTE_LENGTH)
		{
			digest.BlockUpdate(key, 0, keyLength, state, xBuf, ref xBufOff, ref byteCount);
			digest.DoFinalNoReset(inputPad, 0, state, xBuf, ref xBufOff, ref byteCount);
			digest.Reset(state);
		}
		else
		{
			for (var i = 0; i < keyLength; i++)
			{
				inputPad[i] = key[i];
			}
		}

		inputPad.CopyTo(outputBuf);

		XorPad(inputPad, BYTE_LENGTH, IPAD);
		XorPad(outputBuf, BYTE_LENGTH, OPAD);

		var stateToRestore = new DigestState(state, xBuf, xBufOff, byteCount);
		digest.BlockUpdate(outputBuf, 0, BYTE_LENGTH, state, xBuf, ref xBufOff, ref byteCount);
		opadState = new DigestState(state, xBuf, xBufOff, byteCount);

		// Restore from state object:
		xBufOff = stateToRestore.XBufOffset;
		byteCount = stateToRestore.ByteCount;
		stateToRestore.CopyTo(state, xBuf);

		digest.BlockUpdate(inputPad, 0, inputPad.Length, state, xBuf, ref xBufOff, ref byteCount);
		ipadState = new DigestState(state, xBuf, xBufOff, byteCount);
	}

	/*
	public void Update(byte input)
	{
		digest.Update(input);
	}

	public void BlockUpdate(byte[] input, int inOff, int len)
	{
		digest.BlockUpdate(input, inOff, len);
	}

	public int OutputMac(byte[] output, int outOff)
	{
		Span<byte> digestBuffer = stackalloc byte[digestSize];
		digest.DoFinal(ref digestBuffer, 0);

		digest.CopyIn(opadState);
		digest.BlockUpdate(digestBuffer, 0, digestSize);

		return digest.DoFinal(output, outOff);
	}
	
	public int OutputMac(ref Span<byte> output, int outOff)
	{
		Span<byte> digestBuffer = stackalloc byte[digestSize];
		digest.DoFinal(ref digestBuffer, 0);

		digest.CopyIn(opadState);
		digest.BlockUpdate(digestBuffer, 0, digestSize);

		return digest.DoFinal(ref output, outOff);
	}
	
	/**
	* Reset the mac generator to the state that it was in after the Init call.
	
	public void Reset()
	{
		// Reset underlying digest:
		digest.CopyIn(ipadState);
	}
	*/

	private static void XorPad(Span<byte> pad, int len, byte n)
	{
		for (int i = 0; i < len; ++i)
		{
			pad[i] ^= n;
		}
	}
}
