using Org.BouncyCastle.Math;
using System;

namespace Lumity.SikeIsogeny;

/**
 * Element of a quadratic extension field F(p^2): x0 + x1*i.
 *
 * @author Roman Strobl, roman.strobl@wultra.com
 */
public class Fp2ElementRef {

    private FpElementRef x0;
    private FpElementRef x1;

    private SikeParam sikeParam;

    /**
     * The F(p^2) field element constructor for given F(p) elements.
     * @param sikeParam SIKE parameters.
     * @param x0 The x0 real F(p) element.
     * @param x1 The x1 imaginary F(p) element.
     */
    public Fp2ElementRef(SikeParam sikeParam, FpElementRef x0, FpElementRef x1) {
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
    public Fp2ElementRef(SikeParam sikeParam, BigInteger x0b, BigInteger x1b) {
        this.sikeParam = sikeParam;
        this.x0 = new FpElementRef(sikeParam, x0b);
        this.x1 = new FpElementRef(sikeParam, x1b);
    }

    /**
     * Get the real part of element.
     * @return Real part of element.
     */
    public FpElementRef GetX0() {
        return x0;
    }

    /**
     * Get the imaginary part of element.
     * @return Imaginary part of element.
     */
    public FpElementRef GetX1() {
        return x1;
    }

    /**
     * Add two elements.
     * @param y Other element.
     * @return Calculation result.
     */
    public Fp2ElementRef Add(Fp2ElementRef y) {
        // y = (x0 + i*x1) + (y0 + i*y1) = x0 + y0 + i*(x1 + y1)
        FpElementRef r, i;

        r = x0.Add(y.GetX0());
        i = x1.Add(y.GetX1());
        return new Fp2ElementRef(sikeParam, r, i);
    }

    /**
     * Subtract two elements.
     * @param y Other element.
     * @return Calculation result.
     */
    public Fp2ElementRef Subtract(Fp2ElementRef y) {
        // y = (x0 + i*x1) - (y0 + i*y1) = x0 - y0 + i*(x1 - y1)
        FpElementRef r, i;

        r = x0.Subtract(y.GetX0());
        i = x1.Subtract(y.GetX1());
        return new Fp2ElementRef(sikeParam, r, i);
    }

    /**
     * Multiply two elements.
     * @param y Other element.
     * @return Calculation result.
     */
    public Fp2ElementRef Multiply(Fp2ElementRef y) {
        // y = (x0 + i*x1) * (y0 + i*y1) = x0y0 - x1y1 + i*(x0y1 + x1y0)
        FpElementRef r1, r2, r, i1, i2, i;

        r1 = x0.Multiply(y.GetX0());
        r2 = x1.Multiply(y.GetX1());
        r = r1.Subtract(r2);

        i1 = x0.Multiply(y.GetX1());
        i2 = x1.Multiply(y.GetX0());
        i = i1.Add(i2);

        return new Fp2ElementRef(sikeParam, r, i);
    }

    /**
     * Multiply by the imaginary part of the element.
     * @return Calculation result.
     */
    public Fp2ElementRef MultiplyByI() {
        return new Fp2ElementRef(sikeParam, x1.Negate(), x0.Copy());
    }

    /**
     * Square the element.
     * @return Calculation result.
     */
    public Fp2ElementRef Square() {
        return Multiply(this);
    }

    /**
     * Element exponentiation.
     * @param n Exponent
     * @return Calculation result.
     */
    public Fp2ElementRef Pow(BigInteger n) {
        if (n.CompareTo(BigInteger.Zero) < 0) {
            throw new Exception("Negative exponent");
        }
        if (n.CompareTo(BigInteger.Zero) == 0) {
            return sikeParam.GetFp2ElementFactory().One();
        }
        if (n.CompareTo(BigInteger.One) == 0) {
            return Copy();
        }
        BigInteger e = n;
        Fp2ElementRef baseVal = Copy();
        Fp2ElementRef result = sikeParam.GetFp2ElementFactory().One();
        while (e.CompareTo(BigInteger.Zero) > 0) {
            if (e.TestBit(0)) {
                result = result.Multiply(baseVal);
            }
            e = e.ShiftRight(1);
            baseVal = baseVal.Square();
        }
        return result;
    }

    /**
     * Calculate the square root of the element.
     * @return Calculation result.
     */
    public Fp2ElementRef Sqrt() {
        // TODO - compare performance with reference C implementation, consider replacing algorithm
        if (IsZero()) {
            return sikeParam.GetFp2ElementFactory().Zero();
        }
        if (!IsQuadraticResidue()) {
            throw new Exception("The square root of a quadratic non-residue cannot be computed");
        }
        BigInteger prime = sikeParam.GetPrime();
        if (prime.Mod(SideChannelUtil.BigIntegerFour).CompareTo(SideChannelUtil.BigIntegerThree) != 0) {
            throw new Exception("Field prime mod 4 is not 3");
        }
        Fp2ElementRef a1, a2;
        Fp2ElementRef neg1 = sikeParam.GetFp2ElementFactory().One();
        BigInteger p = prime;
        p = p.ShiftRight(2);
        a1 = Copy();
        a1 = a1.Pow(p);
        a2 = Copy();
        a2 = a2.Multiply(a1);
        a1 = a1.Multiply(a2);
        if (a1.Equals(neg1)) {
            return a2.MultiplyByI();
        }
        p = prime;
        p = p.ShiftRight(1);
        a1 = a1.Add(sikeParam.GetFp2ElementFactory().One());
        a1 = a1.Pow(p);
        return a1.Multiply(a2);
    }

    /**
     * Get whether the element is a quadratic residue modulo prime.
     * @return Whether the element is a quadratic residue.
     */
    public bool IsQuadraticResidue() {
        Fp2ElementRef baseVal = Copy();
        BigInteger p = sikeParam.GetPrime();
        p = p.Multiply(p);
        p = p.Subtract(BigInteger.One);
        p = p.ShiftRight(1);
        baseVal = baseVal.Pow(p);
        return baseVal.Equals(sikeParam.GetFp2ElementFactory().One());
    }

    /**
     * Invert the element.
     * @return Calculation result.
     */
    public Fp2ElementRef Inverse() {
        FpElementRef t0, t1, o0, o1;
        t0 = x0.Square();
        t1 = x1.Square();
        t0 = t0.Add(t1);
        t0 = t0.Inverse();
        o1 = x1.Negate();
        o0 = x0.Multiply(t0);
        o1 = o1.Multiply(t0);
        return new Fp2ElementRef(sikeParam, o0, o1);
    }

    /**
     * Negate the element.
     * @return Calculation result.
     */
    public Fp2ElementRef Negate() {
        return new Fp2ElementRef(sikeParam, x0.Negate(), x1.Negate());
    }

    /**
     * Get whether the element is the zero element.
     * @return Whether the element is the zero element.
     */
    public bool IsZero() {
        return x0.IsZero() && x1.IsZero();
    }

    /**
     * Copy the element.
     * @return Element copy.
     */
    public Fp2ElementRef Copy() {
        return new Fp2ElementRef(sikeParam, new FpElementRef(sikeParam, x0.GetX()), new FpElementRef(sikeParam, x1.GetX()));
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
        return x1 + "i" + " + " + x0;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="that"></param>
    /// <returns></returns>
    public bool Equals(Fp2ElementRef that) {
        if (this == that) return true;
        if (that == null) return false;
        return sikeParam.GetPrime().Equals(that.sikeParam.GetPrime())
                && x0.Equals(that.x0)
                && x1.Equals(that.x1);
    }
	
}
