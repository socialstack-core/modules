using Org.BouncyCastle.Math;

namespace Lumity.SikeIsogeny;

/**
 * SIKE parameters.
 *
 * @author Roman Strobl, roman.strobl@wultra.com
 */
public interface SikeParam {

    /**
     * Get implementation type.
     * @return Implementation type.
     */
    ImplementationType GetImplementationType();

    /**
     * Factory for Fp2Elements.
     * @return Factory for Fp2Elements.
     */
    Fp2ElementFactoryRef GetFp2ElementFactory();

    /**
     * Factory for Fp2Elements.
     * @return Factory for Fp2Elements.
     */
    Fp2ElementFactoryOpti GetFp2ElementFactoryOpti();

    /**
     * Get Montgomery curve math algorithms.
     * @return Montgomery curve math algorithms.
     */
    MontgomeryAffine GetMontgomery();

    /**
     * Get Isogeny curve algorithms.
     * @return Isogeny curve algorithms.
     */
    IsogenyAffine GetIsogeny();

    /**
     * Get Montgomery curve math algorithms.
     * @return Montgomery curve math algorithms.
     */
    MontgomeryProjective GetMontgomeryOpti();

    /**
     * Get Isogeny curve algorithms.
     * @return Isogeny curve algorithms.
     */
    IsogenyProjective GetIsogenyOpti();

    /**
     * Get SIKE variant name.
     * @return SIKE variant name.
     */
    string GetName();

    /**
     * Get Montgomery coefficient a for starting curve.
     * @return Montgomery coefficient a for starting curve.
     */
    Fp2ElementRef GetA();

    /**
     * Get Montgomery coefficient b for starting curve.
     * @return Montgomery coefficient b for starting curve.
     */
    Fp2ElementRef GetB();

    /**
     * Get Montgomery coefficient a for starting curve.
     * @return Montgomery coefficient a for starting curve.
     */
    Fp2ElementOpti GetAOpti();

    /**
     * Get Montgomery coefficient b for starting curve.
     * @return Montgomery coefficient b for starting curve.
     */
    Fp2ElementOpti GetBOpti();

    /**
     * Get parameter eA.
     * @return Parameter eA.
     */
    int GetEA();

    /**
     * Get parameter eB.
     * @return Parameter eB.
     */
    int GetEB();

    /**
     * Get factor of A.
     * @return Factor of A.
     */
    BigInteger GetOrdA();

    /**
     * Get factor of B.
     * @return Factor of b.
     */
    BigInteger GetOrdB();

    /**
     * Get most significant bit of A.
     * @return Most significant bit of A.
     */
    int GetBitsA();

    /**
     * Get most significant bit of B.
     * @return Most significant bit of B.
     */
    int GetBitsB();

    /**
     * Get mask used for key generation of A.
     * @return Mask used for key generation of A.
     */
    byte GetMaskA();

    /**
     * Get mask used for key generation of B.
     * @return Mask used for key generation of B.
     */
    byte GetMaskB();

    /**
     * Get field prime.
     * @return Field prime.
     */
    BigInteger GetPrime();

    /**
     * Get point PA.
     * @return point PA.
     */
    Fp2PointAffine GetPA();

    /**
     * Get point QA.
     * @return point QA.
     */
    Fp2PointAffine GetQA();

    /**
     * Get point RA.
     * @return point RA.
     */
    Fp2PointAffine GetRA();

    /**
     * Get point PB.
     * @return point PB.
     */
    Fp2PointAffine GetPB();

    /**
     * Get point QB.
     * @return point QB.
     */
    Fp2PointAffine GetQB();

    /**
     * Get point RB.
     * @return point RB.
     */
    Fp2PointAffine GetRB();

    /**
     * Get point PA.
     * @return point PA.
     */
    Fp2PointProjective GetPAOpti();

    /**
     * Get point QA.
     * @return point QA.
     */
    Fp2PointProjective GetQAOpti();

    /**
     * Get point RA.
     * @return point RA.
     */
    Fp2PointProjective GetRAOpti();

    /**
     * Get point PB.
     * @return point PB.
     */
    Fp2PointProjective GetPBOpti();

    /**
     * Get point QB.
     * @return point QB.
     */
    Fp2PointProjective GetQBOpti();

    /**
     * Get point RB.
     * @return point RB.
     */
    Fp2PointProjective GetRBOpti();

    /**
     * Get the number of bytes used for cryptography operations.
     * @return Number of bytes used for cryptography operations.
     */
    int GetCryptoBytes();

    /**
     * Get the number of bytes used for message operations.
     * @return Number of bytes used for message operations.
     */
    int GetMessageBytes();

    /**
     * Get number of rows for optimized tree computations in the 2-isogeny graph.
     * @return Number of rows for optimized tree computations in the 2-isogeny graph.
     */
    int GetTreeRowsA();

    /**
     * Get number of rows for optimized tree computations in the 3-isogeny graph.
     * @return Number of rows for optimized tree computations in the 3-isogeny graph.
     */
    int GetTreeRowsB();

    /**
     * Get maximum number of points for optimized tree computations in the 2-isogeny graph.
     * @return Maxim number of points for optimized tree computations in the 2-isogeny graph.
     */
    int GetTreePointsA();

    /**
     * Get maximum number of points for optimized tree computations in the 3-isogeny graph.
     * @return Maxim number of points for optimized tree computations in the 3-isogeny graph.
     */
    int GetTreePointsB();

    /**
     * Get optimization strategy for tree computations in the 2-isogeny graph.
     * @return Optimization strategy for tree computations in the 2-isogeny graph.
     */
    int[] GetStrategyA();

    /**
     * Get optimization strategy for tree computations in the 3-isogeny graph.
     * @return Optimization strategy for tree computations in the 3-isogeny graph.
     */
    int[] GetStrategyB();

    /**
     * Get size of long array for optimized elements.
     * @return Size of long array.
     */
    int GetFpWords();

    /**
     * Get number of 0 digits in the least significant part of p + 1.
     * @return Number of 0 digits in the least significant part of p + 1.
     */
    int GetZeroWords();

    /**
     * Get optimized field prime p.
     * @return Field prime p.
     */
    FpElementOpti GetP();

    /**
     * Get optimized value p + 1.
     * @return Value p + 1.
     */
    FpElementOpti GetP1();

    /**
     * Get optimized value p * 2.
     * @return Value p * 2.
     */
    FpElementOpti GetPx2();

    /**
     * Get optimized value pR2.
     * @return Optimized value pR2.
     */
    FpElementOpti GetPR2();

    /**
     * Get the power strategy for the p34 algorithm.
     * @return Power strategy.
     */
    int[] GetPowStrategy();

    /**
     * Get the multiplication strategy for the p34 algorithm.
     * @return Multiplication strategy.
     */
    int[] GetMulStrategy();

    /**
     * Get initial multiplication value for the p34 algorithm.
     * @return Initial multiplication value for the p34 algorithm
     */
    int GetInitialMul();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="o"></param>
    /// <returns></returns>
    bool Equals(SikeParam o);

}
