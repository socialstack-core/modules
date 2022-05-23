using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using System;

namespace Lumity.SikeIsogeny;

/**
 * SIKE key encapsulation.
 *
 * @author Roman Strobl, roman.strobl@wultra.com
 */
public class Sike {

    private SikeParam sikeParam;
    private RandomGenerator randomGenerator;
    private KeyGenerator keyGenerator;
    private Sidh sidh;

    /**
     * SIKE key encapsulation constructor.
     * @param sikeParam SIKE parameters.
     */
    public Sike(SikeParam sikeParam) {
        this.sikeParam = sikeParam;
        this.randomGenerator = new RandomGenerator();
        keyGenerator = new KeyGenerator(sikeParam);
        sidh = new Sidh(sikeParam);
    }

    /**
     * SIKE key encapsulation constructor with specified SecureRandom.
     * @param sikeParam SIKE parameters.
     * @param secureRandom SecureRandom to use.
     */
    public Sike(SikeParam sikeParam, SecureRandom secureRandom) {
        this.sikeParam = sikeParam;
        this.randomGenerator = new RandomGenerator(secureRandom);
        keyGenerator = new KeyGenerator(sikeParam, randomGenerator);
        sidh = new Sidh(sikeParam);
    }

    /**
     * SIKE encapsulation.
     * @param pk3 Bob's public key.
     * @return Encapsulation result with shared secret and encrypted message.
     * @throws GeneralSecurityException Thrown in case cryptography fails.
     */
    public EncapsulationResultRef Encapsulate(SidhPublicKeyRef pk3) {
        byte[] m = randomGenerator.GenerateRandomBytes(sikeParam.GetMessageBytes());
        byte[] r = GenerateR(m, pk3.GetEncoded());
        var encrypted = Encrypt(pk3, m, r);
        var c0Key = encrypted.GetC0();
        byte[] k = GenerateK(m, c0Key.GetEncoded(), encrypted.GetC1());
        return new EncapsulationResultRef(k, encrypted);
    }
    
    /**
     * SIKE encapsulation.
     * @param pk3 Bob's public key.
     * @return Encapsulation result with shared secret and encrypted message.
     * @throws GeneralSecurityException Thrown in case cryptography fails.
     */
    public EncapsulationResultOpti Encapsulate(SidhPublicKeyOpti pk3) {
        byte[] m = randomGenerator.GenerateRandomBytes(sikeParam.GetMessageBytes());
        byte[] r = GenerateR(m, pk3.GetEncoded());
        var encrypted = Encrypt(pk3, m, r);
        var c0Key = encrypted.GetC0();
        byte[] k = GenerateK(m, c0Key.GetEncoded(), encrypted.GetC1());
        return new EncapsulationResultOpti(k, encrypted);
    }

    /**
     * SIKE decapsulation.
     * @param sk3 Bob's private key.
     * @param pk3 Bob's public key.
     * @param encrypted Encrypted message received from Alice.
     * @return Shared secret.
     * @throws GeneralSecurityException Thrown in case cryptography fails.
     */
    public byte[] Decapsulate(SidhPrivateKeyRef sk3, SidhPublicKeyRef pk3, EncryptedMessageRef encrypted) {
        if (encrypted == null) {
            throw new InvalidParameterException("Encrypted message is null");
        }
        if (encrypted.GetC0() == null) {
            throw new InvalidParameterException("Invalid parameter c0");
        }
        if (encrypted.GetC1() == null) {
            throw new InvalidParameterException("Invalid parameter c1");
        }
        var priv3 = sk3;
        if (priv3.GetS() == null) {
            throw new InvalidParameterException("Private key cannot be used for decapsulation");
        }
        byte[] m = Decrypt(sk3, encrypted);
        byte[] r = GenerateR(m, pk3.GetEncoded());
        BigInteger modulo = SideChannelUtil.BigIntegerTwo.Pow(sikeParam.GetEA());
        BigInteger key = ByteEncoding.FromByteArray(r).Mod(modulo);
        var rKey = new SidhPrivateKeyRef(sikeParam, Party.ALICE, key);
        var c0Key = keyGenerator.DerivePublicKey(Party.ALICE, rKey);
        byte[] k;
        // The public key equals method runs in constant time
        if (c0Key.Equals(encrypted.GetC0())) {
            k = GenerateK(m, c0Key.GetEncoded(), encrypted.GetC1());
        } else {
            k = GenerateK(priv3.GetS(), c0Key.GetEncoded(), encrypted.GetC1());
        }
        return k;
    }
    
