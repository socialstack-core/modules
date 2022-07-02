using Org.BouncyCastle.Math;

namespace Lumity.SikeIsogeny;

/**
 * Factory for reference elements of quadratic extension field F(p^2).
 *
 * @author Roman Strobl, roman.strobl@wultra.com
 */
public class Fp2ElementFactoryRef {

    private SikeParam sikeParam;

    /**
     * Fp2Element factory constructor for reference elements.
     * @param sikeParam SIKE parameters.
     */
    public Fp2ElementFactoryRef(SikeParam sikeParam) {
        this.sikeParam = sikeParam;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public Fp2ElementRef Zero() {
        return new Fp2ElementRef(sikeParam, BigInteger.Zero, BigInteger.Zero);
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public Fp2ElementRef One() {
        return new Fp2ElementRef(sikeParam, BigInteger.One, BigInteger.Zero);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="x0r"></param>
    /// <returns></returns>
    public Fp2ElementRef Generate(BigInteger x0r) {
        return new Fp2ElementRef(sikeParam, x0r, BigInteger.Zero);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="x0r"></param>
    /// <param name="x0i"></param>
    /// <returns></returns>
    public Fp2ElementRef Generate(BigInteger x0r, BigInteger x0i) {
        return new Fp2ElementRef(sikeParam, x0r, x0i);
    }
}
