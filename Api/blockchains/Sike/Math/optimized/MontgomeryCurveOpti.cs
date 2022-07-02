using System;

namespace Lumity.SikeIsogeny;

/**
 * Montgomery curve parameters.
 *
 * @author Roman Strobl, roman.strobl@wultra.com
 */
public class MontgomeryCurveOpti {

    private SikeParam sikeParam;
    private Fp2ElementOpti a;
    private Fp2ElementOpti b;
    private MontgomeryConstants optimizedConstants;

    /**
     * Montgomery curve constructor.
     * @param sikeParam SIKE parameters.
     */
    public MontgomeryCurveOpti(SikeParam sikeParam) {
        this.sikeParam = sikeParam;
        optimizedConstants = new MontgomeryConstants(sikeParam);
    }

    /**
     * Montgomery curve constructor.
     * @param sikeParam SIKE parameters.
     * @param a Montgomery curve coefficient a.
     */
    public MontgomeryCurveOpti(SikeParam sikeParam, Fp2ElementOpti a) {
        this.sikeParam = sikeParam;
        this.a = a;
        if (sikeParam.GetImplementationType() == ImplementationType.OPTIMIZED) {
            optimizedConstants = new MontgomeryConstants(sikeParam, a);
        }
    }

    /**
     * Montgomery curve constructor.
     * @param sikeParam SIKE parameters.
     * @param a Montgomery curve coefficient a.
     * @param b Montgomery curve coefficient b.
     */
    public MontgomeryCurveOpti(SikeParam sikeParam, Fp2ElementOpti a, Fp2ElementOpti b) : this(sikeParam, a) {
        this.b = b;
    }

    /**
     * Get SIKE parameters.
     * @return SIKE parameters.
     */
    public SikeParam GetSikeParam() {
        return sikeParam;
    }

    /**
     * Get Montgomery curve coefficient a.
     * @return Montgomery curve coefficient a.
     */
    public Fp2ElementOpti GetA() {
        return a;
    }

    /**
     * Set Montgomery curve coefficient a.
     * @param a Montgomery curve coefficient a.
     */
    public void SetA(Fp2ElementOpti a) {
        this.a = a;
    }

    /**
     * Get Montgomery curve coefficient b.
     * @return Montgomery curve coefficient b.
     */
    public Fp2ElementOpti GetB() {
        return b;
    }

    /**
     * Set Montgomery curve coefficient b.
     * @param b Montgomery curve coefficient b.
     */
    public void SetB(Fp2ElementOpti b) {
        this.b = b;
    }

    /**
     * Get optimized montgomery constants for this curve.
     * @return Optimized montgomery constants for this curve.
     */
    public MontgomeryConstants GetOptimizedConstants() {
        return optimizedConstants;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public override string ToString() {
        if (sikeParam.GetImplementationType() == ImplementationType.OPTIMIZED) {
            return "a = " + a + ", " + optimizedConstants;
        }
        return "a = " + a + ", b = " + b;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="that"></param>
    /// <returns></returns>
    public bool Equals(MontgomeryCurveOpti that) {
        if (this == that) return true;
        return sikeParam.Equals(that.sikeParam) &&
                a.Equals(that.a) &&
                b.Equals(that.b) &&
                optimizedConstants == that.optimizedConstants;
    }
}
