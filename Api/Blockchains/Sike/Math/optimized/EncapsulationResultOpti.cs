namespace Lumity.SikeIsogeny;

/**
 * SIKE encapsulation result.
 *
 * @author Roman Strobl, roman.strobl@wultra.com
 */
public class EncapsulationResultOpti {

    private byte[] secret;
    private EncryptedMessageOpti encryptedMessage;

    /**
     * SIKE encapsulation result constructor.
     * @param secret Shared secret.
     * @param encryptedMessage Encrypted message to be sent to Bob.
     */
    public EncapsulationResultOpti(byte[] secret, EncryptedMessageOpti encryptedMessage) {
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
    public EncryptedMessageOpti GetEncryptedMessage() {
        return encryptedMessage;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="that"></param>
    /// <returns></returns>
    public bool Equals(EncapsulationResultOpti that) {
        if (this == that) return true;
        // Use constant time comparison to avoid timing attacks
        return SideChannelUtil.ConstantTimeAreEqual(secret, that.secret) &
                encryptedMessage.Equals(that.encryptedMessage);
    }
}
