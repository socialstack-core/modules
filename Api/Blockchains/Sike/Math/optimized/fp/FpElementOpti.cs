using Org.BouncyCastle.Math;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Lumity.SikeIsogeny;

/**
 * Representation of an optimized element of the base field F(p).
 *
 * @author Roman Strobl, roman.strobl@wultra.com
 */
public struct FpElementOpti {

    private SikeParam sikeParam;
    private ulong[] value;

    /**
     * FpElement constructor.
     * @param sikeParam SIKE parameters.
     */
    public FpElementOpti(SikeParam sikeParam) {
        ulong[] value = new ulong[sikeParam.GetFpWords()];
        this.sikeParam = sikeParam;
        this.value = value;
    }

    /**
     * FpElement constructor with provided long array value.
     * @param sikeParam SIKE parameters.
     * @param value Element value.
     */
    public FpElementOpti(SikeParam sikeParam, ulong[] value) {
        this.sikeParam = sikeParam;
        this.value = value;
    }

    /**
     * FpElement constructor with provided BigInteger value.
     * @param sikeParam SIKE parameters.
     * @param x BigInteger value.
     */
    public FpElementOpti(SikeParam sikeParam, BigInteger x) {
        this.sikeParam = sikeParam;
        // Convert element to Montgomery domain
        ulong[] value = new ulong[sikeParam.GetFpWords()];
        int primeSize = (sikeParam.GetPrime().BitLength + 7) / 8;
        byte[] encoded = ByteEncoding.ToByteArray(x, primeSize);
        for (int i = 0; i < primeSize; i++) {
            int j = i / 8;
            int k = i % 8;
            value[j] |= (((ulong)encoded[i] & 0xFFL) << (8 * k));
        }
        FpElementOpti a = new FpElementOpti(sikeParam, value);
        FpElementOpti b = (FpElementOpti) a.Multiply(sikeParam.GetPR2());
        FpElementOpti reduced = b.ReduceMontgomery();
        this.value = new ulong[sikeParam.GetFpWords()];
        Array.Copy(reduced.GetValue(), 0, this.value, 0, sikeParam.GetFpWords());
    }

    /**
     * Get element value as long array.
     * @return Element value as long array.
     */
    public ulong[] GetValue() {
        return value;
    }

