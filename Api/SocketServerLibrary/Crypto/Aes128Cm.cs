using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;

namespace Api.SocketServerLibrary.Crypto
{
	
    /// <summary>
    /// AES-128-CM, hardware accelerated on X86 architectures only.
    /// This is the cipher used by SRTP/ WebRTC and note that it is not quite the same as AES-GCM.
    /// </summary>
    public class Aes128Cm
    {
        private Vector128<byte>[] roundKeys;

        /// <summary>
        /// Creates a new accelerated AES128 instance. The resulting object is thread safe as it is internally stateless.
        /// </summary>
        /// <param name="key"></param>
        public Aes128Cm(Span<byte> key)
        {
            roundKeys = CreateRoundKeys(key);
        }

        /// <summary>
        /// Sets up the given key.
        /// </summary>
        /// <param name="key"></param>
        public void Init(Span<byte> key)
        {
            roundKeys = CreateRoundKeys(key);
        }

        /// <summary>
        /// Displays the round keys
        /// </summary>
        public void DumpKeys()
        {
            Span<byte> key = stackalloc byte[16];
            var sb = new StringBuilder();

            for (var i = 0; i < roundKeys.Length; i++)
            {
                Unsafe.WriteUnaligned(ref key[0], roundKeys[i]);

                var hexStr = Hex.Convert(key);

                sb.Append(i);
                sb.Append(": ");
                sb.Append(hexStr);
            }

			Log.Info("serversocketlibrary", sb.ToString());
		}

        static readonly Vector128<byte> ONE = Vector128.Create((byte)0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1);

        /// <summary>
        /// Thread safe.
        /// Encrypts the given plaintext, or decrypts the given ciphertext, in place. 
        /// Note that this method expects that the plaintext is relatively short (typically a single packet).
        /// </summary>
        /// <param name="buff"></param>
        /// <param name="off"></param>
        /// <param name="len"></param>
        /// <param name="iv"></param>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void Process(byte[] buff, int off, int len, Span<byte> iv)
        {   
            // For each block of 16 bytes..
            var blockCount = len >> 4;
            Span<Vector128<byte>> blocks = MemoryMarshal.Cast<byte, Vector128<byte>>(buff.AsSpan(off, len));
            Vector128<byte> ivVec = MemoryMarshal.Cast<byte, Vector128<byte>>(iv)[0];
            Vector128<byte>[] keys = roundKeys;
            Vector128<byte> b;

            // Makes the JIT remove all the other range checks on keys
            _ = keys[10];
            
            for (int i = 0; i < blockCount; i++)
            {
                // Generate the cipher stream for this block:
                b = Sse2.Xor(ivVec, keys[0]);
                b = Aes.Encrypt(b, keys[1]);
                b = Aes.Encrypt(b, keys[2]);
                b = Aes.Encrypt(b, keys[3]);
                b = Aes.Encrypt(b, keys[4]);
                b = Aes.Encrypt(b, keys[5]);
                b = Aes.Encrypt(b, keys[6]);
                b = Aes.Encrypt(b, keys[7]);
                b = Aes.Encrypt(b, keys[8]);
                b = Aes.Encrypt(b, keys[9]);
                b = Aes.EncryptLast(b, keys[10]);

                blocks[i] = Sse2.Xor(blocks[i], b);

                // Update the input IV:
                ivVec = Sse2.Add(ivVec, ONE);
            }

            // Partial block:
            var eob = (blockCount << 4);
            var remainingBytes = len - eob;

            if (remainingBytes > 0)
            {
                eob += off;

                // Prepare for the last block:
                b = Sse2.Xor(ivVec, keys[0]);
                b = Aes.Encrypt(b, keys[1]);
                b = Aes.Encrypt(b, keys[2]);
                b = Aes.Encrypt(b, keys[3]);
                b = Aes.Encrypt(b, keys[4]);
                b = Aes.Encrypt(b, keys[5]);
                b = Aes.Encrypt(b, keys[6]);
                b = Aes.Encrypt(b, keys[7]);
                b = Aes.Encrypt(b, keys[8]);
                b = Aes.Encrypt(b, keys[9]);
                b = Aes.EncryptLast(b, keys[10]);

                Span<byte> lastBlock = stackalloc byte[16];

                Unsafe.WriteUnaligned(ref lastBlock[0], b);

                for (var i = 0; i < remainingBytes; i++)
                {
                    buff[eob + i] = (byte)(buff[eob + i] ^ lastBlock[i]);
                }
            }
        }

