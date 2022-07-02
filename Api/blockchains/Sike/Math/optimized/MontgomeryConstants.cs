namespace Lumity.SikeIsogeny;

/**
 * Montgomery curve constants for optimization in projective coordinates.
 *
 * @author Roman Strobl, roman.strobl@wultra.com
 */
public class MontgomeryConstants {

    private Fp2ElementOpti c;
    private Fp2ElementOpti a24plus;
    private Fp2ElementOpti a24minus;
    private Fp2ElementOpti c24;
    private Fp2ElementOpti k1;
    private Fp2ElementOpti k2;
    private Fp2ElementOpti k3;

    /**
     * Default optimized montgomery constants constructor.
     * @param sikeParam SIKE parameters.
     */
    public MontgomeryConstants(SikeParam sikeParam) {
        c = sikeParam.GetFp2ElementFactoryOpti().One();
    }

    /**
     * Optimized montgomery constants constructor with calculation of constants.
     * @param sikeParam SIKE parameters.
     * @param a Montgomery curve coefficient a.
     */
    public MontgomeryConstants(SikeParam sikeParam, Fp2ElementOpti a) : this(sikeParam) {
        Fp2ElementOpti t1 = c.Add(c);
        this.a24plus = a.Add(t1);
        this.a24minus = a.Subtract(t1);
        this.c24 = t1.Add(t1);
    }

    /**
     * Get Montgomery curve constant c.
     * @return Montgomery constant c.
     */
    public Fp2ElementOpti GetC() {
        return c;
    }

    /**
     * Set Montgomery curve constant c.
     * @param c Montgomery curve constant c.
     */
    public void SetC(Fp2ElementOpti c) {
        this.c = c;
    }

    /**
     * Get Montgomery curve constant a24+.
     * @return Montgomery curve constant a24+.
     */
    public Fp2ElementOpti GetA24plus() {
        return a24plus;
    }

    /**
     * Set Montgomery curve constant a24+.
     * @param a24plus Montgomery curve constant a24+.
     */
    public void SetA24plus(Fp2ElementOpti a24plus) {
        this.a24plus = a24plus;
    }

    /**
     * Get Montgomery curve constant a24-.
     * @return Montgomery curve constant a24-.
     */
    public Fp2ElementOpti GetA24minus() {
        return a24minus;
    }

    /**
     * Set Montgomery curve constant a24-.
     * @param a24minus Montgomery curve constant a24-.
     */
    public void SetA24minus(Fp2ElementOpti a24minus) {
        this.a24minus = a24minus;
    }

    /**
     * Get Montgomery curve constant c24.
     * @return Montgomery curve constant c24.
     */
    public Fp2ElementOpti GetC24() {
        return c24;
    }

    /**
     * Set Montgomery curve constant c24.
     * @param c24 Montgomery curve constant c24.
     */
    public void SetC24(Fp2ElementOpti c24) {
        this.c24 = c24;
    }

    /**
     * Get Montgomery curve constant K1.
     * @return Montgomery curve constant K1.
     */
    public Fp2ElementOpti GetK1() {
        return k1;
    }

    /**
     * Set Montgomery curve constant K2.
     * @param k1 Montgomery curve constant K2.
     */
    public void SetK1(Fp2ElementOpti k1) {
        this.k1 = k1;
    }

    /**
     * Get Montgomery curve constant K2.
     * @return Montgomery curve constant K2.
     */
    public Fp2ElementOpti GetK2() {
        return k2;
    }

    /**
     * Set Montgomery curve constant K2.
     * @param k2 Montgomery curve constant K2.
     */
    public void SetK2(Fp2ElementOpti k2) {
        this.k2 = k2;
    }

    /**
     * Get Montgomery curve constant K3.
     * @return Montgomery curve constant K3.
     */
    public Fp2ElementOpti GetK3() {
        return k3;
    }

    /**
     * Set Montgomery curve constant K3.
     * @param k3 Montgomery curve constant K3.
     */
    public void SetK3(Fp2ElementOpti k3) {
        this.k3 = k3;
    }
	
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public override string ToString() {
        return "MontgomeryConstants{" +
                "c=" + c +
                ", a24plus=" + a24plus +
                ", a24minus=" + a24minus +
                ", c24=" + c24 +
                ", k1=" + k1 +
                ", k2=" + k2 +
                ", k3=" + k3 +
                '}';
    }
}
