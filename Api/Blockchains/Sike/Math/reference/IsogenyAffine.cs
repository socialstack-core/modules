using Org.BouncyCastle.Math;

namespace Lumity.SikeIsogeny;

/**
 * Reference elliptic curve isogeny operations on Montgomery curves with projective coordinates.
 *
 * @author Roman Strobl, roman.strobl@wultra.com
 */
public class IsogenyAffine {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="curve"></param>
    /// <param name="p2"></param>
    /// <returns></returns>
    public MontgomeryCurve Curve2Iso(MontgomeryCurve curve, Fp2PointAffine p2) {
        Fp2ElementRef t1, aAp, bAp;
        Fp2ElementRef b = curve.GetB();
        SikeParam sikeParam = curve.GetSikeParam();
        t1 = p2.GetX().Square();
        t1 = t1.Add(t1);
        t1 = sikeParam.GetFp2ElementFactory().One().Subtract(t1);
        aAp = t1.Add(t1);
        bAp = p2.GetX().Multiply(b);
        return new MontgomeryCurve(curve.GetSikeParam(), aAp, bAp);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="curve"></param>
    /// <param name="p3"></param>
    /// <returns></returns>
    public MontgomeryCurve Curve3Iso(MontgomeryCurve curve, Fp2PointAffine p3) {
        Fp2ElementRef t1, t2, aAp, bAp;
        Fp2ElementRef b = curve.GetB();
        SikeParam sikeParam = curve.GetSikeParam();
        t1 = p3.GetX().Square();
        bAp = b.Multiply(t1);
        t1 = t1.Add(t1);
        t2 = t1.Add(t1);
        t1 = t1.Add(t2);
        t2 = sikeParam.GetFp2ElementFactory().Generate(SideChannelUtil.BigIntegerSix);
        t1 = t1.Subtract(t2);
        t2 = curve.GetA().Multiply(p3.GetX());
        t1 = t2.Subtract(t1);
        aAp = t1.Multiply(p3.GetX());
        return new MontgomeryCurve(curve.GetSikeParam(), aAp, bAp);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="curve"></param>
    /// <param name="p4"></param>
    /// <returns></returns>
    public MontgomeryCurve Curve4Iso(MontgomeryCurve curve, Fp2PointAffine p4) {
        Fp2ElementRef t1, t2, aAp, bAp;
        Fp2ElementRef b = curve.GetB();
        SikeParam sikeParam = curve.GetSikeParam();
        t1 = p4.GetX().Square();
        aAp = t1.Square();
        aAp = aAp.Add(aAp);
        aAp = aAp.Add(aAp);
        t2 = sikeParam.GetFp2ElementFactory().Generate(SideChannelUtil.BigIntegerTwo);
        aAp = aAp.Subtract(t2);
        t1 = p4.GetX().Multiply(t1);
        t1 = t1.Add(p4.GetX());
        t1 = t1.Multiply(b);
        t2 = t2.Inverse();
        t2 = t2.Negate();
        bAp = t2.Multiply(t1);
        return new MontgomeryCurve(curve.GetSikeParam(), aAp, bAp);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="q"></param>
    /// <param name="p2"></param>
    /// <returns></returns>
    public Fp2PointAffine Eval2Iso(Fp2PointAffine q, Fp2PointAffine p2) {
        Fp2ElementRef t1, t2, t3, qxAp, qyAp;
        t1 = q.GetX().Multiply(p2.GetX());
        t2 = q.GetX().Multiply(t1);
        t3 = t1.Multiply(p2.GetX());
        t3 = t3.Add(t3);
        t3 = t2.Subtract(t3);
        t3 = t3.Add(p2.GetX());
        t3 = q.GetY().Multiply(t3);
        t2 = t2.Subtract(q.GetX());
        t1 = q.GetX().Subtract(p2.GetX());
        t1 = t1.Inverse();
        qxAp = t2.Multiply(t1);
        t1 = t1.Square();
        qyAp = t3.Multiply(t1);
        return new Fp2PointAffine(qxAp, qyAp);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="curve"></param>
    /// <param name="q"></param>
    /// <param name="p3"></param>
    /// <returns></returns>
    public Fp2PointAffine Eval3Iso(MontgomeryCurve curve, Fp2PointAffine q, Fp2PointAffine p3) {
        Fp2ElementRef t1, t2, t3, t4, qxAp, qyAp;
        SikeParam sikeParam = curve.GetSikeParam();
        t1 = q.GetX().Square();
        t1 = t1.Multiply(p3.GetX());
        t2 = p3.GetX().Square();
        t2 = q.GetX().Multiply(t2);
        t3 = t2.Add(t2);
        t2 = t2.Add(t3);
        t1 = t1.Subtract(t2);
        t1 = t1.Add(q.GetX());
        t1 = t1.Add(p3.GetX());
        t2 = q.GetX().Subtract(p3.GetX());
        t2 = t2.Inverse();
        t3 = t2.Square();
        t2 = t2.Multiply(t3);
        t4 = q.GetX().Multiply(p3.GetX());
        t4 = t4.Subtract(sikeParam.GetFp2ElementFactory().One());
        t1 = t4.Multiply(t1);
        t1 = t1.Multiply(t2);
        t2 = t4.Square();
        t2 = t2.Multiply(t3);
        qxAp = q.GetX().Multiply(t2);
        qyAp = q.GetY().Multiply(t1);
        return new Fp2PointAffine(qxAp, qyAp);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="curve"></param>
    /// <param name="q"></param>
    /// <param name="p4"></param>
    /// <returns></returns>
    public Fp2PointAffine Eval4Iso(MontgomeryCurve curve, Fp2PointAffine q, Fp2PointAffine p4) {
        Fp2ElementRef t1, t2, t3, t4, t5, qxAp, qyAp;
        SikeParam sikeParam = curve.GetSikeParam();
        t1 = q.GetX().Square();
        t2 = t1.Square();
        t3 = p4.GetX().Square();
        t4 = t2.Multiply(t3);
        t2 = t2.Add(t4);
        t4 = t1.Multiply(t3);
        t4 = t4.Add(t4);
        t5 = t4.Add(t4);
        t5 = t5.Add(t5);
        t4 = t4.Add(t5);
        t2 = t2.Add(t4);
        t4 = t3.Square();
        t5 = t1.Multiply(t4);
        t5 = t5.Add(t5);
        t2 = t2.Add(t5);
        t1 = t1.Multiply(q.GetX());
        t4 = p4.GetX().Multiply(t3);
        t5 = t1.Multiply(t4);
        t5 = t5.Add(t5);
        t5 = t5.Add(t5);
        t2 = t2.Subtract(t5);
        t1 = t1.Multiply(p4.GetX());
        t1 = t1.Add(t1);
        t1 = t1.Add(t1);
        t1 = t2.Subtract(t1);
        t2 = q.GetX().Multiply(t4);
        t2 = t2.Add(t2);
        t2 = t2.Add(t2);
        t1 = t1.Subtract(t2);
        t1 = t1.Add(t3);
        t1 = t1.Add(sikeParam.GetFp2ElementFactory().One());
        t2 = q.GetX().Multiply(p4.GetX());
        t4 = t2.Subtract(sikeParam.GetFp2ElementFactory().One());
        t2 = t2.Add(t2);
        t5 = t2.Add(t2);
        t1 = t1.Subtract(t5);
        t1 = t4.Multiply(t1);
        t1 = t3.Multiply(t1);
        t1 = q.GetY().Multiply(t1);
        t1 = t1.Add(t1);
        qyAp = t1.Negate();
        t2 = t2.Subtract(t3);
        t1 = t2.Subtract(sikeParam.GetFp2ElementFactory().One());
        t2 = q.GetX().Subtract(p4.GetX());
        t1 = t2.Multiply(t1);
        t5 = t1.Square();
        t5 = t5.Multiply(t2);
        t5 = t5.Inverse();
        qyAp = qyAp.Multiply(t5);
        t1 = t1.Multiply(t2);
        t1 = t1.Inverse();
        t4 = t4.Square();
        t1 = t1.Multiply(t4);
        t1 = q.GetX().Multiply(t1);
        t2 = q.GetX().Multiply(t3);
        t2 = t2.Add(q.GetX());
        t3 = p4.GetX().Add(p4.GetX());
        t2 = t2.Subtract(t3);
        t2 = t2.Negate();
        qxAp = t1.Multiply(t2);
        return new Fp2PointAffine(qxAp, qyAp);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="curve"></param>
    /// <param name="s"></param>
    /// <param name="points"></param>
    /// <returns></returns>
    public EvaluatedCurve Iso2e(MontgomeryCurve curve, Fp2PointAffine s, params Fp2PointAffine[] points) {
        var montgomery = curve.GetSikeParam().GetMontgomery();
        var curveAp = curve;
        Fp2PointAffine sAp = s, phiP = null, phiQ = null;
        if (points.Length == 2) {
            phiP = points[0];
            phiQ = points[1];
        }
        Fp2PointAffine r;
        int eAp = curve.GetSikeParam().GetEA();
        if (eAp % 2 == 1) {
            r = montgomery.XDble(curveAp, sAp, eAp - 1);
            curveAp = Curve2Iso(curveAp, r);
            sAp = Eval2Iso(sAp, r);
            if (points.Length == 2) {
                phiP = Eval2Iso(phiP, r);
                phiQ = Eval2Iso(phiQ, r);
            }
            eAp--;
        }
        for (int e = eAp - 2; e >= 0; e -= 2) {
            r = montgomery.XDble(curveAp, sAp, e);
            curveAp = Curve4Iso(curveAp, r);
            // Fix division by zero in reference implementation
            if (e > 0) {
                sAp = Eval4Iso(curveAp, sAp, r);
            }
            if (points.Length == 2) {
                phiP = Eval4Iso(curveAp, phiP, r);
                phiQ = Eval4Iso(curveAp, phiQ, r);
            }
        }
        return new EvaluatedCurve(curveAp, phiP, phiQ);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="curve"></param>
    /// <param name="s"></param>
    /// <param name="points"></param>
    /// <returns></returns>
    public EvaluatedCurve Iso3e(MontgomeryCurve curve, Fp2PointAffine s, params Fp2PointAffine[] points) {
        var montgomery = curve.GetSikeParam().GetMontgomery();
        MontgomeryCurve curveAp = curve;
        Fp2PointAffine sAp = s, phiP = null, phiQ = null;
        if (points.Length == 2) {
            phiP = points[0];
            phiQ = points[1];
        }
        Fp2PointAffine r;
        for (int e = curve.GetSikeParam().GetEB() - 1; e >= 0; e--) {
            r = montgomery.XTple(curveAp, sAp, e);
            curveAp = Curve3Iso(curveAp, r);
            // Fix division by zero in reference implementation
            if (e > 0) {
                sAp = Eval3Iso(curveAp, sAp, r);
            }
            if (points.Length == 2) {
                phiP = Eval3Iso(curveAp, phiP, r);
                phiQ = Eval3Iso(curveAp, phiQ, r);
            }
        }
        return new EvaluatedCurve(curveAp, phiP, phiQ);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="curve"></param>
    /// <param name="privateKey"></param>
    /// <returns></returns>
    public SidhPublicKeyRef IsoGen2(MontgomeryCurve curve, SidhPrivateKeyRef privateKey) {
        SikeParam sikeParam = curve.GetSikeParam();
        MontgomeryAffine montgomery = (MontgomeryAffine) sikeParam.GetMontgomery();
        var isogeny = sikeParam.GetIsogeny();
        var s = montgomery.DoubleAndAdd(curve, privateKey.GetFpElement().GetX(), sikeParam.GetQA(), sikeParam.GetBitsA());
        s = montgomery.XAdd(curve, sikeParam.GetPA(), s);
        EvaluatedCurve evaluatedCurve = isogeny.Iso2e(curve, s, sikeParam.GetPB(), sikeParam.GetQB());
        return CreatePublicKey(sikeParam, evaluatedCurve);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="curve"></param>
    /// <param name="privateKey"></param>
    /// <returns></returns>
    public SidhPublicKeyRef IsoGen3(MontgomeryCurve curve, SidhPrivateKeyRef privateKey) {
        SikeParam sikeParam = curve.GetSikeParam();
        MontgomeryAffine montgomery = (MontgomeryAffine) sikeParam.GetMontgomery();
        var isogeny = sikeParam.GetIsogeny();
        var s = montgomery.DoubleAndAdd(curve, privateKey.GetFpElement().GetX(), sikeParam.GetQB(), sikeParam.GetBitsB() - 1);
        s = montgomery.XAdd(curve, sikeParam.GetPB(), s);
        EvaluatedCurve evaluatedCurve = isogeny.Iso3e(curve, s, sikeParam.GetPA(), sikeParam.GetQA());
        return CreatePublicKey(sikeParam, evaluatedCurve);
    }

    /**
     * Create a public key from evaluated curve.
     * @param sikeParam SIKE parameters.
     * @param evaluatedCurve Evaluated curve.
     * @return Public key.
     */
    private SidhPublicKeyRef CreatePublicKey(SikeParam sikeParam, EvaluatedCurve evaluatedCurve) {
        MontgomeryAffine montgomery = (MontgomeryAffine) sikeParam.GetMontgomery();
        var p = evaluatedCurve.GetP();
        var q = evaluatedCurve.GetQ();
        var r = montgomery.GetXr(evaluatedCurve.GetCurve(), p, q);

        var px = new Fp2ElementRef(sikeParam, p.GetX().GetX0(), p.GetX().GetX1());
        var qx = new Fp2ElementRef(sikeParam, q.GetX().GetX0(), q.GetX().GetX1());
        var rx = new Fp2ElementRef(sikeParam, r.GetX().GetX0(), r.GetX().GetX1());
        return new SidhPublicKeyRef(sikeParam, px, qx, rx);
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
    public Fp2ElementRef IsoEx2(SikeParam sikeParam, byte[] sk2, Fp2ElementRef p2, Fp2ElementRef q2, Fp2ElementRef r2) {
        MontgomeryAffine montgomery = (MontgomeryAffine) sikeParam.GetMontgomery();
        EvaluatedCurve iso = montgomery.GetYpYqAB(sikeParam, p2, q2, r2);
        MontgomeryCurve curve = iso.GetCurve();
        BigInteger m = ByteEncoding.FromByteArray(sk2);
        var s = montgomery.DoubleAndAdd(curve, m, iso.GetQ(), sikeParam.GetBitsA());
        s = montgomery.XAdd(curve, iso.GetP(), s);
        EvaluatedCurve iso2 = Iso2e(curve, s);
        return montgomery.JInv(iso2.GetCurve());
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
    public Fp2ElementRef IsoEx3(SikeParam sikeParam, byte[] sk3, Fp2ElementRef p3, Fp2ElementRef q3, Fp2ElementRef r3) {
        var montgomery = sikeParam.GetMontgomery();
        var iso = montgomery.GetYpYqAB(sikeParam, p3, q3, r3);
        var curve = iso.GetCurve();
        var m = ByteEncoding.FromByteArray(sk3);
        var s = montgomery.DoubleAndAdd(curve, m, iso.GetQ(), sikeParam.GetBitsB() - 1);
        s = montgomery.XAdd(curve, iso.GetP(), s);
        EvaluatedCurve iso3 = Iso3e(curve, s);
        return montgomery.JInv(iso3.GetCurve());
    }

}
