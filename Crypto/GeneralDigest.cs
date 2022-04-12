using System;

using Org.BouncyCastle.Utilities;

namespace Api.WebRTC.Crypto;

/// <summary>
/// Stores digest state
/// </summary>
public class DigestState
{
	/// <summary>
	/// The main digest state in 4 byte words
	/// </summary>
	public uint[] State;
	/// <summary>
	/// Bytes to be processed
	/// </summary>
	public byte[] XBuffer;
	/// <summary>
	/// 0-3 value offset in the xbuffer (bytes yet to be processed)
	/// </summary>
	public int XBufOffset;
	/// <summary>
	/// Total bytes processed
	/// </summary>
	public long ByteCount;


	/// <summary>
	/// 
	/// </summary>
	/// <param name="state"></param>
	/// <param name="xBuffer"></param>
	/// <param name="xOffset"></param>
	/// <param name="byteCount"></param>
	public DigestState(Span<uint> state, Span<byte> xBuffer, int xOffset, long byteCount)
	{
		State = new uint[state.Length];
		state.CopyTo(State);
		XBuffer = new byte[xBuffer.Length];
		xBuffer.CopyTo(XBuffer);
		XBufOffset = xOffset;
		ByteCount = byteCount;
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="state"></param>
	/// <param name="xBuffer"></param>
	public void CopyTo(Span<uint> state, Span<byte> xBuffer)
	{
		State.CopyTo(state);
		XBuffer.CopyTo(xBuffer);
	}
}

/// <summary>
/// base implementation of MD4 family style digest as outlined in "Handbook of Applied Cryptography", pages 344 - 347.
/// </summary>
public abstract class GeneralDigest
{
	internal GeneralDigest()
	{
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="input"></param>
	/// <param name="runningState"></param>
	/// <param name="xBuf"></param>
	/// <param name="xBufOff"></param>
	/// <param name="byteCount"></param>
	public void Update(byte input,
		Span<uint> runningState,
		Span<byte> xBuf,
		ref int xBufOff,
		ref long byteCount)
	{
		xBuf[xBufOff++] = input;

		if (xBufOff == xBuf.Length)
		{
			ProcessWord(xBuf, 0, runningState);
			xBufOff = 0;
		}

		byteCount++;
	}
	
	/// <summary>
	/// 
	/// </summary>
	/// <param name="input"></param>
	/// <param name="runningState"></param>
	/// <param name="xBuf"></param>
	/// <param name="xBufOff"></param>
	/// <param name="byteCount"></param>
	public void WordUpdate(uint input,
		Span<uint> runningState,
		Span<byte> xBuf,
		ref int xBufOff,
		ref long byteCount)
	{
		for (var i = 24; i >= 0; i-= 8)
		{
			xBuf[xBufOff++] = (byte)(input >> i);

			if (xBufOff == xBuf.Length)
			{
				ProcessWord(xBuf, 0, runningState);
				xBufOff = 0;
			}

			byteCount++;
		}
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="input"></param>
	/// <param name="inOff"></param>
	/// <param name="length"></param>
	/// <param name="runningState"></param>
	/// <param name="xBuf"></param>
	/// <param name="xBufOff"></param>
	/// <param name="byteCount"></param>
	public void BlockUpdate(
		Span<byte>  input,
		int     inOff,
		int     length,
		Span<uint> runningState,
		Span<byte> xBuf,
		ref int xBufOff,
		ref long byteCount)
	{
		length = System.Math.Max(0, length);

		//
		// fill the current word
		//
		int i = 0;
		if (xBufOff != 0)
		{
			while (i < length)
			{
				xBuf[xBufOff++] = input[inOff + i++];
				if (xBufOff == 4)
				{
					ProcessWord(xBuf, 0, runningState);
					xBufOff = 0;
					break;
				}
			}
		}

		//
		// process whole words.
		//
		int limit = ((length - i) & ~3) + i;
		for (; i < limit; i += 4)
		{
			ProcessWord(input, inOff + i, runningState);
		}

		//
		// load in the remainder.
		//
		while (i < length)
		{
			xBuf[xBufOff++] = input[inOff + i++];
		}

		byteCount += length;
	}
	
	/// <summary>
	/// 
	/// </summary>
	/// <param name="input"></param>
	/// <param name="inOff"></param>
	/// <param name="length"></param>
	/// <param name="runningState"></param>
	/// <param name="xBuf"></param>
	/// <param name="xBufOff"></param>
	/// <param name="byteCount"></param>
	public void BlockUpdate(
		byte[]  input,
		int     inOff,
		int     length,
		Span<uint> runningState,
		Span<byte> xBuf,
		ref int xBufOff,
		ref long byteCount)
	{
		length = System.Math.Max(0, length);

		//
		// fill the current word
		//
		int i = 0;
		if (xBufOff != 0)
		{
			while (i < length)
			{
				xBuf[xBufOff++] = input[inOff + i++];
				if (xBufOff == 4)
				{
					ProcessWord(xBuf, 0, runningState);
					xBufOff = 0;
					break;
				}
			}
		}

		//
		// process whole words.
		//
		int limit = ((length - i) & ~3) + i;
		for (; i < limit; i += 4)
		{
			ProcessWord(input, inOff + i, runningState);
		}

		//
		// load in the remainder.
		//
		while (i < length)
		{
			xBuf[xBufOff++] = input[inOff + i++];
		}

		byteCount += length;
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="runningState"></param>
	/// <param name="xBuf"></param>
	/// <param name="xBufOff"></param>
	/// <param name="byteCount"></param>
	public void Finish(Span<uint> runningState, Span<byte> xBuf, ref int xBufOff, ref long byteCount)
	{
		long    bitLength = (byteCount << 3);

		//
		// add the pad bytes.
		//
		Update((byte)128, runningState, xBuf, ref xBufOff, ref byteCount);

		while (xBufOff != 0) Update((byte)0, runningState, xBuf, ref xBufOff, ref byteCount);
		ProcessLength(bitLength, runningState);
		ProcessBlock(runningState);
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="state"></param>
	public virtual void Reset(Span<uint> state)
	{
	}

	/// <summary>
	/// 
	/// </summary>
	/// <returns></returns>
	public abstract int GetBufferSize();

	/// <summary>
	/// 
	/// </summary>
	/// <param name="input"></param>
	/// <param name="inOff"></param>
	/// <param name="runningState"></param>
	internal abstract void ProcessWord(byte[] input, int inOff, Span<uint> runningState);
	/// <summary>
	/// 
	/// </summary>
	/// <param name="input"></param>
	/// <param name="inOff"></param>
	/// <param name="runningState"></param>
	internal abstract void ProcessWord(Span<byte> input, int inOff, Span<uint> runningState);
	/// <summary>
	/// 
	/// </summary>
	/// <param name="bitLength"></param>
	/// <param name="runningState"></param>
	internal abstract void ProcessLength(long bitLength, Span<uint> runningState);
	/// <summary>
	/// 
	/// </summary>
	/// <param name="runningState"></param>
	internal abstract void ProcessBlock(Span<uint> runningState);
	/// <summary>
	/// 
	/// </summary>
	public abstract string AlgorithmName { get; }
	/// <summary>
	/// 
	/// </summary>
	/// <returns></returns>
	public abstract int GetDigestSize();
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
	public abstract int DoFinalNoReset(byte[] output, int outOff, Span<uint> X, Span<byte> xBuf, ref int xBufOff, ref long byteCount);
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
	public abstract int DoFinalNoReset(Span<byte> output, int outOff, Span<uint> X, Span<byte> xBuf, ref int xBufOff, ref long byteCount);
}
