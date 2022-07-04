namespace Lumity.SikeIsogeny;

/**
 * SIKE encapsulation result.
 *
 * @author Roman Strobl, roman.strobl@wultra.com
 */
public class EncapsulationResultRef {

    private byte[] secret;
    private EncryptedMessageRef encryptedMessage;

    /**
     * SIKE encapsulation result constructor.
     * @param secret Shared secret.
     * @param encryptedMessage Encrypted message to be sent to Bob.
     */
    public EncapsulationResultRef(byte[] secret, EncryptedMessageRef encryptedMessage) {
        this.secret = secret;
        this.encryptedMessage = encryptedMessage;
    }

    /**
     * Get the shared secret.
     * @return Shared secret.
     */
    public byte[] GetSecret() {
        return secret;
    }

    /**
     * Get the encrypted message.
     * @return Encypted message.
     */
    public EncryptedMessageRef GetEncryptedMessage() {
        return encryptedMessage;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="that"></param>
    /// <returns></returns>
    public bool Equals(EncapsulationResultRef that) {
        if (this == that) return true;
        // Use constant time comparison to avoid timing attacks
        return SideChannelUtil.ConstantTimeAreEqual(secret, that.secret) &
                encryptedMessage.Equals(that.encryptedMessage);
    }
}
