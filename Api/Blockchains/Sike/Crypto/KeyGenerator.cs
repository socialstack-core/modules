using Org.BouncyCastle.Math;
using System;

namespace Lumity.SikeIsogeny;

/**
 * SIDH and SIKE key generator.
 *
 * @author Roman Strobl, roman.strobl@wultra.com
 */
public class KeyGenerator {

    private SikeParam sikeParam;
    private RandomGenerator randomGenerator;

    /**
     * Key generator constructor.
     * @param sikeParam SIKE parameters.
     */
    public KeyGenerator(SikeParam sikeParam) {
        this.sikeParam = sikeParam;
        this.randomGenerator = new RandomGenerator();
    }

    /**
     * Constructor for key generator with alternative random generator.
     * @param sikeParam SIKE parameters.
     * @param randomGenerator Alternative random generator.
     */
    public KeyGenerator(SikeParam sikeParam, RandomGenerator randomGenerator) {
        this.sikeParam = sikeParam;
        this.randomGenerator = randomGenerator;
    }

    /**
     * Generate a key pair.
     * @param party Alice or Bob.
     * @return Generated key pair.
     * @throws GeneralSecurityException Thrown in case cryptography fails.
     */
    public SidhKeyPairRef GenerateKeyPairRef(Party party) {
        var privateKey = GeneratePrivateKeyRef(party);
        var publicKey = DerivePublicKey(party, privateKey);
        return new SidhKeyPairRef(publicKey, privateKey);
    }

    /**
     * Generate a private key.
     * @param party Alice or Bob.
     * @return Generated key pair.
     * @throws GeneralSecurityException Thrown in case cryptography fails.
     */
    public SidhPrivateKeyOpti GeneratePrivateKeyOpti(Party party) {
        byte[] s = randomGenerator.GenerateRandomBytes(sikeParam.GetMessageBytes());
        BigInteger randomKey = GenerateRandomKey(sikeParam, party);
        return new SidhPrivateKeyOpti(sikeParam, party, randomKey, s);
    }
    
    /**
     * Generate a key pair.
     * @param party Alice or Bob.
     * @return Generated key pair.
     * @throws GeneralSecurityException Thrown in case cryptography fails.
     */
    public SidhKeyPairOpti GenerateKeyPairOpti(Party party) {
        var privateKey = GeneratePrivateKeyOpti(party);
        var publicKey = DerivePublicKey(party, privateKey);
        return new SidhKeyPairOpti(publicKey, privateKey);
    }

    /**
     * Generate a private key.
     * @param party Alice or Bob.
     * @return Generated key pair.
     * @throws GeneralSecurityException Thrown in case cryptography fails.
     */
    public SidhPrivateKeyRef GeneratePrivateKeyRef(Party party) {
        byte[] s = randomGenerator.GenerateRandomBytes(sikeParam.GetMessageBytes());
        BigInteger randomKey = GenerateRandomKey(sikeParam, party);
        return new SidhPrivateKeyRef(sikeParam, party, randomKey, s);
    }

    /**
     * Derive public key from a private key.
     * @param party Alice or Bob.
     * @param privateKey Private key.
     * @return Derived public key.
     * @throws InvalidKeyException Thrown in case key derivation fails.
     */
    public SidhPublicKeyRef DerivePublicKey(Party party, SidhPrivateKeyRef priv) {
        var curve = new MontgomeryCurve(sikeParam, sikeParam.GetA(), sikeParam.GetB());

        if (party == Party.ALICE) {
            return sikeParam.GetIsogeny().IsoGen2(curve, priv);
        } else if (party == Party.BOB) {
            return sikeParam.GetIsogeny().IsoGen3(curve, priv);
        }
        throw new Exception("Invalid party");
    }
    
    /**
     * Derive public key from a private key.
     * @param party Alice or Bob.
     * @param privateKey Private key.
     * @return Derived public key.
     * @throws InvalidKeyException Thrown in case key derivation fails.
     */
    public SidhPublicKeyOpti DerivePublicKey(Party party, SidhPrivateKeyOpti priv) {
        var curve = new MontgomeryCurveOpti(sikeParam, sikeParam.GetAOpti());

        if (party == Party.ALICE) {
            return sikeParam.GetIsogenyOpti().IsoGen2(curve, priv);
        } else if (party == Party.BOB) {
            return sikeParam.GetIsogenyOpti().IsoGen3(curve, priv);
        }
        throw new Exception("Invalid party");
    }

    /**
     * Generate a random key.
     * @param sikeParam SIKE parameters.
     * @return Random BigInteger usable as a private key.
     * @throws GeneralSecurityException Thrown in case cryptography fails.
     */
    private BigInteger GenerateRandomKey(SikeParam sikeParam, Party party) {
        if (party == Party.ALICE) {
            // random value in [0, 2^eA - 1]
            int length = (sikeParam.GetBitsA() + 7) / 8;
            byte[] randomBytes = randomGenerator.GenerateRandomBytes(length);
            randomBytes[randomBytes.Length - 1] &= sikeParam.GetMaskA();
            return ByteEncoding.FromByteArray(randomBytes);
        }
        if (party == Party.BOB) {
            // random value in [0, 2^Floor(Log(2,3^eB)) - 1]
            int length = (sikeParam.GetBitsB() - 1 + 7) / 8;
            byte[] randomBytes = randomGenerator.GenerateRandomBytes(length);
            randomBytes[randomBytes.Length - 1] &= sikeParam.GetMaskB();
            return ByteEncoding.FromByteArray(randomBytes);
        }
        throw new Exception("Invalid party");
    }

}