        /// <summary>
        /// section 4.1.1 in RFC3711. Thread safe - you can call this with as many threads as you want.
        /// </summary>
        /// <param name="output"></param>
        /// <param name="length"></param>
        /// <param name="iv"></param>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void GetCipherStream(byte[] output, int length, Span<byte> iv)
        {
            var blockCount = length >> 4;
            Span<Vector128<byte>> blocks = MemoryMarshal.Cast<byte, Vector128<byte>>(output.AsSpan(0, length));
            Vector128<byte> ivVec = MemoryMarshal.Cast<byte, Vector128<byte>>(iv)[0];
            Vector128<byte>[] keys = roundKeys;
            Vector128<byte> b;

            // Makes the JIT remove all the other range checks on keys
            _ = keys[10];
            
            for (int i = 0; i < blockCount; i++)
            {
                // Generate the cipher stream for this block:
                b = Sse2.Xor(ivVec, keys[0]);
                b = Aes.Encrypt(b, keys[1]);
                b = Aes.Encrypt(b, keys[2]);
                b = Aes.Encrypt(b, keys[3]);
                b = Aes.Encrypt(b, keys[4]);
                b = Aes.Encrypt(b, keys[5]);
                b = Aes.Encrypt(b, keys[6]);
                b = Aes.Encrypt(b, keys[7]);
                b = Aes.Encrypt(b, keys[8]);
                b = Aes.Encrypt(b, keys[9]);
                b = Aes.EncryptLast(b, keys[10]);

                // Directly copy b to blocks[i]:
                blocks[i] = b;

                // Update the input IV:
                ivVec = Sse2.Add(ivVec, ONE);
            }

            // Partial block:
            var eob = (blockCount << 4);
            var remainingBytes = length - eob;

            if (remainingBytes > 0)
            {
                // Prepare for the last block:
                b = Sse2.Xor(ivVec, keys[0]);
                b = Aes.Encrypt(b, keys[1]);
                b = Aes.Encrypt(b, keys[2]);
                b = Aes.Encrypt(b, keys[3]);
                b = Aes.Encrypt(b, keys[4]);
                b = Aes.Encrypt(b, keys[5]);
                b = Aes.Encrypt(b, keys[6]);
                b = Aes.Encrypt(b, keys[7]);
                b = Aes.Encrypt(b, keys[8]);
                b = Aes.Encrypt(b, keys[9]);
                b = Aes.EncryptLast(b, keys[10]);

                // Directly copy b to blocks[i]:
                Span<byte> lastBlock = stackalloc byte[16];

                Unsafe.WriteUnaligned(ref lastBlock[0], b);

                for (var i = 0; i < remainingBytes; i++)
                {
                    output[eob + i] = lastBlock[i];
                }
            }
        }

        /// <summary>
        /// section 4.1.1 in RFC3711. Thread safe - you can call this with as many threads as you want.
        /// </summary>
        /// <param name="output"></param>
        /// <param name="iv"></param>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void GetCipherStream(Span<byte> output, Span<byte> iv)
        {
            var blockCount = output.Length >> 4;
            Span<Vector128<byte>> blocks = MemoryMarshal.Cast<byte, Vector128<byte>>(output);
            Vector128<byte> ivVec = MemoryMarshal.Cast<byte, Vector128<byte>>(iv)[0];
            Vector128<byte>[] keys = roundKeys;
            Vector128<byte> b;

            // Makes the JIT remove all the other range checks on keys
            _ = keys[10];
            
            for (int i = 0; i < blockCount; i++)
            {
                // Generate the cipher stream for this block:
                b = Sse2.Xor(ivVec, keys[0]);
                b = Aes.Encrypt(b, keys[1]);
                b = Aes.Encrypt(b, keys[2]);
                b = Aes.Encrypt(b, keys[3]);
                b = Aes.Encrypt(b, keys[4]);
                b = Aes.Encrypt(b, keys[5]);
                b = Aes.Encrypt(b, keys[6]);
                b = Aes.Encrypt(b, keys[7]);
                b = Aes.Encrypt(b, keys[8]);
                b = Aes.Encrypt(b, keys[9]);
                b = Aes.EncryptLast(b, keys[10]);

                // Directly copy b to blocks[i]:
                blocks[i] = b;

                // Update the input IV:
                ivVec = Sse2.Add(ivVec, ONE);
            }

            // Partial block:
            var eob = (blockCount << 4);
            var remainingBytes = output.Length - eob;

            if (remainingBytes > 0)
            {
                // Prepare for the last block:
                b = Sse2.Xor(ivVec, keys[0]);
                b = Aes.Encrypt(b, keys[1]);
                b = Aes.Encrypt(b, keys[2]);
                b = Aes.Encrypt(b, keys[3]);
                b = Aes.Encrypt(b, keys[4]);
                b = Aes.Encrypt(b, keys[5]);
                b = Aes.Encrypt(b, keys[6]);
                b = Aes.Encrypt(b, keys[7]);
                b = Aes.Encrypt(b, keys[8]);
                b = Aes.Encrypt(b, keys[9]);
                b = Aes.EncryptLast(b, keys[10]);

                // Directly copy b to blocks[i]:
                Span<byte> lastBlock = stackalloc byte[16];

                Unsafe.WriteUnaligned(ref lastBlock[0], b);

                lastBlock.Slice(0, remainingBytes).CopyTo(output.Slice(eob));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private static Vector128<byte>[] CreateRoundKeys(Span<byte> key)
        {
            var keys = new Vector128<byte>[11];

            keys[0] = Unsafe.ReadUnaligned<Vector128<byte>>(ref key[0]);

            MakeRoundKey(keys, 1, 0x01);
            MakeRoundKey(keys, 2, 0x02);
            MakeRoundKey(keys, 3, 0x04);
            MakeRoundKey(keys, 4, 0x08);
            MakeRoundKey(keys, 5, 0x10);
            MakeRoundKey(keys, 6, 0x20);
            MakeRoundKey(keys, 7, 0x40);
            MakeRoundKey(keys, 8, 0x80);
            MakeRoundKey(keys, 9, 0x1b);
            MakeRoundKey(keys, 10, 0x36);

            return keys;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private static void MakeRoundKey(Vector128<byte>[] keys, int i, byte rcon)
        {
            Vector128<byte> s = keys[i - 1];
            Vector128<byte> t = keys[i - 1];

            t = Aes.KeygenAssist(t, rcon);
            t = Sse2.Shuffle(t.AsUInt32(), 0xFF).AsByte();

            s = Sse2.Xor(s, Sse2.ShiftLeftLogical128BitLane(s, 4));
            s = Sse2.Xor(s, Sse2.ShiftLeftLogical128BitLane(s, 8));

            keys[i] = Sse2.Xor(s, t);
        }
    }
}