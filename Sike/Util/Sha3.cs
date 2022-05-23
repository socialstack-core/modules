using Org.BouncyCastle.Crypto.Digests;

namespace Lumity.SikeIsogeny;


/**
 * SHA-3 hash function SHAKE256 with variable output length.
 *
 * @author Roman Strobl, roman.strobl@wultra.com
 */
public class Sha3 {

    private Sha3() {
        
    }

    /**
     * Hash data using SHAKE256.
     * @param data Data to hash.
     * @param outputLen Output length.
     * @return Hashed data.
     */
    public static byte[] Shake256(byte[] data, int outputLen) {
        var shake256 = new ShakeDigest(256);
        shake256.BlockUpdate(data, 0, data.Length);
        byte[] hashed = new byte[outputLen];
        // Squeeze output to required message length
        shake256.DoFinal(hashed, 0, outputLen);
        return hashed;
    }
}
