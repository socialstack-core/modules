using Org.BouncyCastle.Math;

namespace Lumity.SikeIsogeny;

/**
 * Reference elliptic curve mathematics on Montgomery curves with affine coordinates.
 *
 * @author Roman Strobl, roman.strobl@wultra.com
 */
public class MontgomeryAffine {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="curve"></param>
    /// <param name="p"></param>
    /// <returns></returns>
    public Fp2PointAffine XDbl(MontgomeryCurve curve, Fp2PointAffine p) {
        if (p.IsInfinite()) {
            return p;
        }

        Fp2ElementRef t0, t1, t2, x2p, y2p;
        Fp2ElementRef b = curve.GetB();
        SikeParam sikeParam = curve.GetSikeParam();

        t0 = p.GetX().Square();
        t1 = t0.Add(t0);
        t2 = sikeParam.GetFp2ElementFactory().One();
        t0 = t0.Add(t1);
        t1 = curve.GetA().Multiply(p.GetX());
        t1 = t1.Add(t1);
        t0 = t0.Add(t1);
        t0 = t0.Add(t2);
        t1 = b.Multiply(p.GetY());
        t1 = t1.Add(t1);
        t1 = t1.Inverse();
        t0 = t0.Multiply(t1);
        t1 = t0.Square();
        t2 = b.Multiply(t1);
        t2 = t2.Subtract(curve.GetA());
        t2 = t2.Subtract(p.GetX());
        t2 = t2.Subtract(p.GetX());
        t1 = t0.Multiply(t1);
        t1 = b.Multiply(t1);
        t1 = t1.Add(p.GetY());
        y2p = p.GetX().Add(p.GetX());
        y2p = y2p.Add(p.GetX());
        y2p = y2p.Add(curve.GetA());
        y2p = y2p.Multiply(t0);
        y2p = y2p.Subtract(t1);
        x2p = t2;
        return new Fp2PointAffine(x2p, y2p);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="curve"></param>
    /// <param name="p"></param>
    /// <returns></returns>
    public Fp2PointAffine XTpl(MontgomeryCurve curve, Fp2PointAffine p) {
        var p2 = XDbl(curve, p);
        return XAdd(curve, p, p2);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="curve"></param>
    /// <param name="p"></param>
    /// <param name="e"></param>
    /// <returns></returns>
    public Fp2PointAffine XDble(MontgomeryCurve curve, Fp2PointAffine p, int e) {
        var pAp = p;
        for (int i = 0; i < e; i++) {
            pAp = XDbl(curve, pAp);
        }
        return pAp;
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="curve"></param>
    /// <param name="p"></param>
    /// <param name="e"></param>
    /// <returns></returns>
    public Fp2PointAffine XTple(MontgomeryCurve curve, Fp2PointAffine p, int e) {
        var pAp = p;
        for (int i = 0; i < e; i++) {
            pAp = XTpl(curve, pAp);
        }
        return pAp;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="curve"></param>
    /// <returns></returns>
    public Fp2ElementRef JInv(MontgomeryCurve curve) {
        Fp2ElementRef t0, t1, j;
        var a = curve.GetA();
        SikeParam sikeParam = curve.GetSikeParam();
        t0 = a.Square();
        j = sikeParam.GetFp2ElementFactory().Generate(SideChannelUtil.BigIntegerThree);
        j = t0.Subtract(j);
        t1 = j.Square();
        j = j.Multiply(t1);
        j = j.Add(j);
        j = j.Add(j);
        j = j.Add(j);
        j = j.Add(j);
        j = j.Add(j);
        j = j.Add(j);
        j = j.Add(j);
        j = j.Add(j);
        t1 = sikeParam.GetFp2ElementFactory().Generate(SideChannelUtil.BigIntegerFour);
        t0 = t0.Subtract(t1);
        t0 = t0.Inverse();
        j = j.Multiply(t0);
        return j;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sikeParam"></param>
    /// <param name="px"></param>
    /// <param name="qx"></param>
    /// <param name="rx"></param>
    /// <returns></returns>
    public Fp2ElementRef GetA(SikeParam sikeParam, Fp2ElementRef px, Fp2ElementRef qx, Fp2ElementRef rx) {
        Fp2ElementRef t0, t1, a;
        t1 = px.Add(qx);
        t0 = px.Multiply(qx);
        a = rx.Multiply(t1);
        a = a.Add(t0);
        t0 = t0.Multiply(rx);
        a = a.Subtract(sikeParam.GetFp2ElementFactory().One());
        t0 = t0.Add(t0);
        t1 = t1.Add(rx);
        t0 = t0.Add(t0);
        a = a.Square();
        t0 = t0.Inverse();
        a = a.Multiply(t0);
        a = a.Subtract(t1);
        return a;
    }

    /**
     * Double-and-add scalar multiplication.
     * @param curve Current curve.
     * @param m Scalar value.
     * @param p Point on the curve.
     * @param bits Number of bits in field elements.
     * @return Calculated new point.
     */
    public Fp2PointAffine DoubleAndAdd(MontgomeryCurve curve, BigInteger m, Fp2PointAffine p, int bits) {
        SikeParam sikeParam = curve.GetSikeParam();
        var q = Fp2PointAffine.Infinity(sikeParam);
        for (int i = bits - 1; i >= 0; i--) {
            q = XDbl(curve, q);
            if (m.TestBit(i)) {
                q = XAdd(curve, q, p);
            }
        }
        return q;
    }

    /**
     * Adding of two points.
     * @param curve Current curve.
     * @param p First point on the curve.
     * @param q Second point on the curve.
     * @return Calculated new point.
     */
    public Fp2PointAffine XAdd(MontgomeryCurve curve, Fp2PointAffine p, Fp2PointAffine q) {
        if (p.IsInfinite()) {
            return q;
        }
        if (q.IsInfinite()) {
            return p;
        }
        if (p.Equals(q)) {
            return XDbl(curve, p);
        }
        if (p.Equals(q.Negate())) {
            SikeParam sikeParam = curve.GetSikeParam();
            return Fp2PointAffine.Infinity(sikeParam);
        }

        Fp2ElementRef t0, t1, t2, xpq, ypq;
        var b = curve.GetB();

        t0 = q.GetY().Subtract(p.GetY());
        t1 = q.GetX().Subtract(p.GetX());
        t1 = t1.Inverse();
        t0 = t0.Multiply(t1);
        t1 = t0.Square();
        t2 = p.GetX().Add(p.GetX());
        t2 = t2.Add(q.GetX());
        t2 = t2.Add(curve.GetA());
        t2 = t2.Multiply(t0);
        t0 = t0.Multiply(t1);
        t0 = b.Multiply(t0);
        t0 = t0.Add(p.GetY());
        t0 = t2.Subtract(t0);
        t1 = b.Multiply(t1);
        t1 = t1.Subtract(curve.GetA());
        t1 = t1.Subtract(p.GetX());
        xpq = t1.Subtract(q.GetX());
        ypq = t0;
        return new Fp2PointAffine(xpq, ypq);
    }

    /**
     * Recover the point R = P - Q.
     * @param curve Current curve.
     * @param p Point P.
     * @param q Point Q.
     * @return Calculated point R.
     */
    public Fp2PointAffine GetXr(MontgomeryCurve curve, Fp2PointAffine p, Fp2PointAffine q) {
        var qNeg = new Fp2PointAffine(q.GetX(), q.GetY().Negate());
        return XAdd(curve, p, qNeg);
    }

    /**
     * Recover the curve and points P and Q.
     * @param sikeParam SIKE parameters.
     * @param px The x coordinate of point P.
     * @param qx The x coordinate of point Q.
     * @param rx The x coordinate of point R.
     * @return A recovered curve and points P and Q.
     */
    public EvaluatedCurve GetYpYqAB(SikeParam sikeParam, Fp2ElementRef px, Fp2ElementRef qx, Fp2ElementRef rx) {
        Fp2ElementRef b, t1, t2, py, qy;
        var a = GetA(sikeParam, px, qx, rx);
        b = sikeParam.GetFp2ElementFactory().One();
        MontgomeryCurve curve = new MontgomeryCurve(sikeParam, a, b);
        t1 = px.Square();
        t2 = px.Multiply(t1);
        t1 = a.Multiply(t1);
        t1 = t2.Add(t1);
        t1 = t1.Add(px);
        py = t1.Sqrt();
        t1 = qx.Square();
        t2 = qx.Multiply(t1);
        t1 = a.Multiply(t1);
        t1 = t2.Add(t1);
        t1 = t1.Add(qx);
        qy = t1.Sqrt();
        var p = new Fp2PointAffine(px, py);
        var q1 = new Fp2PointAffine(qx, qy.Negate());
        var t = XAdd(curve, p, q1);
        if (!t.GetX().Equals(rx)) {
            qy = qy.Negate();
        }
        var q = new Fp2PointAffine(qx, qy);
        return new EvaluatedCurve(curve, p, q);
    }

}
