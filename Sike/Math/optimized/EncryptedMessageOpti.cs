using System;

namespace Lumity.SikeIsogeny;

/**
 * SIKE encrypted message.
 *
 * @author Roman Strobl, roman.strobl@wultra.com
 */
public class EncryptedMessageOpti {

    private SidhPublicKeyOpti c0;
    private byte[] c1;

    /**
     * SIKE encrypted message constructor from public key and encrypted data.
     * @param c0 Alice's public key.
     * @param c1 Encrypted data.
     */
    public EncryptedMessageOpti(SidhPublicKeyOpti c0, byte[] c1) {
        this.c0 = c0;
        this.c1 = c1;
    }

    /**
     * SIKE encrypted message constructor from message encoded into byte array.
     * @param sikeParam SIKE parameters.
     * @param bytes Encrypted message encoded into byte array.
     */
    public EncryptedMessageOpti(SikeParam sikeParam, byte[] bytes) {
        if (sikeParam == null) {
            throw new Exception("Invalid parameter sikeParam");
        }
        int primeSize = (sikeParam.GetPrime().BitLength + 7) / 8;
        int pubKeySize = primeSize * 6;
        int messageSize = sikeParam.GetMessageBytes();
        int expectedSize = pubKeySize + messageSize;
        if (bytes == null || bytes.Length != expectedSize) {
            throw new Exception("Invalid parameter bytes");
        }
        byte[] pubKeyBytes = new byte[pubKeySize];
        Array.Copy(bytes, 0, pubKeyBytes, 0, pubKeySize);
        this.c0 = new SidhPublicKeyOpti(sikeParam, pubKeyBytes);
        this.c1 = new byte[messageSize];
        Array.Copy(bytes, pubKeySize, this.c1, 0, messageSize);
    }

    /**
     * Get encrypted message encoded into byte array.
     * @return Encrypted message encoded into byte array.
     */
    public byte[] GetEncoded() {
        if (c0 == null || c1 == null) {
            return null;
        }
        byte[] pubKey = c0.GetEncoded();
        byte[] encoded = new byte[pubKey.Length + c1.Length];
        Array.Copy(pubKey, 0, encoded, 0, pubKey.Length);
        Array.Copy(c1, 0, encoded, pubKey.Length, c1.Length);
        return encoded;
    }

    /**
     * Get Alice's public key.
     * @return Public key.
     */
    public SidhPublicKeyOpti GetC0() {
        return c0;
    }

    /**
     * Get encrypted data.
     * @return Encrypted data.
     */
    public byte[] GetC1() {
        return c1;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="that"></param>
    /// <returns></returns>
    public bool Equals(EncryptedMessageOpti that) {
        if (this == that) return true;
        // Use constant time comparison to avoid timing attacks
        return SideChannelUtil.ConstantTimeAreEqual(GetEncoded(), that.GetEncoded());
    }
}