    /**
     * Get element long array size.
     * @return Elemennt long array size.
     */
    public int Size() {
        return value.Length;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public BigInteger GetX() {
        return ByteEncoding.FromByteArray(GetEncoded());
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="o"></param>
    /// <returns></returns>
    public FpElementOpti Add(FpElementOpti o) {
        // Compute z = x + y (mod 2*p)
        var z = new FpElementOpti(sikeParam);
        ulong carry = 0L;

        var fpWordsLength = sikeParam.GetFpWords();
        var zv = z.GetValue();
        var ov = o.GetValue();
        var px2v = sikeParam.GetPx2().GetValue();

        // z = x + y % p
        for (int i = 0; i < fpWordsLength; i++) {
            ulong result = UnsignedLong.Add(value[i], ov[i], ref carry);
            zv[i] = result;
        }

        // z = z - p * 2
        carry = 0L;
        for (int i = 0; i < fpWordsLength; i++) {
            ulong result = UnsignedLong.Sub(zv[i], px2v[i], ref carry);
            zv[i] = result;
        }

        // if z < 0, add p * 2 back
        ulong mask = carry == 0 ? 0 : ulong.MaxValue;
        carry = 0L;
        for (int i = 0; i < fpWordsLength; i++) {
            ulong result = UnsignedLong.Add(zv[i], px2v[i] & mask, ref carry);
            zv[i] = result;
        }
        return z;
    }

    /**
     * Add two elements without reduction.
     * @param o Other element.
     * @return Calculation result.
     */
    public FpElementOpti AddNoReduction(FpElementOpti o) {
        // Compute z = x + y, without reducing mod p.
        var fpWordsLength = sikeParam.GetFpWords();
        var z = new FpElementOpti(sikeParam, new ulong[fpWordsLength * 2]);
        ulong carry = 0L;
        var zv = z.GetValue();
        var ov = o.GetValue();

        for (int i = 0; i < 2 * fpWordsLength; i++) {
            var addResult = UnsignedLong.Add(value[i], ov[i], ref carry);
            zv[i] = addResult;
        }
        return z;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="o"></param>
    /// <returns></returns>
    public FpElementOpti Subtract(FpElementOpti o) {
        // Compute z = x - y (mod 2*p)
        var z = new FpElementOpti(sikeParam);
        ulong borrow = 0L;
        var zv = z.GetValue();
        var ov = o.GetValue();
        var px2v = sikeParam.GetPx2().GetValue();
        var fpWordsLength = sikeParam.GetFpWords();

        // z = z - p * 2
        for (int i = 0; i < fpWordsLength; i++) {
            var result = UnsignedLong.Sub(value[i], ov[i], ref borrow);
            zv[i] = result;
        }

        // if z < 0, add p * 2 back
        ulong mask = borrow == 0 ? 0 : ulong.MaxValue;
        borrow = 0L;
        for (int i = 0; i < fpWordsLength; i++) {
            var result = UnsignedLong.Add(zv[i], px2v[i] & mask, ref borrow);
            zv[i] = result;
        }
        return z;
    }

    /**
     * Subtract two elements without reduction.
     * @param o Other element.
     * @return Calculation result.
     */
    public FpElementOpti SubtractNoReduction(FpElementOpti o) {
        // Compute z = x - y, without reducing mod p
        var fpWordsLength = sikeParam.GetFpWords();
        var z = new FpElementOpti(sikeParam, new ulong[fpWordsLength * 2]);
        ulong borrow = 0L;
        var zv = z.GetValue();
        var ov = o.GetValue();
        var pv = sikeParam.GetP().GetValue();

        for (int i = 0; i < fpWordsLength * 2; i++) {
            var result = UnsignedLong.Sub(value[i], ov[i], ref borrow);
            zv[i] = result;
        }
        ulong mask = borrow == 0 ? 0 : ulong.MaxValue;
        borrow = 0L;
        for (int i = fpWordsLength; i < fpWordsLength * 2; i++) {
            var result = UnsignedLong.Add(zv[i], pv[i - fpWordsLength] & mask, ref borrow);
            zv[i] = result;
        }
        return z;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="o"></param>
    /// <returns></returns>
    public FpElementOpti Multiply(FpElementOpti o) {
        // Compute z = x * y
        var fpWordsLength = sikeParam.GetFpWords();
        var z = new FpElementOpti(sikeParam, new ulong[fpWordsLength * 2]);
        ulong carry;
        ulong t = 0L;
        ulong u = 0L;
        ulong v = 0L;
        ulong carry2;
        var zv = z.GetValue();
        var ov = o.GetValue();

        for (int i = 0; i < fpWordsLength; i++) {
            for (int j = 0; j <= i; j++) {
                var mulResult = UnsignedLong.Mul(value[j], ov[i - j], out carry);
                carry2 = 0L;
                var addResult1 = UnsignedLong.Add(carry, v, ref carry2);
                v = addResult1;
                carry = carry2;
                u = UnsignedLong.Add(mulResult, u, ref carry);
                t += carry;
            }
            zv[i] = v;
            v = u;
            u = t;
            t = 0L;
        }

        for (int i = fpWordsLength; i < (2 * fpWordsLength) - 1; i++) {
            for (int j = i - fpWordsLength + 1; j < fpWordsLength; j++) {
                ulong mulResult = UnsignedLong.Mul(value[j], ov[i - j], out carry);
                carry2 = 0L;
                ulong addResult1 = UnsignedLong.Add(carry, v, ref carry2);
                v = addResult1;
                carry = carry2;
                u = UnsignedLong.Add(mulResult, u, ref carry);
                t += carry;
            }
            zv[i] = v;
            v = u;
            u = t;
            t = 0L;
        }
        zv[2 * fpWordsLength - 1] = v;
        return z;
    }

    /**
     * Montgomery multiplication. Input values must be already in Montgomery domain.
     * @param o Other value.
     * @return Multiplication result.
     */
    public FpElementOpti MultiplyMontgomery(FpElementOpti o) {
        var ab = Multiply(o);
        return ab.ReduceMontgomery();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public FpElementOpti Square() {
        return Multiply(this);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public FpElementOpti Inverse() {
        throw new Exception("Not implemented yet");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public FpElementOpti Negate() {
        return sikeParam.GetP().Subtract(this);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public bool IsZero() {
        var zero = new FpElementOpti(sikeParam, new ulong[value.Length]);
        return Equals(zero);
    }

    /**
     * Reduce a field element in [0, 2*p) to one in [0,p).
     */
    public void Reduce() {
        ulong borrow = 0L;
        var fpWordsLength = sikeParam.GetFpWords();
        var p = sikeParam.GetP().GetValue();

        for (int i = 0; i < fpWordsLength; i++) {
            var result = UnsignedLong.Sub(value[i], p[i], ref borrow);
            value[i] = result;
        }

        // Sets all bits if borrow = 1
        ulong mask = borrow == 1 ? ulong.MaxValue : 0;
        borrow = 0L;
        for (int i = 0; i < fpWordsLength; i++) {
            var result = UnsignedLong.Add(value[i], p[i] & mask, ref borrow);
            value[i] = result;
        }
    }

    /**
     * Perform Montgomery reduction.
     * @return Reduced value.
     */
    public FpElementOpti ReduceMontgomery()
    {
        var fpWordsLength = sikeParam.GetFpWords();
        var z = new FpElementOpti(sikeParam, new ulong[fpWordsLength]);
        ulong carry;
        ulong t = 0L;
        ulong u = 0L;
        ulong v = 0L;
        int count = sikeParam.GetZeroWords(); // number of 0 digits in the least significant part of p + 1
        ulong addResult1;
        ulong addResult2;
        ulong lo;
        var zv = z.GetValue();
        var p1v = sikeParam.GetP1().GetValue();

        for (int i = 0; i < fpWordsLength; i++) {
            for (int j = 0; j < i; j++) {
                if (j < i - count + 1) {
                    ulong mulResult = UnsignedLong.Mul(zv[j], p1v[i - j], out lo);
                    carry = 0;
                    addResult1 = UnsignedLong.Add(lo, v, ref carry);
                    v = addResult1;
                    addResult2 = UnsignedLong.Add(mulResult, u, ref carry);
                    u = addResult2;
                    t = t + carry;
                }
            }
            carry = 0;
            addResult1 = UnsignedLong.Add(v, value[i], ref carry);
            v = addResult1;
            addResult2 = UnsignedLong.Add(u, 0L, ref carry);
            u = addResult2;
            t = t + carry;
            zv[i] = v;
            v = u;
            u = t;
            t = 0L;
        }

        for (int i = fpWordsLength; i < (2 * fpWordsLength) - 1; i++) {
            if (count > 0) {
                count--;
            }
            for (int j = i - fpWordsLength + 1; j < fpWordsLength; j++) {
                if (j < (fpWordsLength - count)) {
                    ulong mulResult = UnsignedLong.Mul(zv[j], p1v[i - j], out lo);
                    carry = 0;
                    addResult1 = UnsignedLong.Add(lo, v, ref carry);
                    v = addResult1;
                    addResult2 = UnsignedLong.Add(mulResult, u, ref carry);
                    u = addResult2;
                    t = t + carry;
                }
            }
            carry = 0;
            addResult1 = UnsignedLong.Add(v, value[i], ref carry);
            v = addResult1;
            addResult2 = UnsignedLong.Add(u, 0L, ref carry);
            u = addResult2;
            t = t + carry;
            zv[i - fpWordsLength] = v;
            v = u;
            u = t;
            t = 0L;
        }

        carry = 0;
        var addResult = UnsignedLong.Add(v, value[2 * fpWordsLength - 1], ref carry);
        zv[fpWordsLength - 1] = addResult;
        return z;
    }

    /**
     * Swap field elements conditionally, in constant time.
     * @param x First element.
     * @param y Second element.
     * @param mask Swap condition, if zero swap is not performed.
     */
    public static void ConditionalSwap(FpElementOpti x, FpElementOpti y, ulong mask) {
        ulong maskNeg = mask == 0 ? 0 : ulong.MaxValue;
        var xv = x.GetValue();
        var yv = y.GetValue();

        for (int i = 0; i < xv.Length; i++) {
            ulong tmp = maskNeg & (xv[i] ^ yv[i]);
            xv[i] = tmp ^ xv[i];
            yv[i] = tmp ^ yv[i];
        }
    }

    /**
     * Create copy of the element.
     * @return Element copy.
     */
    public FpElementOpti Copy() {
        var clone = new ulong[value.Length];
        Array.Copy(value, 0, clone, 0, value.Length);
        return new FpElementOpti(sikeParam, clone);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public byte[] GetEncoded() {
        int primeSize = (sikeParam.GetPrime().BitLength + 7) / 8;
        byte[] bytes = new byte[primeSize];
        // Convert element from Montgomery domain
        ulong[] val = new ulong[sikeParam.GetFpWords() * 2];
        Array.Copy(value, 0, val, 0, sikeParam.GetFpWords());
        FpElementOpti el = new FpElementOpti(sikeParam, val);
        FpElementOpti a = el.ReduceMontgomery();
        Reduce();
        for (int i = 0; i < primeSize; i++) {
            int j = i / 8;
            int k = i % 8;
            bytes[i] = (byte) (a.GetValue()[j] >> (8 * k));
        }
        return bytes;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public string ToOctetString() {
        int primeSize = (sikeParam.GetPrime().BitLength + 7) / 8;
        BigInteger x = ByteEncoding.FromByteArray(GetEncoded());
        return OctetEncoding.ToOctetString(x, primeSize);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="that"></param>
    /// <returns></returns>
    public bool Equals(FpElementOpti that) {
        // Use constant time comparison to avoid timing attacks
        return SideChannelUtil.ConstantTimeAreEqual(GetEncoded(), that.GetEncoded());
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public override string ToString() {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < value.Length; i++) {
            sb.Append(value[i]);
            if (i < value.Length - 1) {
                sb.Append(" ");
            }
        }
        return sb.ToString();
    }
}
