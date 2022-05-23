using Org.BouncyCastle.Math;
using System;

namespace Lumity.SikeIsogeny;

/**
 * Element of a quadratic extension field F(p^2): x0 + x1*i.
 *
 * @author Roman Strobl, roman.strobl@wultra.com
 */
public struct Fp2ElementOpti {

    private FpElementOpti x0;
    private FpElementOpti x1;

    private SikeParam sikeParam;

    /**
     * The F(p^2) field element constructor for given F(p) elements.
     * @param sikeParam SIKE parameters.
     * @param x0 The x0 real F(p) element.
     * @param x1 The x1 imaginary F(p) element.
     */
    public Fp2ElementOpti(SikeParam sikeParam, FpElementOpti x0, FpElementOpti x1) {
        this.sikeParam = sikeParam;
        this.x0 = x0.Copy();
        this.x1 = x1.Copy();
    }

    /**
     * The F(p^2) field element constructor for given BigInteger values.
     * @param sikeParam SIKE parameters.
     * @param x0b The x0 real F(p) element.
     * @param x1b The x1 imaginary F(p) element.
     */
    public Fp2ElementOpti(SikeParam sikeParam, BigInteger x0b, BigInteger x1b) {
        this.sikeParam = sikeParam;
        this.x0 = new FpElementOpti(sikeParam, x0b);
        this.x1 = new FpElementOpti(sikeParam, x1b);
    }

    /**
     * Get the real part of element.
     * @return Real part of element.
     */
    public FpElementOpti GetX0() {
        return x0;
    }

    /**
     * Get the imaginary part of element.
     * @return Imaginary part of element.
     */
    public FpElementOpti GetX1() {
        return x1;
    }

    /**
     * Add two elements.
     * @param y Other element.
     * @return Calculation result.
     */
    public Fp2ElementOpti Add(Fp2ElementOpti y) {
        // y = (x0 + i*x1) + (y0 + i*y1) = x0 + y0 + i*(x1 + y1)
        FpElementOpti r, i;

        r = x0.Add(y.GetX0());
        i = x1.Add(y.GetX1());
        return new Fp2ElementOpti(sikeParam, r, i);
    }

    /**
     * Subtract two elements.
     * @param y Other element.
     * @return Calculation result.
     */
    public Fp2ElementOpti Subtract(Fp2ElementOpti y) {
        // y = (x0 + i*x1) - (y0 + i*y1) = x0 - y0 + i*(x1 - y1)
        FpElementOpti r, i;

        r = x0.Subtract(y.GetX0());
        i = x1.Subtract(y.GetX1());
        return new Fp2ElementOpti(sikeParam, r, i);
    }

