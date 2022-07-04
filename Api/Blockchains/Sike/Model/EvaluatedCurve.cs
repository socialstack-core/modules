namespace Lumity.SikeIsogeny;

/**
 * Evaluated curve and optional points.
 *
 * @author Roman Strobl, roman.strobl@wultra.com
 */
public class EvaluatedCurve {

    private MontgomeryCurve curve;
    private Fp2PointAffine p;
    private Fp2PointAffine q;
    private Fp2PointAffine r;

    /**
     * Curve constructor.
     * @param curve Evaluated curve.
     * @param p Optional point P.
     * @param q Optional point Q.
     */
    public EvaluatedCurve(MontgomeryCurve curve, Fp2PointAffine p, Fp2PointAffine q) {
        this.curve = curve;
        this.p = p;
        this.q = q;
        this.r = null;
    }

    /**
     * Curve constructor.
     * @param curve Evaluated curve.
     * @param p Optional point P.
     * @param q Optional point Q.
     * @param r Optional point R.
     */
    public EvaluatedCurve(MontgomeryCurve curve, Fp2PointAffine p, Fp2PointAffine q, Fp2PointAffine r) {
        this.curve = curve;
        this.p = p;
        this.q = q;
        this.r = r;
    }

    /**
     * Get the evaluated curve.
     * @return Evaluated curve.
     */
    public MontgomeryCurve GetCurve() {
        return curve;
    }

    /**
     * Get optional point P.
     * @return Optional point P.
     */
    public Fp2PointAffine GetP() {
        return p;
    }

    /**
     * Get optional point Q.
     * @return Optional point Q.
     */
    public Fp2PointAffine GetQ() {
        return q;
    }

    /**
     * Get optional point R.
     * @return Optional point R.
     */
    public Fp2PointAffine GetR() {
        return r;
    }
	
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public override string ToString() {
        return curve.ToString();
    }
	
    /// <summary>
    /// 
    /// </summary>
    /// <param name="that"></param>
    /// <returns></returns>
    public bool Equals(EvaluatedCurve that) {
        if (this == that) return true;
        // Use & to avoid timing attacks
        return curve.Equals(that.curve)
                & p.Equals(that.p)
                & q.Equals(that.q)
                & r.Equals(that.r);
    }
}
