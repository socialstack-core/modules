namespace Lumity.SikeIsogeny;

/**
 * Optimized elliptic curve mathematics on Montgomery curves with projective coordinates.
 *
 * @author Roman Strobl, roman.strobl@wultra.com
 */
public class MontgomeryProjective {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="curve"></param>
    /// <param name="p"></param>
    /// <returns></returns>
    public Fp2PointProjective XDbl(MontgomeryCurveOpti curve, Fp2PointProjective p) {
        MontgomeryConstants constants = curve.GetOptimizedConstants();
        var a24plus = constants.GetA24plus();
        var c24 = constants.GetC24();
        Fp2ElementOpti t0, t1, p2x, p2z;
        t0 = p.GetX().Subtract(p.GetZ());
        t1 = p.GetX().Add(p.GetZ());
        t0 = t0.Square();
        t1 = t1.Square();
        p2z = c24.Multiply(t0);
        p2x = p2z.Multiply(t1);
        t1 = t1.Subtract(t0);
        t0 = a24plus.Multiply(t1);
        p2z = p2z.Add(t0);
        p2z = p2z.Multiply(t1);
        return new Fp2PointProjective(p2x, p2z);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="curve"></param>
    /// <param name="p"></param>
    /// <returns></returns>
    public Fp2PointProjective XTpl(MontgomeryCurveOpti curve, Fp2PointProjective p) {
        MontgomeryConstants constants = curve.GetOptimizedConstants();
        var a24plus = constants.GetA24plus();
        var a24minus = constants.GetA24minus();
        Fp2ElementOpti t0, t1, t2, t3, t4, t5, t6, p3x, p3z;
        t0 = p.GetX().Subtract(p.GetZ());
        t2 = t0.Square();
        t1 = p.GetX().Add(p.GetZ());
        t3 = t1.Square();
        t4 = t1.Add(t0);
        t0 = t1.Subtract(t0);
        t1 = t4.Square();
        t1 = t1.Subtract(t3);
        t1 = t1.Subtract(t2);
        // Multiplicands are swapped for faster computation as it is done in official C implementation.
        t5 = a24plus.Multiply(t3);
        t3 = t5.Multiply(t3);
        t6 = t2.Multiply(a24minus);
        t2 = t2.Multiply(t6);
        t3 = t2.Subtract(t3);
        t2 = t5.Subtract(t6);
        t1 = t2.Multiply(t1);
        t2 = t3.Add(t1);
        t2 = t2.Square();
        p3x = t2.Multiply(t4);
        t1 = t3.Subtract(t1);
        t1 = t1.Square();
        p3z = t1.Multiply(t0);
        return new Fp2PointProjective(p3x, p3z);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="curve"></param>
    /// <param name="p"></param>
    /// <param name="e"></param>
    /// <returns></returns>
    public Fp2PointProjective XDble(MontgomeryCurveOpti curve, Fp2PointProjective p, int e) {
        Fp2PointProjective pAp = p;
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
    public Fp2PointProjective XTple(MontgomeryCurveOpti curve, Fp2PointProjective p, int e) {
        Fp2PointProjective pAp = p;
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
    public Fp2ElementOpti JInv(MontgomeryCurveOpti curve) {
        var a = curve.GetA();
        MontgomeryConstants constants = curve.GetOptimizedConstants();
        var c = constants.GetC();
        Fp2ElementOpti t0, t1, j;
        j = a.Square();
        t1 = c.Square();
        t0 = t1.Add(t1);
        t0 = j.Subtract(t0);
        t0 = t0.Subtract(t1);
        j = t0.Subtract(t1);
        t1 = t1.Square();
        j = j.Multiply(t1);
        t0 = t0.Add(t0);
        t0 = t0.Add(t0);
        t1 = t0.Square();
        t0 = t0.Multiply(t1);
        t0 = t0.Add(t0);
        t0 = t0.Add(t0);
        j = j.Inverse();
        j = t0.Multiply(j);
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
    public Fp2ElementOpti GetA(SikeParam sikeParam, Fp2ElementOpti px, Fp2ElementOpti qx, Fp2ElementOpti rx) {
        Fp2ElementOpti t0, t1, ap;
        t1 = px.Add(qx);
        t0 = px.Multiply(qx);
        ap = rx.Multiply(t1);
        ap = ap.Add(t0);
        t0 = t0.Multiply(rx);
        ap = ap.Subtract(sikeParam.GetFp2ElementFactoryOpti().One());
        t0 = t0.Add(t0);
        t1 = t1.Add(rx);
        t0 = t0.Add(t0);
        ap = ap.Square();
        t0 = t0.Inverse();
        ap = ap.Multiply(t0);
        ap = ap.Subtract(t1);
        return ap;
    }

    /**
     * Combined coordinate doubling and differential addition.
     * @param p Point P.
     * @param q Point Q.
     * @param r Point P - Q.
     * @return Points P2 and P + Q.
     */
    private Fp2PointProjective[] XDblAdd(Fp2PointProjective p, Fp2PointProjective q, Fp2PointProjective r, Fp2ElementOpti a24plus) {
        Fp2ElementOpti t0, t1, t2, p2x, p2z, pqx, pqz;
        t0 = p.GetX().Add(p.GetZ());
        t1 = p.GetX().Subtract(p.GetZ());
        p2x = t0.Square();
        t2 = q.GetX().Subtract(q.GetZ());
        pqx = q.GetX().Add(q.GetZ());
        t0 = t0.Multiply(t2);
        p2z = t1.Square();
        t1 = t1.Multiply(pqx);
        t2 = p2x.Subtract(p2z);
        p2x = p2x.Multiply(p2z);
        pqx = a24plus.Multiply(t2);
        pqz = t0.Subtract(t1);
        p2z = pqx.Add(p2z);
        pqx = t0.Add(t1);
        p2z = p2z.Multiply(t2);
        pqz = pqz.Square();
        pqx = pqx.Square();
        pqz = r.GetX().Multiply(pqz);
        pqx = r.GetZ().Multiply(pqx);
        Fp2PointProjective p2 = new Fp2PointProjective(p2x, p2z);
        Fp2PointProjective pq = new Fp2PointProjective(pqx, pqz);
        return new Fp2PointProjective[]{p2, pq};
    }

    /**
     * Three point Montgomery ladder.
     * @param curve Current curve.
     * @param m Scalar value.
     * @param px The x coordinate of point P.
     * @param qx The x coordinate of point Q.
     * @param rx The x coordinate of point P - Q.
     * @param bits Number of bits in field elements.
     * @return Calculated new point.
     */
    public Fp2PointProjective Ladder3Pt(MontgomeryCurveOpti curve, byte[] m, Fp2ElementOpti px, Fp2ElementOpti qx, Fp2ElementOpti rx, int bits) {
        SikeParam sikeParam = curve.GetSikeParam();
        var a = curve.GetA();
        var factory = sikeParam.GetFp2ElementFactoryOpti();
        var r0 = new Fp2PointProjective(qx.Copy(), factory.One());
        var r1 = new Fp2PointProjective(px.Copy(), factory.One());
        var r2 = new Fp2PointProjective(rx.Copy(), factory.One());

        // Compute A + 2C / 4C
        var c = curve.GetOptimizedConstants().GetC();
        var c2 = c.Add(c);
        var aPlus2c = a.Add(c2);
        var c4 = c2.Add(c2);
        var c4Inv = c4.Inverse();
        var aPlus2cOver4c = aPlus2c.Multiply(c4Inv);

        byte prevBit = 0;
        for (int i = 0; i < bits; i++) {
            byte bit = (byte) (m[i >> 3] >> (i & 7) & 1);
            byte swap = (byte) (prevBit ^ bit);
            prevBit = bit;
            CondSwap(r1, r2, swap);
            Fp2PointProjective[] points = XDblAdd(r0, r2, r1, aPlus2cOver4c);
            r0 = points[0].Copy();
            r2 = points[1].Copy();
        }
        CondSwap(r1, r2, prevBit);
        return r1;
    }

    /**
     * Swap two points conditionally.
     * @param sikeParam SIKE parameters.
     * @param p Point p.
     * @param q Point q.
     * @param mask Swap condition, if zero swap is not performed.
     */
    private void CondSwap(Fp2PointProjective p, Fp2PointProjective q, ulong mask) {
        FpElementOpti.ConditionalSwap(p.GetX().GetX0(), q.GetX().GetX0(), mask);
        FpElementOpti.ConditionalSwap(p.GetX().GetX1(), q.GetX().GetX1(), mask);
        FpElementOpti.ConditionalSwap(p.GetZ().GetX0(), q.GetZ().GetX0(), mask);
        FpElementOpti.ConditionalSwap(p.GetZ().GetX1(), q.GetZ().GetX1(), mask);
    }

}
