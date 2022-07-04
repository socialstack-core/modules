using Org.BouncyCastle.Math;
using System;
using System.Text;

namespace Lumity.SikeIsogeny;

/**
 * Converter for octet encoding specified in SIKE specification.
 *
 * @author Roman Strobl, roman.strobl@wultra.com
 */
public class OctetEncoding {

    private OctetEncoding() {

    }

    /**
     * Convert a BigInteger into octet string.
     *
     * @param n      A non-negative number to convert.
     * @param length Length of generated octet string specified as number of octets.
     * @return Converted octet string.
     */
    public static string ToOctetString(BigInteger n, int length) {
        if (n.SignValue == -1) {
            throw new Exception("Number is negative");
        }
        string hex = n.ToString(16).ToUpper();
        if (hex.Length % 2 == 1) {
            hex = "0" + hex;
        }
        char[] chars = hex.ToCharArray();
        int expectedLength = length * 2;
        if (chars.Length > expectedLength) {
            throw new Exception("Number is too large, length: " + chars.Length + ", expected: " + expectedLength);
        }
        StringBuilder sb = new StringBuilder();
        for (int i = hex.Length - 1; i >= 0; i -= 2) {
            sb.Append(chars[i - 1]);
            sb.Append(chars[i]);
        }
        for (int i = 0; i < expectedLength - chars.Length; i++) {
            sb.Append('0');
        }
        return sb.ToString();
    }

    /**
     * Convert a byte array representing a number into an octet string.
     *
     * @param data   Byte array representing a number.
     * @param length Length of generated octet string specified as number of octets.
     * @return Converted octet string.
     */
    public static string ToOctetString(byte[] data, int length) {
        return ToOctetString(ByteEncoding.FromByteArray(data), length);
    }


    /**
     * Convert an octet string into BigInteger.
     *
     * @param str Octet string.
     * @return Converted BigInteger value.
     */
    public static BigInteger FromOctetString(string str) {
        if (str == null || str.Length % 2 == 1) {
            throw new Exception("Invalid octet string");
        }
        char[] chars = str.ToCharArray();
        StringBuilder sb = new StringBuilder();
        for (int i = chars.Length - 1; i >= 0; i -= 2) {
            sb.Append(chars[i - 1]);
            sb.Append(chars[i]);
        }
        return new BigInteger(sb.ToString(), 16);
    }

    /**
     * Convert an octet string to byte array.
     * @param str Octet string.
     * @param length Expected length of byte array.
     * @return Converted byte array value.
     */
    public static byte[] FromOctetString(string str, int length) {
        return ByteEncoding.ToByteArray(FromOctetString(str), length);
    }
}