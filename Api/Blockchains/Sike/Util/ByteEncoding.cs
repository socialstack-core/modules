using Org.BouncyCastle.Math;
using Org.BouncyCastle.Utilities;
using System;

namespace Lumity.SikeIsogeny;

/**
 * Converter for byte encoding for compatibility with the GMP library.
 *
 * @author Roman Strobl, roman.strobl@wultra.com
 */
public class ByteEncoding {

    private ByteEncoding() {

    }

    /**
     * Convert unsigned number from byte array. The byte array is stored in big-endian ordering.
     *
     * @param data Byte array representing the number.
     * @return Converted number.
     */
    public static BigInteger FromByteArray(byte[] data) {
        return new BigInteger(Reverse(data));
    }

    /**
     * Convert unsigned number into a byte array. The byte array is stored in big-endian ordering.
     * @param n Number to convert.
     * @param length Length of byte array.
     * @return Byte array representing converted number.
     */
    public static byte[] ToByteArray(BigInteger n, int length) {
        byte[] encoded = Reverse(BigIntegers.AsUnsignedByteArray(n));
        if (encoded.Length > length) {
            throw new Exception("Number is too large");
        }
        if (encoded.Length == length) {
            return encoded;
        }
        byte[] padded = new byte[length];
        Array.Copy(encoded, 0, padded, 0, encoded.Length);
        return padded;
    }

    /**
     * Reverse byte array.
     * @param data Source byte array.
     * @return Reversed byte array.
     */
    private static byte[] Reverse(byte[] data) {
        byte[] o = new byte[data.Length];
        for(int i = 0; i < data.Length; i++) {
            o[i] = data[data.Length - i - 1];
        }
        return o;
    }

}
