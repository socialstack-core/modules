using Org.BouncyCastle.Math;

namespace Lumity.SikeIsogeny;

/**
 * Optimized elliptic curve isogeny operations on Montgomery curves with projective coordinates.
 *
 * @author Roman Strobl, roman.strobl@wultra.com
 */
public class IsogenyProjective {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="curve"></param>
    /// <param name="p2"></param>
    /// <returns></returns>
    public MontgomeryCurveOpti Curve2Iso(MontgomeryCurveOpti curve, Fp2PointProjective p2) {
        Fp2ElementOpti a24plus, c24;
        a24plus = p2.GetX().Square();
        c24 = p2.GetZ().Square();
        a24plus = c24.Subtract(a24plus);
        var curve2 = new MontgomeryCurveOpti(curve.GetSikeParam());
        MontgomeryConstants constants = curve2.GetOptimizedConstants();
        constants.SetA24plus(a24plus);
        constants.SetC24(c24);
        return curve2;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="curve"></param>
    /// <param name="p3"></param>
    /// <returns></returns>
    public MontgomeryCurveOpti Curve3Iso(MontgomeryCurveOpti curve, Fp2PointProjective p3) {
        Fp2ElementOpti k1, k2, t0, t1, t2, t3, t4, a24plus, a24minus;
        k1 = p3.GetX().Subtract(p3.GetZ());
        t0 = k1.Square();
        k2 = p3.GetX().Add(p3.GetZ());
        t1 = k2.Square();
        // The optimized implementation deviates from the specification at this point, see:
        // https://github.com/microsoft/PQCrypto-SIDH/commit/7218dce28a25f17c3860c227df5d8a7b2adf1d1b#diff-08daf821aec6f05034194147b25fb0d9726a4d4ede8f4634c6a66fdbb728047fR170
        // The algorithm is defined as Alg. 15 in specification at https://sike.org/files/SIDH-spec.pdf
        // This implementation will use the original algorithm until the change is propagated into the specification.
        t2 = t0.Add(t1);
        t3 = k1.Add(k2);
        t3 = t3.Square();
        t3 = t3.Subtract(t2);
        t2 = t1.Add(t3);
        t3 = t3.Add(t0);
        t4 = t3.Add(t0);
        t4 = t4.Add(t4);
        t4 = t1.Add(t4);
        a24minus = t2.Multiply(t4);
        t4 = t1.Add(t2);
        t4 = t4.Add(t4);
        t4 = t0.Add(t4);
        a24plus = t3.Multiply(t4);
        var curve3 = new MontgomeryCurveOpti(curve.GetSikeParam());
        MontgomeryConstants constants = curve3.GetOptimizedConstants();
        constants.SetA24plus(a24plus);
        constants.SetA24minus(a24minus);
        constants.SetK1(k1);
        constants.SetK2(k2);
        return curve3;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="curve"></param>
    /// <param name="p4"></param>
    /// <returns></returns>
    public MontgomeryCurveOpti Curve4Iso(MontgomeryCurveOpti curve, Fp2PointProjective p4) {
        Fp2ElementOpti k1, k2, k3, a24plus, c24;
        k2 = p4.GetX().Subtract(p4.GetZ());
        k3 = p4.GetX().Add(p4.GetZ());
        k1 = p4.GetZ().Square();
        k1 = k1.Add(k1);
        c24 = k1.Square();
        k1 = k1.Add(k1);
        a24plus = p4.GetX().Square();
        a24plus = a24plus.Add(a24plus);
        a24plus = a24plus.Square();
        var curve4 = new MontgomeryCurveOpti(curve.GetSikeParam());
        MontgomeryConstants constants = curve4.GetOptimizedConstants();
        constants.SetA24plus(a24plus);
        constants.SetC24(c24);
        constants.SetK1(k1);
        constants.SetK2(k2);
        constants.SetK3(k3);
        return curve4;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="q"></param>
    /// <param name="p2"></param>
    /// <returns></returns>
    public Fp2PointProjective Eval2Iso(Fp2PointProjective q, Fp2PointProjective p2) {
        Fp2ElementOpti t0, t1, t2, t3, qx, qz;
        t0 = p2.GetX().Add(p2.GetZ());
        t1 = p2.GetX().Subtract(p2.GetZ());
        t2 = q.GetX().Add(q.GetZ());
        t3 = q.GetX().Subtract(q.GetZ());
        t0 = t0.Multiply(t3);
        t1 = t1.Multiply(t2);
        t2 = t0.Add(t1);
        t3 = t0.Subtract(t1);
        qx = q.GetX().Multiply(t2);
        qz = q.GetZ().Multiply(t3);
        return new Fp2PointProjective(qx, qz);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="curve"></param>
    /// <param name="q"></param>
    /// <param name="p3"></param>
    /// <returns></returns>
    public Fp2PointProjective Eval3Iso(MontgomeryCurveOpti curve, Fp2PointProjective q, Fp2PointProjective p3) {
        Fp2ElementOpti t0, t1, t2, qx, qz, k1, k2;
        k1 = curve.GetOptimizedConstants().GetK1();
        k2 = curve.GetOptimizedConstants().GetK2();
        t0 = q.GetX().Add(q.GetZ());
        t1 = q.GetX().Subtract(q.GetZ());
        t0 = k1.Multiply(t0);
        t1 = k2.Multiply(t1);
        t2 = t0.Add(t1);
        t0 = t1.Subtract(t0);
        t2 = t2.Square();
        t0 = t0.Square();
        qx = q.GetX().Multiply(t2);
        qz = q.GetZ().Multiply(t0);
        return new Fp2PointProjective(qx, qz);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="curve"></param>
    /// <param name="q"></param>
    /// <param name="p4"></param>
    /// <returns></returns>
    public Fp2PointProjective Eval4Iso(MontgomeryCurveOpti curve, Fp2PointProjective q, Fp2PointProjective p4) {
        Fp2ElementOpti t0, t1, qx, qz, k1, k2, k3;
        k1 = curve.GetOptimizedConstants().GetK1();
        k2 = curve.GetOptimizedConstants().GetK2();
        k3 = curve.GetOptimizedConstants().GetK3();
        t0 = q.GetX().Add(q.GetZ());
        t1 = q.GetX().Subtract(q.GetZ());
        qx = t0.Multiply(k2);
        qz = t1.Multiply(k3);
        t0 = t0.Multiply(t1);
        // Multiplicands are swapped for faster computation as it is done in official C implementation.
        t0 = k1.Multiply(t0);
        t1 = qx.Add(qz);
        qz = qx.Subtract(qz);
        t1 = t1.Square();
        qz = qz.Square();
        qx = t0.Add(t1);
        t0 = qz.Subtract(t0);
        qx = qx.Multiply(t1);
        qz = qz.Multiply(t0);
        return new Fp2PointProjective(qx, qz);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="curve"></param>
    /// <param name="s0"></param>
    /// <param name="points"></param>
    /// <returns></returns>
    public EvaluatedCurveOpti Iso2e(MontgomeryCurveOpti curve, Fp2PointProjective s0, params Fp2PointProjective[] points) {
        SikeParam sikeParam = curve.GetSikeParam();
        var montgomery = sikeParam.GetMontgomeryOpti();
        var curveAp = curve;
        Fp2PointProjective r = s0, phiP = null, phiQ = null, phiR = null;
        if (points.Length == 3) {
            phiP = points[0];
            phiQ = points[1];
            phiR = points[2];
        }
        int m, pointCount = 0, ii = 0;
        Fp2PointProjective[] treePoints = new Fp2PointProjective[sikeParam.GetTreePointsA()];
        int[] pointIndex = new int[sikeParam.GetTreeRowsA()];
        int[] strategy = sikeParam.GetStrategyA();

        int eAp = curve.GetSikeParam().GetEA();
        if (eAp % 2 == 1) {
            Fp2PointProjective s = montgomery.XDble(curveAp, r, eAp - 1);
            curveAp = Curve2Iso(curveAp, s);
            if (points.Length == 3) {
                phiP = Eval2Iso(phiP, s);
                phiQ = Eval2Iso(phiQ, s);
                phiR = Eval2Iso(phiR, s);
            }
            r = Eval2Iso(r, s);
        }

        int index = 0;
        for (int row = 1; row < sikeParam.GetTreeRowsA(); row++) {
            while (index < sikeParam.GetTreeRowsA() - row) {
                treePoints[pointCount] = r.Copy();
                pointIndex[pointCount++] = index;
                m = strategy[ii++];
                r = montgomery.XDble(curveAp, r, 2 * m);
                index += m;
            }

            curveAp = Curve4Iso(curveAp, r);

            for (int i = 0; i < pointCount; i++) {
                treePoints[i] = Eval4Iso(curveAp, treePoints[i], null);
            }
            if (points.Length == 3) {
                phiP = Eval4Iso(curveAp, phiP, null);
                phiQ = Eval4Iso(curveAp, phiQ, null);
                phiR = Eval4Iso(curveAp, phiR, null);
            }
            r = treePoints[pointCount - 1].Copy();
            index = pointIndex[pointCount - 1];
            pointCount--;
        }

        curveAp = Curve4Iso(curveAp, r);

        if (points.Length == 3) {
            phiP = Eval4Iso(curveAp, phiP, null);
            phiQ = Eval4Iso(curveAp, phiQ, null);
            phiR = Eval4Iso(curveAp, phiR, null);
            Fp2ElementOpti[] inverted = inv3Way(phiP.GetZ(), phiQ.GetZ(), phiR.GetZ());
            phiP = new Fp2PointProjective(phiP.GetX().Multiply(inverted[0]), inverted[0]);
            phiQ = new Fp2PointProjective(phiQ.GetX().Multiply(inverted[1]), inverted[1]);
            phiR = new Fp2PointProjective(phiR.GetX().Multiply(inverted[2]), inverted[2]);
        }

        return new EvaluatedCurveOpti(curveAp, phiP, phiQ, phiR);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="curve"></param>
    /// <param name="s0"></param>
    /// <param name="points"></param>
    /// <returns></returns>
    public EvaluatedCurveOpti Iso3e(MontgomeryCurveOpti curve, Fp2PointProjective s0, params Fp2PointProjective[] points) {
        SikeParam sikeParam = curve.GetSikeParam();
        var montgomery = sikeParam.GetMontgomeryOpti();
        var curveAp = curve;
        Fp2PointProjective r = s0, phiP = null, phiQ = null, phiR = null;
        if (points.Length == 3) {
            phiP = points[0];
            phiQ = points[1];
            phiR = points[2];
        }
        int m, pointCount = 0, ii = 0;
        Fp2PointProjective[] treePoints = new Fp2PointProjective[sikeParam.GetTreePointsB()];
        int[] pointIndex = new int[sikeParam.GetTreeRowsB()];
        int[] strategy = sikeParam.GetStrategyB();

        int index = 0;
        for (int row = 1; row < sikeParam.GetTreeRowsB(); row++) {
            while (index < sikeParam.GetTreeRowsB() - row) {
                treePoints[pointCount] = r.Copy();
                pointIndex[pointCount++] = index;
                m = strategy[ii++];
                r = montgomery.XTple(curveAp, r, m);
                index += m;
            }

            curveAp = Curve3Iso(curveAp, r);

            for (int i = 0; i < pointCount; i++) {
                treePoints[i] = Eval3Iso(curveAp, treePoints[i], r);
            }
            if (points.Length == 3) {
                phiP = Eval3Iso(curveAp, phiP, r);
                phiQ = Eval3Iso(curveAp, phiQ, r);
                phiR = Eval3Iso(curveAp, phiR, r);
            }
            r = treePoints[pointCount - 1].Copy();
            index = pointIndex[pointCount - 1];
            pointCount--;
        }

        curveAp = Curve3Iso(curveAp, r);

        if (points.Length == 3) {
            phiP = Eval3Iso(curveAp, phiP, r);
            phiQ = Eval3Iso(curveAp, phiQ, r);
            phiR = Eval3Iso(curveAp, phiR, r);
            Fp2ElementOpti[] inverted = inv3Way(phiP.GetZ(), phiQ.GetZ(), phiR.GetZ());
            phiP = new Fp2PointProjective(phiP.GetX().Multiply(inverted[0]), inverted[0]);
            phiQ = new Fp2PointProjective(phiQ.GetX().Multiply(inverted[1]), inverted[1]);
            phiR = new Fp2PointProjective(phiR.GetX().Multiply(inverted[2]), inverted[2]);
        }

        return new EvaluatedCurveOpti(curveAp, phiP, phiQ, phiR);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="curve"></param>
    /// <param name="privateKey"></param>
    /// <returns></returns>
    public SidhPublicKeyOpti IsoGen2(MontgomeryCurveOpti curve, SidhPrivateKeyOpti privateKey) {
        SikeParam sikeParam = curve.GetSikeParam();
        MontgomeryProjective montgomery = sikeParam.GetMontgomeryOpti();
        var p1 = new Fp2PointProjective(sikeParam.GetPBOpti().GetX(), sikeParam.GetFp2ElementFactoryOpti().One());
        var p2 = new Fp2PointProjective(sikeParam.GetQBOpti().GetX(), sikeParam.GetFp2ElementFactoryOpti().One());
        var p3 = new Fp2PointProjective(sikeParam.GetRBOpti().GetX(), sikeParam.GetFp2ElementFactoryOpti().One());
        var s = montgomery.Ladder3Pt(curve, privateKey.GetKey(), sikeParam.GetPAOpti().GetX(), sikeParam.GetQAOpti().GetX(),
                sikeParam.GetRAOpti().GetX(), sikeParam.GetBitsA());
        var evaluatedCurve = Iso2e(curve, s, p1, p2, p3);
        return CreatePublicKey(sikeParam, evaluatedCurve);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="curve"></param>
    /// <param name="privateKey"></param>
    /// <returns></returns>
    public SidhPublicKeyOpti IsoGen3(MontgomeryCurveOpti curve, SidhPrivateKeyOpti privateKey) {
        SikeParam sikeParam = curve.GetSikeParam();
        var montgomery = sikeParam.GetMontgomeryOpti();
        var p1 = new Fp2PointProjective(sikeParam.GetPAOpti().GetX(), sikeParam.GetFp2ElementFactoryOpti().One());
        var p2 = new Fp2PointProjective(sikeParam.GetQAOpti().GetX(), sikeParam.GetFp2ElementFactoryOpti().One());
        var p3 = new Fp2PointProjective(sikeParam.GetRAOpti().GetX(), sikeParam.GetFp2ElementFactoryOpti().One());
        var s = montgomery.Ladder3Pt(curve, privateKey.GetKey(), sikeParam.GetPBOpti().GetX(), sikeParam.GetQBOpti().GetX(),
                sikeParam.GetRBOpti().GetX(), sikeParam.GetBitsB() - 1);
        var evaluatedCurve = Iso3e(curve, s, p1, p2, p3);
        return CreatePublicKey(sikeParam, evaluatedCurve);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sikeParam"></param>
    /// <param name="evaluatedCurve"></param>
    /// <returns></returns>
    private SidhPublicKeyOpti CreatePublicKey(SikeParam sikeParam, EvaluatedCurveOpti evaluatedCurve) {
        var p = evaluatedCurve.GetP();
        var q = evaluatedCurve.GetQ();
        var r = evaluatedCurve.GetR();
        var px = new Fp2ElementOpti(sikeParam, p.GetX().GetX0(), p.GetX().GetX1());
        var qx = new Fp2ElementOpti(sikeParam, q.GetX().GetX0(), q.GetX().GetX1());
        var rx = new Fp2ElementOpti(sikeParam, r.GetX().GetX0(), r.GetX().GetX1());
        return new SidhPublicKeyOpti(sikeParam, px, qx, rx);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sikeParam"></param>
    /// <param name="sk2"></param>
    /// <param name="p2"></param>
    /// <param name="q2"></param>
    /// <param name="r2"></param>
    /// <returns></returns>
    public Fp2ElementOpti IsoEx2(SikeParam sikeParam, byte[] sk2, Fp2ElementOpti p2, Fp2ElementOpti q2, Fp2ElementOpti r2) {
        var montgomery = sikeParam.GetMontgomeryOpti();
        var a = montgomery.GetA(sikeParam, p2, q2, r2);
        var curve = new MontgomeryCurveOpti(sikeParam, a);
        var s = montgomery.Ladder3Pt(curve, sk2, p2, q2, r2, sikeParam.GetBitsA());
        var two = sikeParam.GetFp2ElementFactoryOpti().Generate(SideChannelUtil.BigIntegerTwo);
        var four = sikeParam.GetFp2ElementFactoryOpti().Generate(SideChannelUtil.BigIntegerFour);
        curve.GetOptimizedConstants().SetA24minus(curve.GetA().Add(two));
        curve.GetOptimizedConstants().SetC24(four);
        var iso2 = Iso2e(curve, s);
        var curve2 = iso2.GetCurve();
        var a24plus = curve2.GetOptimizedConstants().GetA24plus();
        var c24 = curve2.GetOptimizedConstants().GetC24();
        var ap = a24plus.Multiply(four);
        ap = ap.Subtract(c24.Multiply(two));
        curve2.SetA(ap);
        curve2.GetOptimizedConstants().SetC(c24);
        return montgomery.JInv(curve2);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sikeParam"></param>
    /// <param name="sk3"></param>
    /// <param name="p3"></param>
    /// <param name="q3"></param>
    /// <param name="r3"></param>
    /// <returns></returns>
    public Fp2ElementOpti IsoEx3(SikeParam sikeParam, byte[] sk3, Fp2ElementOpti p3, Fp2ElementOpti q3, Fp2ElementOpti r3) {
        var montgomery = sikeParam.GetMontgomeryOpti();
        var a = montgomery.GetA(sikeParam, p3, q3, r3);
        var curve = new MontgomeryCurveOpti(sikeParam, a);
        var s = montgomery.Ladder3Pt(curve, sk3, p3, q3, r3, sikeParam.GetBitsB() - 1);
        var two = sikeParam.GetFp2ElementFactoryOpti().Generate(SideChannelUtil.BigIntegerTwo);
        curve.GetOptimizedConstants().SetA24minus(curve.GetA().Add(two));
        curve.GetOptimizedConstants().SetA24minus(curve.GetA().Subtract(two));
        var iso3 = Iso3e(curve, s);
        var curve3 = iso3.GetCurve();
        var a24plus = curve3.GetOptimizedConstants().GetA24plus();
        var a24minus = curve3.GetOptimizedConstants().GetA24minus();
        var ap = a24minus.Add(a24plus);
        ap = ap.Multiply(two);
        var c = a24plus.Subtract(a24minus);
        curve3.SetA(ap);
        curve3.GetOptimizedConstants().SetC(c);
        return montgomery.JInv(curve3);
    }

    /// <summary>
    /// Inverse three F(p^2) elements simultaneously.
    /// </summary>
    /// <param name="z1">First element.</param>
    /// <param name="z2">Second element.</param>
    /// <param name="z3">Third element.</param>
    /// <returns>Inversed elements.</returns>
    private Fp2ElementOpti[] inv3Way(Fp2ElementOpti z1, Fp2ElementOpti z2, Fp2ElementOpti z3) {
        Fp2ElementOpti t0, t1, t2, t3;
        t0 = z1.Multiply(z2);
        t1 = z3.Multiply(t0);
        t1 = t1.Inverse();
        t2 = z3.Multiply(t1);
        t3 = t2.Multiply(z2);
        z2 = t2.Multiply(z1);
        z3 = t0.Multiply(t1);
        z1 = t3;
        return new Fp2ElementOpti[]{z1, z2, z3};
    }
}
