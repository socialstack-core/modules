namespace Lumity.SikeIsogeny;

/**
 * Mathematical functions for the 64-bit unsigned integer type.
 * All methods are constant time to prevent side channel attacks.
 *
 * @author Roman Strobl, roman.strobl@wultra.com
 */
public class UnsignedLong {

    private UnsignedLong() {

    }

    /**
     * Add two unsigned long values with carry.
     * @param x First unsigned long value.
     * @param y Second unsigned long value.
     * @param carry Carry set to 1 in case of overflow, otherwise 0.
     * @return Unsigned long addition result.
     */
    public static ulong Add(ulong x, ulong y, ref ulong carry) {
        ulong sum = unchecked(x + y + carry);
        carry = (((x & y) | ((x | y) & ~sum)) >> 63);
        return sum;
    }

    /**
     * Subtract two unsigned long values with borrow.
     * @param x First unsigned long value.
     * @param y Second unsigned long value.
     * @param borrow Borrow set to 1 in case of underflow, otherwise 0.
     * @return Unsigned long subtraction result.
     */
    public static ulong Sub(ulong x, ulong y, ref ulong borrow) {
        ulong sub = unchecked(x - y);

        // 1 ^ just inverts it. If it was 0, it becomes 1.
        // ( >> 63)) Gets the sign bit.
        // (sub | -sub) Really just checking for == 0?

        ulong borrowOut = (sub == 0 ? borrow : 0) | (x ^ ((x ^ y) | ((x - y) ^ y))) >> 63;
        ulong diff = unchecked(sub - borrow);
        borrow = borrowOut;
        return diff;
    }

    /**
     * Multiply two unsigned long values.
     * @param x First unsigned long value.
     * @param y Second unsigned long value.
     * @return Result of multiplication of two unsigned longs, represented by their hi and lo values, each 64-bit.
     */
    public static ulong Mul(ulong x, ulong y, out ulong lo) {
        ulong al, bl, ah, bh, albl, albh, ahbl, ahbh;
        ulong res1, res2, res3;
        ulong carry, temp;
        ulong maskL = 0L, maskH;
        ulong hi;

		unchecked
        {
            maskL = (~maskL) >> 32;
            maskH = ~maskL;

            al = x & maskL;
            ah = x >> 32;
            bl = y & maskL;
            bh = y >> 32;

            albl = al * bl;
            albh = al * bh;
            ahbl = ah * bl;
            ahbh = ah * bh;
            lo = albl & maskL;

            res1 = albl >> 32;
            res2 = ahbl & maskL;
            res3 = albh & maskL;
            temp = res1 + res2 + res3;
            carry = temp >> 32;
            lo ^= temp << 32;

            res1 = ahbl >> 32;
            res2 = albh >> 32;
            res3 = ahbh & maskL;
            temp = res1 + res2 + res3 + carry;
            hi = temp & maskL;
            carry = temp & maskH;
            hi ^= (ahbh & maskH) + carry;
        }
        return hi;
    }

}
