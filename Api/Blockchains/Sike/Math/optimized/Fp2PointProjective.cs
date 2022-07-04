using System;

namespace Lumity.SikeIsogeny;

/**
 * Point with projective coordinates [x:z] in F(p^2).
 *
 * @author Roman Strobl, roman.strobl@wultra.com
 */
public class Fp2PointProjective {

    private Fp2ElementOpti x;
    private Fp2ElementOpti z;

    /**
     * Projective point constructor.
     * @param x The x element.
     * @param z The z element.
     */
    public Fp2PointProjective(Fp2ElementOpti x, Fp2ElementOpti z) {
        this.x = x;
        this.z = z;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public Fp2ElementOpti GetX() {
        return x;
    }

    /**
     * The y coordinate is not defined in projective coordinate system.
     */
    public Fp2ElementOpti GetY() {
        throw new Exception("Invalid point coordinate");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public Fp2ElementOpti GetZ() {
        return z;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="o"></param>
    /// <returns></returns>
    public Fp2PointProjective Add(Fp2PointProjective o) {
        return new Fp2PointProjective(x.Add(o.GetX()), z.Add(o.GetY()));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="o"></param>
    /// <returns></returns>
    public Fp2PointProjective Subtract(Fp2PointProjective o) {
        return new Fp2PointProjective(x.Subtract(o.GetX()), z.Subtract(o.GetY()));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="o"></param>
    /// <returns></returns>
    public Fp2PointProjective Multiply(Fp2PointProjective o) {
        return new Fp2PointProjective(x.Multiply(o.GetX()), z.Multiply(o.GetY()));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public Fp2PointProjective Square() {
        return Multiply(this);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public Fp2PointProjective Inverse() {
        return new Fp2PointProjective(x.Inverse(), z.Inverse());
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public Fp2PointProjective Negate() {
        throw new Exception("Not implemented yet");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public bool IsInfinite() {
        throw new Exception("Not implemented yet");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public Fp2PointProjective Copy() {
        return new Fp2PointProjective(x.Copy(), z.Copy());
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public override string ToString() {
        return "(" + x.ToString() + ", " + z.ToString() + ")";
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="that"></param>
    /// <returns></returns>
    public bool Equals(Fp2PointProjective that) {
        if (this == that) return true;
        // Use & to avoid timing attacks
        return x.Equals(that.x) & z.Equals(that.z);
    }

}
