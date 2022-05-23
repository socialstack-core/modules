using System;

namespace Lumity.SikeIsogeny;

/**
 * Point with affine coordinates [x:y] in F(p^2).
 *
 * @author Roman Strobl, roman.strobl@wultra.com
 */
public class Fp2PointAffine {

    private Fp2ElementRef x;
    private Fp2ElementRef y;

    /**
     * Affine point constructor.
     * @param x The x element.
     * @param y The y element.
     */
    public Fp2PointAffine(Fp2ElementRef x, Fp2ElementRef y) {
        this.x = x;
        this.y = y;
    }

    /**
     * Construct point at infinity.
     * @param sikeParam SIKE parameters.
     * @return Point at infinity.
     */
    public static Fp2PointAffine Infinity(SikeParam sikeParam) {
        var zero = sikeParam.GetFp2ElementFactory().Zero();
        return new Fp2PointAffine(zero, zero);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public Fp2ElementRef GetX() {
        return x;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public Fp2ElementRef GetY() {
        return y;
    }

    /**
     * The z coordinate is not defined in affine coordinate system.
     */
    public Fp2ElementRef GetZ() {
        throw new Exception("Invalid point coordinate");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="o"></param>
    /// <returns></returns>
    public Fp2PointAffine Add(Fp2PointAffine o) {
        return new Fp2PointAffine(x.Add(o.GetX()), y.Add(o.GetY()));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="o"></param>
    /// <returns></returns>
    public Fp2PointAffine Subtract(Fp2PointAffine o) {
        return new Fp2PointAffine(x.Subtract(o.GetX()), y.Subtract(o.GetY()));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="o"></param>
    /// <returns></returns>
    public Fp2PointAffine Multiply(Fp2PointAffine o) {
        return new Fp2PointAffine(x.Multiply(o.GetX()), y.Multiply(o.GetY()));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public Fp2PointAffine Square() {
        return Multiply(this);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public Fp2PointAffine Inverse() {
        return new Fp2PointAffine(x.Inverse(), y.Inverse());
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public Fp2PointAffine Negate() {
        return new Fp2PointAffine(x, y.Negate());
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public bool IsInfinite() {
        return y.IsZero();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public Fp2PointAffine Copy() {
        return new Fp2PointAffine(x.Copy(), y.Copy());
    }
	
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public override string ToString() {
        return "(" + x.ToString() + ", " + y.ToString() + ")";
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="o"></param>
    /// <returns></returns>
    public bool Equals(Fp2PointAffine that) {
        if (this == that) return true;
        if (that == null) return false;
        return x.Equals(that.x) &&
                y.Equals(that.y);
    }
}
