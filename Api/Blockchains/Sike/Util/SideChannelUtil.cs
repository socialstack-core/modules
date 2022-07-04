using Org.BouncyCastle.Math;
using Org.BouncyCastle.Utilities;

namespace Lumity.SikeIsogeny;

/**
 * Utilities for preventing side channel attacks.
 *
 * @author Roman Strobl, roman.strobl@wultra.com
 */
public static class SideChannelUtil {

    /// <summary>
    /// 2 as a bigint.
    /// </summary>
    public static readonly BigInteger BigIntegerTwo = new BigInteger("2");
    
    /// <summary>
    /// 3 as a bigint.
    /// </summary>
    public static readonly BigInteger BigIntegerThree = new BigInteger("3");
    
    /// <summary>
    /// 4 as a bigint.
    /// </summary>
    public static readonly BigInteger BigIntegerFour = new BigInteger("4");
    
    /// <summary>
    /// 6 as a bigint.
    /// </summary>
    public static readonly BigInteger BigIntegerSix = new BigInteger("6");

    /**
     * Compare two byte arrays in constant time.
     * @param bytes1 First byte array.
     * @param bytes2 Second byte array.
     * @return Whether byte arrays are equal.
     */
    public static bool ConstantTimeAreEqual(byte[] bytes1, byte[] bytes2) {
        return Arrays.ConstantTimeAreEqual(bytes1, bytes2);
    }
}
