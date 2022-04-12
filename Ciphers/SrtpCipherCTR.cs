//-----------------------------------------------------------------------------
// Filename: SrtpCipherCTR.cs
//
// Description: Implements SRTP Counter Mode Encryption.
//
// Derived From:
// https://github.com/jitsi/jitsi-srtp/blob/master/src/main/java/org/jitsi/srtp/crypto/SrtpCipherCtr.java
//
// Author(s):
// Rafael Soares (raf.csoares@kyubinteractive.com)
//
// History:
// 01 Jul 2020	Rafael Soares   Created.
//
// License:
// Customisations: BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
// Original Source: Apache License: see below
//-----------------------------------------------------------------------------

/*
 * Copyright @ 2016 - present 8x8, Inc
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

/*
 * SRTPCipherF8 implements SRTP F8 Mode AES Encryption (AES-f8).
 * F8 Mode AES Encryption algorithm is defined in RFC3711, section 4.1.2.
 * 
 * Other than Null Cipher, RFC3711 defined two encryption algorithms:
 * Counter Mode AES Encryption and F8 Mode AES encryption. Both encryption
 * algorithms are capable to encrypt / decrypt arbitrary length data, and the
 * size of packet data is not required to be a multiple of the AES block 
 * size (128bit). So, no padding is needed.
 * 
 * Please note: these two encryption algorithms are specially defined by SRTP.
 * They are not common AES encryption modes, so you will not be able to find a 
 * replacement implementation in common cryptographic libraries. 
 * 
 * As defined by RFC3711: F8 mode encryption is optional.
 *
 *                        mandatory to impl     optional      default
 * -------------------------------------------------------------------------
 *   encryption           AES-CM, NULL          AES-f8        AES-CM
 *   message integrity    HMAC-SHA1                -          HMAC-SHA1
 *   key derivation       (PRF) AES-CM             -          AES-CM 
 *
 * We use AESCipher to handle basic AES encryption / decryption.
 * 
 * author Bing SU (nova.su@gmail.com)
 * author Werner Dittmann werner.dittmann@t-online.de
 */

using System;

namespace Api.WebRTC.Ciphers;
    
/*
    * SRTPCipherCTR implements SRTP Counter Mode AES Encryption (AES-CM).
    * Counter Mode AES Encryption algorithm is defined in RFC3711, section 4.1.1.
    * 
    * Other than Null Cipher, RFC3711 defined two two encryption algorithms:
    * Counter Mode AES Encryption and F8 Mode AES encryption. Both encryption
    * algorithms are capable to encrypt / decrypt arbitrary length data, and the
    * size of packet data is not required to be a multiple of the AES block 
    * size (128bit). So, no padding is needed.
    * 
    * Please note: these two encryption algorithms are specially defined by SRTP.
    * They are not common AES encryption modes, so you will not be able to find a 
    * replacement implementation in common cryptographic libraries. 
    *
    * As defined by RFC3711: Counter Mode Encryption is mandatory..
    *
    *                        mandatory to impl     optional      default
    * -------------------------------------------------------------------------
    *   encryption           AES-CM, NULL          AES-f8        AES-CM
    *   message integrity    HMAC-SHA1                -          HMAC-SHA1
    *   key derivation       (PRF) AES-CM             -          AES-CM 
    *
    * We use AESCipher to handle basic AES encryption / decryption.
    * 
    * @author Werner Dittmann (Werner.Dittmann@t-online.de)
    * @author Bing SU (nova.su@gmail.com)
    */

/// <summary>
/// 
/// </summary>
public class SrtpCipherCTR
{
    /// <summary>
    /// Cipher is stateless in that it doesn't contain any per-call state, only unchanging key material. I.e. instance cannot be shared.
    /// </summary>
    public Crypto.AesEngine Cipher = new Crypto.AesEngine();

    /// <summary>
    /// Thread safe - you can call this with as many threads as you want simultaneously.
    /// </summary>
    /// <param name="buff"></param>
    /// <param name="off"></param>
    /// <param name="len"></param>
    /// <param name="iv"></param>
    public void Process(byte[] buff, int off, int len, Span<byte> iv)
    {
        Span<byte> tmpCipherBlock = stackalloc byte[16];

        // For each block of 16 bytes..
        var blockCount = len >> 4;
        var outputOffset = off;

        for (int block = 0; block < blockCount; block++)
        {
            // Update the input IV:
            iv[14] = (byte)((block & 0xFF00) >> 8);
            iv[15] = (byte)((block & 0x00FF));
            
            // Generate the cipher stream for this block:
            Cipher.ProcessBlock(iv, 0, tmpCipherBlock, 0);

            for (var b = 0; b < 16; b++)
            {
                buff[outputOffset] = (byte)(buff[outputOffset] ^ tmpCipherBlock[b]);
                outputOffset++;
            }
        }

        // Handle the remaining (non 16 multiple) bytes:
        var remainingBytes = len - (blockCount * 16);

        iv[14] = (byte)((blockCount & 0xFF00) >> 8);
        iv[15] = (byte)((blockCount & 0x00FF));

        Cipher.ProcessBlock(iv, 0, tmpCipherBlock, 0);
        
        for (var b = 0; b < remainingBytes; b++)
        {
            buff[outputOffset] = (byte)(buff[outputOffset] ^ tmpCipherBlock[b]);
            outputOffset++;
        }
    }

    /// <summary>
    /// section 4.1.1 in RFC3711. Thread safe - you can call this with as many threads as you want.
    /// </summary>
    /// <param name="_out"></param>
    /// <param name="length"></param>
    /// <param name="iv"></param>
    public void GetCipherStream(byte[] _out, int length, Span<byte> iv)
    {
        Span<byte> cipherInBlock = stackalloc byte[16];
        Span<byte> tmpCipherBlock = stackalloc byte[16];
        iv.CopyTo(cipherInBlock);

        int ctr;
        for (ctr = 0; ctr < length / 16; ctr++)
        {
            // compute the cipher stream
            cipherInBlock[14] = (byte)((ctr & 0xFF00) >> 8);
            cipherInBlock[15] = (byte)((ctr & 0x00FF));

            Cipher.ProcessBlock(cipherInBlock, 0, _out, ctr * 16);
        }

        // Treat the last bytes:
        cipherInBlock[14] = (byte)((ctr & 0xFF00) >> 8);
        cipherInBlock[15] = (byte)((ctr & 0x00FF));

        Cipher.ProcessBlock(cipherInBlock, 0, tmpCipherBlock, 0);

        var byteCount = length % 16;
        var outIndex = ctr * 16;

        for (var i = 0; i < byteCount; i++)
        {
            _out[outIndex++] = tmpCipherBlock[i];
        }
    }
}