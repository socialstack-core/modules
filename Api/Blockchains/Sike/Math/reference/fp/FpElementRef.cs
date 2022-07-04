using Org.BouncyCastle.Math;

namespace Lumity.SikeIsogeny;

/**
 * Element of an F(p) field with a single coordinate x.
 *
 * @author Roman Strobl, roman.strobl@wultra.com
 */
public class FpElementRef {

    private BigInteger x;

    private SikeParam sikeParam;

    /**
     * The F(p^) field element constructor for given BigInteger value.
     * @param sikeParam Field prime.
     * @param x BigInteger value.
     */
    public FpElementRef(SikeParam sikeParam, BigInteger x) {
        this.sikeParam = sikeParam;
        this.x = x.Mod(sikeParam.GetPrime());
    }

    /**
     * Get the element value.
     * @return the element value.
     */
    public BigInteger GetX() {
        return x;
    }

    /**
     * Get the field prime.
     * @return Field prime.
     */
    public BigInteger getPrime() {
        return sikeParam.GetPrime();
    }

    /**
     * Add two elements.
     * @param o Other element.
     * @return Calculation result.
     */
    public FpElementRef Add(FpElementRef o) {
        return new FpElementRef(sikeParam, x.Add(o.GetX()).Mod(getPrime()));
    }

    /**
     * Subtract two elements.
     * @param o Other element.
     * @return Calculation result.
     */
    public FpElementRef Subtract(FpElementRef o) {
        return new FpElementRef(sikeParam, x.Subtract(o.GetX()).Mod(getPrime()));
    }

    /**
     * Multiply two elements.
     * @param o Other element.
     * @return Calculation result.
     */
    public FpElementRef Multiply(FpElementRef o) {
        return new FpElementRef(sikeParam, x.Multiply(o.GetX()).Mod(getPrime()));
    }

    /**
     * Square the elements.
     * @return Calculation result.
     */
    public FpElementRef Square() {
        return Multiply(this);
    }

    /**
     * Invert the element.
     * @return Calculation result.
     */
    public FpElementRef Inverse() {
        return new FpElementRef(sikeParam, x.ModInverse(getPrime()));
    }

    /**
     * Negate the element.
     * @return Calculation result.
     */
    public FpElementRef Negate() {
        return new FpElementRef(sikeParam, getPrime().Subtract(x));
    }

    /**
     * Get whether the element is the zero element.
     * @return Whether the element is the zero element.
     */
    public bool IsZero() {
        return BigInteger.Zero.Equals(x);
    }

    /**
     * Copy the element.
     * @return Element copy.
     */
    public FpElementRef Copy() {
        return new FpElementRef(sikeParam, x);
    }

    /**
     * Encode the element in bytes.
     * @return Encoded element in bytes.
     */
    public byte[] GetEncoded() {
        int primeSize = (sikeParam.GetPrime().BitLength + 7) / 8;
        return ByteEncoding.ToByteArray(x, primeSize);
    }

    /**
     * Convert element to octet string.
     * @return Octet string.
     */
    public string ToOctetString() {
        int primeSize = (sikeParam.GetPrime().BitLength + 7) / 8;
        return OctetEncoding.ToOctetString(x, primeSize);
    }
	
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public override string ToString() {
        return x.ToString();
    }
	
    /// <summary>
    /// 
    /// </summary>
    /// <param name="that"></param>
    /// <returns></returns>
    public bool Equals(FpElementRef that) {
        if (this == that) return true;
        if (that == null) return false;
        return getPrime().Equals(that.getPrime())
                && x.Equals(that.x);
    }
	
}