    /**
     * Multiply two elements.
     * @param y Other element.
     * @return Calculation result.
     */
    public Fp2ElementOpti Multiply(Fp2ElementOpti y) {
        var a = x0;
        var b = x1;
        var c = y.GetX0();
        var d = y.GetX1();

        // (a + bi) * (c + di) = (a * c - b * d) + (a * d + b * c)i

        var ac = a.Multiply(c);
        var bd = b.Multiply(d);

        var bMinusA = b.Subtract(a);
        var cMinusD = c.Subtract(d);

        var adPlusBC = bMinusA.Multiply(cMinusD);
        adPlusBC = adPlusBC.AddNoReduction(ac);
        adPlusBC = adPlusBC.AddNoReduction(bd);

        // x1 = (a * d + b * c) * R mod p
        var x1o = adPlusBC.ReduceMontgomery();

        var acMinusBd = ac.SubtractNoReduction(bd);
        var x0o = acMinusBd.ReduceMontgomery();

        // x0 = (a * c - b * d) * R mod p
        return new Fp2ElementOpti(sikeParam, x0o, x1o);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public Fp2ElementOpti MultiplyByI() {
        return new Fp2ElementOpti(sikeParam, x1.Negate(), x0.Copy());
    }

    /**
     * Square the element.
     * @return Calculation result.
     */
    public Fp2ElementOpti Square() {
        var a = x0;
        var b = x1;

        // (a + bi) * (a + bi) = (a^2 - b^2) + (2ab)i.
        var a2 = a.Add(a);
        var aPlusB = a.Add(b);
        var aMinusB = a.Subtract(b);
        var a2MinB2 = aPlusB.Multiply(aMinusB);
        var ab2 = a2.Multiply(b);

        // (a^2 - b^2) * R mod p
        var x0o = a2MinB2.ReduceMontgomery();

        // 2 * a * b * R mod p
        var x1o = ab2.ReduceMontgomery();

        return new Fp2ElementOpti(sikeParam, x0o, x1o);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="n"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public Fp2ElementOpti Pow(BigInteger n) {
        throw new Exception("Not implemented yet");
    }

    /**
     * Calculate the square root of the element.
     * @return Calculation result.
     */
    public Fp2ElementOpti Sqrt() {
        throw new Exception("Not implemented yet");
    }

    /**
     * Invert the element.
     * @return Calculation result.
     */
    public Fp2ElementOpti Inverse() {
        var e1 = x0.Multiply(x0);
        var e2 = x1.Multiply(x1);
        e1 = e1.AddNoReduction(e2);
        // (a^2 + b^2) * R mod p
        var f1 = e1.ReduceMontgomery();

        var f2 = f1.MultiplyMontgomery(f1);
        f2 = P34(f2);
        f2 = f2.MultiplyMontgomery(f2);
        f2 = f2.MultiplyMontgomery(f1);

        e1 = x0.Multiply(f2);
        var x0o = e1.ReduceMontgomery();

        f1 = new FpElementOpti(sikeParam).Subtract(x1);
        e1 = f1.Multiply(f2);
        var x1o = e1.ReduceMontgomery();

        return new Fp2ElementOpti(sikeParam, x0o, x1o);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public Fp2ElementOpti Negate() {
        return new Fp2ElementOpti(sikeParam, x1.Negate(), x0.Copy());
    }

    /**
     * Compute x ^ ((p - 3) / 4).
     * @param x Value x.
     * @return Computed value.
     */
    private FpElementOpti P34(FpElementOpti x) {
        FpElementOpti[] lookup = new FpElementOpti[16];
        int[] powStrategy = sikeParam.GetPowStrategy();
        int[] mulStrategy = sikeParam.GetMulStrategy();
        int initialMul = sikeParam.GetInitialMul();
        var xSquare = x.MultiplyMontgomery(x);
        lookup[0] = x.Copy();
        for (int i = 1; i < 16; i++) {
            lookup[i] = lookup[i - 1].MultiplyMontgomery(xSquare);
        }
        var dest = lookup[initialMul];
        for (int i = 0; i < powStrategy.Length; i++) {
            dest = dest.MultiplyMontgomery(dest);
            for (int j = 1; j < powStrategy[i]; j++) {
                dest = dest.MultiplyMontgomery(dest);
            }
            dest = dest.MultiplyMontgomery(lookup[mulStrategy[i]]);
        }
        return dest;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public bool IsZero() {
        return x0.IsZero() && x1.IsZero();
    }

    /**
     * Copy the element.
     * @return Element copy.
     */
    public Fp2ElementOpti Copy() {
        return new Fp2ElementOpti(sikeParam, x0.Copy(), x1.Copy());
    }

    /**
     * Encode the element in bytes.
     * @return Encoded element in bytes.
     */
    public byte[] GetEncoded() {
        byte[] x0Encoded = x0.GetEncoded();
        byte[] x1Encoded = x1.GetEncoded();
        byte[] encoded = new byte[x0Encoded.Length + x1Encoded.Length];
        Array.Copy(x0Encoded, 0, encoded, 0, x0Encoded.Length);
        Array.Copy(x1Encoded, 0, encoded, x0Encoded.Length, x1Encoded.Length);
        return encoded;
    }

    /**
     * Convert element to octet string.
     * @return Octet string.
     */
    public string ToOctetString() {
        return x0.ToOctetString() + x1.ToOctetString();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public override string ToString() {
        return x1.GetX() + "i" + " + " + x0.GetX();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="that"></param>
    /// <returns></returns>
    public bool Equals(Fp2ElementOpti that)
    {
        // Use constant time comparison to avoid timing attacks
        return sikeParam.Equals(that.sikeParam)
                && SideChannelUtil.ConstantTimeAreEqual(GetEncoded(), that.GetEncoded());
    }
}
