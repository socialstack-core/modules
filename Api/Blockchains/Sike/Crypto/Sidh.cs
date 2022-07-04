using System;

namespace Lumity.SikeIsogeny;

/**
 * SIDH key exchange.
 *
 * @author Roman Strobl, roman.strobl@wultra.com
 */
public class Sidh {

    private SikeParam sikeParam;

    /**
     * SIDH key exchange constructor.
     * @param sikeParam SIKE parameters.
     */
    public Sidh(SikeParam sikeParam) {
        this.sikeParam = sikeParam;
    }

    /**
     * Generate a shared secret isogeny j-invariant.
     * @param party Alice or Bob.
     * @param privateKey Private key.
     * @param publicKey Public key.
     * @return Shared secret isogeny j-invariant.
     * @throws GeneralSecurityException Thrown in case cryptography fails.
     */
    public Fp2ElementRef GenerateSharedSecret(Party party, SidhPrivateKeyRef priv, SidhPublicKeyRef pub) {

        if (party == Party.ALICE) {
            return sikeParam.GetIsogeny().IsoEx2(sikeParam, priv.GetKey(), pub.GetPx(), pub.GetQx(), pub.GetRx());
        }
        if (party == Party.BOB) {
            return sikeParam.GetIsogeny().IsoEx3(sikeParam, priv.GetKey(), pub.GetPx(), pub.GetQx(), pub.GetRx());
        }
        throw new Exception("Invalid party");
    }

    /**
     * Generate a shared secret isogeny j-invariant.
     * @param party Alice or Bob.
     * @param privateKey Private key.
     * @param publicKey Public key.
     * @return Shared secret isogeny j-invariant.
     * @throws GeneralSecurityException Thrown in case cryptography fails.
     */
    public Fp2ElementOpti GenerateSharedSecret(Party party, SidhPrivateKeyOpti priv, SidhPublicKeyOpti pub) {

        if (party == Party.ALICE) {
            return sikeParam.GetIsogenyOpti().IsoEx2(sikeParam, priv.GetKey(), pub.GetPx(), pub.GetQx(), pub.GetRx());
        }
        if (party == Party.BOB) {
            return sikeParam.GetIsogenyOpti().IsoEx3(sikeParam, priv.GetKey(), pub.GetPx(), pub.GetQx(), pub.GetRx());
        }
        throw new Exception("Invalid party");
    }
}