    /**
     * SIKE decapsulation.
     * @param sk3 Bob's private key.
     * @param pk3 Bob's public key.
     * @param encrypted Encrypted message received from Alice.
     * @return Shared secret.
     * @throws GeneralSecurityException Thrown in case cryptography fails.
     */
    public byte[] Decapsulate(SidhPrivateKeyOpti sk3, SidhPublicKeyOpti pk3, EncryptedMessageOpti encrypted) {
        if (encrypted == null) {
            throw new InvalidParameterException("Encrypted message is null");
        }
        if (encrypted.GetC0() == null) {
            throw new InvalidParameterException("Invalid parameter c0");
        }
        if (encrypted.GetC1() == null) {
            throw new InvalidParameterException("Invalid parameter c1");
        }
        var priv3 = sk3;
        if (priv3.GetS() == null) {
            throw new InvalidParameterException("Private key cannot be used for decapsulation");
        }
        byte[] m = Decrypt(sk3, encrypted);
        byte[] r = GenerateR(m, pk3.GetEncoded());
        BigInteger modulo = SideChannelUtil.BigIntegerTwo.Pow(sikeParam.GetEA());
        BigInteger key = ByteEncoding.FromByteArray(r).Mod(modulo);
        var rKey = new SidhPrivateKeyOpti(sikeParam, Party.ALICE, key);
        var c0Key = keyGenerator.DerivePublicKey(Party.ALICE, rKey);
        byte[] k;
        // The public key equals method runs in constant time
        if (c0Key.Equals(encrypted.GetC0())) {
            k = GenerateK(m, c0Key.GetEncoded(), encrypted.GetC1());
        } else {
            k = GenerateK(priv3.GetS(), c0Key.GetEncoded(), encrypted.GetC1());
        }
        return k;
    }

    /**
     * Encrypt a message.
     * @param pk3 Bob's public key.
     * @param m Message to encrypt, the message size must correspond to the SIKE parameter messageBytes.
     * @return Encrypted message.
     * @throws GeneralSecurityException Thrown in case cryptography fails.
     */
    public EncryptedMessageRef Encrypt(SidhPublicKeyRef pk3, byte[] m) {
        return Encrypt(pk3, m, null);
    }
    
    /**
     * Encrypt a message.
     * @param pk3 Bob's public key.
     * @param m Message to encrypt, the message size must correspond to the SIKE parameter messageBytes.
     * @return Encrypted message.
     * @throws GeneralSecurityException Thrown in case cryptography fails.
     */
    public EncryptedMessageOpti Encrypt(SidhPublicKeyOpti pk3, byte[] m) {
        return Encrypt(pk3, m, null);
    }

    /**
     * Encrypt a message.
     * @param pk3 Bob's public key.
     * @param m Message to encrypt, the message size must correspond to the SIKE parameter messageBytes.
     * @param r Optional byte representation of Alice's private key used in SIKE encapsulation.
     * @return Encrypted message.
     * @throws GeneralSecurityException Thrown in case cryptography fails.
     */
    private EncryptedMessageRef Encrypt(SidhPublicKeyRef pk3, byte[] m, byte[] r) {
        if (m == null || m.Length != sikeParam.GetMessageBytes()) {
            throw new InvalidParameterException("Invalid message");
        }
        SidhPrivateKeyRef sk2;
        if (r == null) {
            // Generate ephemeral private key
            sk2 = keyGenerator.GeneratePrivateKeyRef(Party.ALICE);
        } else {
            // Convert value r into private key
            BigInteger modulo = SideChannelUtil.BigIntegerTwo.Pow(sikeParam.GetEA());
            BigInteger key = ByteEncoding.FromByteArray(r).Mod(modulo);
            sk2 = new SidhPrivateKeyRef(sikeParam, Party.ALICE, key);
        }
        var c0 = keyGenerator.DerivePublicKey(Party.ALICE, sk2);
        var j = sidh.GenerateSharedSecret(Party.ALICE, sk2, pk3);
        byte[] h = Sha3.Shake256(j.GetEncoded(), sikeParam.GetMessageBytes());
        byte[] c1 = new byte[sikeParam.GetMessageBytes()];
        for (int i = 0; i < sikeParam.GetMessageBytes(); i++) {
            c1[i] = (byte) (h[i] ^ m[i]);
        }
        return new EncryptedMessageRef(c0, c1);
    }
    
