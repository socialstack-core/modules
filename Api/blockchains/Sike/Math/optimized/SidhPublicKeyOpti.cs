using Org.BouncyCastle.Math;
using Org.BouncyCastle.Utilities;
using System;

namespace Lumity.SikeIsogeny;

/**
 * SIDH or SIKE public key.
 *
 * @author Roman Strobl, roman.strobl@wultra.com
 */
public class SidhPublicKeyOpti {

    private SikeParam sikeParam;

    private Fp2ElementOpti px;
    private Fp2ElementOpti qx;
    private Fp2ElementOpti rx;

    /**
     * Public key constructor from F(p^2) Elements.
     * @param sikeParam SIKE parameters.
     * @param px The x coordinate of public point P.
     * @param qx The x coordinate of public point Q.
     * @param rx The x coordinate of public point R.
     */
    public SidhPublicKeyOpti(SikeParam sikeParam, Fp2ElementOpti px, Fp2ElementOpti qx, Fp2ElementOpti rx) {
        this.sikeParam = sikeParam;
        this.px = px;
        this.qx = qx;
        this.rx = rx;
    }

    /**
     * Public key constructor from byte array representation.
     * @param sikeParam SIKE parameters.
     * @param bytes The x coordinates of public points P, Q and R.
     */
    public SidhPublicKeyOpti(SikeParam sikeParam, byte[] bytes) {
        this.sikeParam = sikeParam;
        BigInteger prime = sikeParam.GetPrime();
        int primeSize = (prime.BitLength + 7) / 8;
        if (bytes == null || bytes.Length != 6 * primeSize) {
            throw new Exception("Invalid public key");
        }
        BigInteger[] keyParts = new BigInteger[6];
        for (int i = 0; i < 6; i++) {
            byte[] keyBytes = new byte[primeSize];
            Array.Copy(bytes, i * primeSize, keyBytes, 0, keyBytes.Length);
            keyParts[i] = ByteEncoding.FromByteArray(keyBytes);
        }
        this.px = sikeParam.GetFp2ElementFactoryOpti().Generate(keyParts[0], keyParts[1]);
        this.qx = sikeParam.GetFp2ElementFactoryOpti().Generate(keyParts[2], keyParts[3]);
        this.rx = sikeParam.GetFp2ElementFactoryOpti().Generate(keyParts[4], keyParts[5]);
    }

    /**
     * Construct public key from octets.
     * @param sikeParam SIKE parameters.
     * @param octets Octet value of the private key.
     */
    public SidhPublicKeyOpti(SikeParam sikeParam, string octets) {
        this.sikeParam = sikeParam;
        BigInteger prime = sikeParam.GetPrime();
        int primeSize = (prime.BitLength + 7) / 8;
        if (octets == null || octets.Length != 12 * primeSize) {
            throw new Exception("Invalid public key");
        }
        byte[] octetBytes = System.Text.Encoding.ASCII.GetBytes(octets);
        BigInteger[] keyParts = new BigInteger[6];
        for (int i = 0; i < 6; i++) {
            byte[] keyBytes = new byte[primeSize * 2];
            Array.Copy(octetBytes, i * primeSize * 2, keyBytes, 0, keyBytes.Length);
            keyParts[i] = OctetEncoding.FromOctetString(System.Text.Encoding.ASCII.GetString(keyBytes));
        }
        this.px = sikeParam.GetFp2ElementFactoryOpti().Generate(keyParts[0], keyParts[1]);
        this.qx = sikeParam.GetFp2ElementFactoryOpti().Generate(keyParts[2], keyParts[3]);
        this.rx = sikeParam.GetFp2ElementFactoryOpti().Generate(keyParts[4], keyParts[5]);
    }

    /**
     * Get the x coordinate of public point P.
     * @return The x coordinate of public point P.
     */
    public Fp2ElementOpti GetPx() {
        return px;
    }

    /**
     * The x coordinate of public point Q.
     * @return The x coordinate of public point Q.
     */
    public Fp2ElementOpti GetQx() {
        return qx;
    }

    /**
     * The x coordinate of public point R.
     * @return The x coordinate of public point R.
     */
    public Fp2ElementOpti GetRx() {
        return rx;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public string GetAlgorithm() {
        return sikeParam.GetName();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public string GetFormat() {
        // ASN.1 encoding is not supported
        return null;
    }

    /**
     * Get the public key encoded as bytes.
     * @return Public key encoded as bytes.
     */
    public byte[] GetEncoded() {
        byte[] pxEncoded = px.GetEncoded();
        byte[] qxEncoded = qx.GetEncoded();
        byte[] rxEncoded = rx.GetEncoded();
        byte[] encoded = new byte[pxEncoded.Length + qxEncoded.Length + rxEncoded.Length];
        Array.Copy(pxEncoded, 0, encoded, 0, pxEncoded.Length);
        Array.Copy(qxEncoded, 0, encoded, pxEncoded.Length, qxEncoded.Length);
        Array.Copy(rxEncoded, 0, encoded, pxEncoded.Length + qxEncoded.Length, rxEncoded.Length);
        return encoded;
    }

    /**
     * Convert public key to octet string.
     * @return Octet string.
     */
    public string ToOctetString() {
        return px.ToOctetString() + qx.ToOctetString() + rx.ToOctetString();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public override string ToString() {
        return "(" + px.ToString() + ", " + qx.ToString() + ", " + rx.ToString() + ")";
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="that"></param>
    /// <returns></returns>
    public bool Equals(SidhPublicKeyOpti that) {
        if (this == that) return true;
        // Use constant time comparison to avoid timing attacks
        return sikeParam.Equals(that.sikeParam)
                && Arrays.ConstantTimeAreEqual(GetEncoded(), that.GetEncoded());
    }
}
