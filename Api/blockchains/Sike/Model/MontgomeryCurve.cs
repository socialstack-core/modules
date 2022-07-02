using System;

namespace Lumity.SikeIsogeny;

/**
 * Montgomery curve parameters.
 *
 * @author Roman Strobl, roman.strobl@wultra.com
 */
public class MontgomeryCurve {

    private SikeParam sikeParam;
    private Fp2ElementRef a;
    private Fp2ElementRef b;

    /**
     * Montgomery curve constructor.
     * @param sikeParam SIKE parameters.
     */
    public MontgomeryCurve(SikeParam sikeParam) {
        this.sikeParam = sikeParam;
    }

    /**
     * Montgomery curve constructor.
     * @param sikeParam SIKE parameters.
     * @param a Montgomery curve coefficient a.
     */
    public MontgomeryCurve(SikeParam sikeParam, Fp2ElementRef a) {
        this.sikeParam = sikeParam;
        this.a = a;
    }

    /**
     * Montgomery curve constructor.
     * @param sikeParam SIKE parameters.
     * @param a Montgomery curve coefficient a.
     * @param b Montgomery curve coefficient b.
     */
    public MontgomeryCurve(SikeParam sikeParam, Fp2ElementRef a, Fp2ElementRef b) : this(sikeParam, a) {
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
    public Fp2ElementRef GetA() {
        return a;
    }

    /**
     * Set Montgomery curve coefficient a.
     * @param a Montgomery curve coefficient a.
     */
    public void SetA(Fp2ElementRef a) {
        this.a = a;
    }

    /**
     * Get Montgomery curve coefficient b.
     * @return Montgomery curve coefficient b.
     */
    public Fp2ElementRef GetB() {
        return b;
    }

    /**
     * Set Montgomery curve coefficient b.
     * @param b Montgomery curve coefficient b.
     */
    public void SetB(Fp2ElementRef b) {
        this.b = b;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public override string ToString() {
        return "a = " + a + ", b = " + b;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="that"></param>
    /// <returns></returns>
    public bool Equals(MontgomeryCurve that) {
        if (this == that) return true;
        return sikeParam.Equals(that.sikeParam) &&
                a.Equals(that.a) &&
                b.Equals(that.b);
    }
}