    /**
     * Encrypt a message.
     * @param pk3 Bob's public key.
     * @param m Message to encrypt, the message size must correspond to the SIKE parameter messageBytes.
     * @param r Optional byte representation of Alice's private key used in SIKE encapsulation.
     * @return Encrypted message.
     * @throws GeneralSecurityException Thrown in case cryptography fails.
     */
    private EncryptedMessageOpti Encrypt(SidhPublicKeyOpti pk3, byte[] m, byte[] r) {
        if (m == null || m.Length != sikeParam.GetMessageBytes()) {
            throw new InvalidParameterException("Invalid message");
        }
        SidhPrivateKeyOpti sk2;
        if (r == null) {
            // Generate ephemeral private key
            sk2 = keyGenerator.GeneratePrivateKeyOpti(Party.ALICE);
        } else {
            // Convert value r into private key
            BigInteger modulo = SideChannelUtil.BigIntegerTwo.Pow(sikeParam.GetEA());
            BigInteger key = ByteEncoding.FromByteArray(r).Mod(modulo);
            sk2 = new SidhPrivateKeyOpti(sikeParam, Party.ALICE, key);
        }
        var c0 = keyGenerator.DerivePublicKey(Party.ALICE, sk2);
        var j = sidh.GenerateSharedSecret(Party.ALICE, sk2, pk3);
        byte[] h = Sha3.Shake256(j.GetEncoded(), sikeParam.GetMessageBytes());
        byte[] c1 = new byte[sikeParam.GetMessageBytes()];
        for (int i = 0; i < sikeParam.GetMessageBytes(); i++) {
            c1[i] = (byte) (h[i] ^ m[i]);
        }
        return new EncryptedMessageOpti(c0, c1);
    }

    /**
     * Decrypt a message.
     * @param sk3 Bob's private key.
     * @param encrypted Encrypted message received from Alice.
     * @return Decrypted message.
     * @throws GeneralSecurityException Thrown in case cryptography fails.
     */
    public byte[] Decrypt(SidhPrivateKeyRef sk3, EncryptedMessageRef encrypted) {
        if (encrypted == null) {
            throw new InvalidParameterException("Encrypted message is null");
        }
        var c0 = encrypted.GetC0();
        byte[] c1 = encrypted.GetC1();
        if (c1 == null) {
            throw new InvalidParameterException("Invalid parameter c1");
        }
        var j = sidh.GenerateSharedSecret(Party.BOB, sk3, c0);
        byte[] h = Sha3.Shake256(j.GetEncoded(), sikeParam.GetMessageBytes());
        byte[] m = new byte[sikeParam.GetMessageBytes()];
        for (int i = 0; i < sikeParam.GetMessageBytes(); i++) {
            m[i] = (byte) (h[i] ^ c1[i]);
        }
        return m;
    }
    
    /**
     * Decrypt a message.
     * @param sk3 Bob's private key.
     * @param encrypted Encrypted message received from Alice.
     * @return Decrypted message.
     * @throws GeneralSecurityException Thrown in case cryptography fails.
     */
    public byte[] Decrypt(SidhPrivateKeyOpti sk3, EncryptedMessageOpti encrypted) {
        if (encrypted == null) {
            throw new InvalidParameterException("Encrypted message is null");
        }
        var c0 = encrypted.GetC0();
        byte[] c1 = encrypted.GetC1();
        if (c1 == null) {
            throw new InvalidParameterException("Invalid parameter c1");
        }
        var j = sidh.GenerateSharedSecret(Party.BOB, sk3, c0);
        byte[] h = Sha3.Shake256(j.GetEncoded(), sikeParam.GetMessageBytes());
        byte[] m = new byte[sikeParam.GetMessageBytes()];
        for (int i = 0; i < sikeParam.GetMessageBytes(); i++) {
            m[i] = (byte) (h[i] ^ c1[i]);
        }
        return m;
    }

    /**
     * Generate the ephemeral private key r.
     * @param m Nonce.
     * @param pk3Enc Public key pk3 encoded in bytes.
     * @return Ephemeral private key r encoded in bytes.
     */
    private byte[] GenerateR(byte[] m, byte[] pk3Enc) {
        byte[] dataR = new byte[(m.Length + pk3Enc.Length)];
        Array.Copy(m, 0, dataR, 0, m.Length);
        Array.Copy(pk3Enc, 0, dataR, m.Length, pk3Enc.Length);
        return Sha3.Shake256(dataR, (sikeParam.GetBitsA() + 7) / 8);
    }

    /**
     * Generate the shared secret K.
     * @param m Nonce.
     * @param c0 Public key bytes.
     * @param c1 Encrypted message bytes.
     * @return Shared secret bytes.
     */
    private byte[] GenerateK(byte[] m, byte[] c0, byte[] c1) {
        byte[] dataK = new byte[(m.Length + c0.Length + c1.Length)];
        Array.Copy(m, 0, dataK, 0, m.Length);
        Array.Copy(c0, 0, dataK, m.Length, c0.Length);
        Array.Copy(c1, 0, dataK, m.Length + c0.Length, c1.Length);
        return Sha3.Shake256(dataK, sikeParam.GetCryptoBytes());
    }

}
