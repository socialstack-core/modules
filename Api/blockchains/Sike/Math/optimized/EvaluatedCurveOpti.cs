namespace Lumity.SikeIsogeny;

/**
 * Evaluated curve and optional points.
 *
 * @author Roman Strobl, roman.strobl@wultra.com
 */
public class EvaluatedCurveOpti {

    private MontgomeryCurveOpti curve;
    private Fp2PointProjective p;
    private Fp2PointProjective q;
    private Fp2PointProjective r;

    /**
     * Curve constructor.
     * @param curve Evaluated curve.
     * @param p Optional point P.
     * @param q Optional point Q.
     */
    public EvaluatedCurveOpti(MontgomeryCurveOpti curve, Fp2PointProjective p, Fp2PointProjective q) {
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
    public EvaluatedCurveOpti(MontgomeryCurveOpti curve, Fp2PointProjective p, Fp2PointProjective q, Fp2PointProjective r) {
        this.curve = curve;
        this.p = p;
        this.q = q;
        this.r = r;
    }

    /**
     * Get the evaluated curve.
     * @return Evaluated curve.
     */
    public MontgomeryCurveOpti GetCurve() {
        return curve;
    }

    /**
     * Get optional point P.
     * @return Optional point P.
     */
    public Fp2PointProjective GetP() {
        return p;
    }

    /**
     * Get optional point Q.
     * @return Optional point Q.
     */
    public Fp2PointProjective GetQ() {
        return q;
    }

    /**
     * Get optional point R.
     * @return Optional point R.
     */
    public Fp2PointProjective GetR() {
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
    public bool Equals(EvaluatedCurveOpti that) {
        if (this == that) return true;
        // Use & to avoid timing attacks
        return curve.Equals(that.curve)
                & p.Equals(that.p)
                & q.Equals(that.q)
                & r.Equals(that.r);
    }
}
