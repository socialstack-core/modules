using Org.BouncyCastle.Math;
using System;

namespace Lumity.SikeIsogeny;

/**
 * SIDH or SIKE private key.
 *
 * @author Roman Strobl, roman.strobl@wultra.com
 */
public class SidhPrivateKeyRef {

    private SikeParam sikeParam;
    private Party party;
    private byte[] key;
    private byte[] s;

    /**
     * Construct private key from a number.
     * @param sikeParam SIKE parameters.
     * @param party Alice or Bob.
     * @param secret Secret value of the private key.
     */
    public SidhPrivateKeyRef(SikeParam sikeParam, Party party, BigInteger secret) {
        this.sikeParam = sikeParam;
        this.party = party;
        ValidatePrivateKey(secret);
        int keyLength = (sikeParam.GetPrime().BitLength + 7) / 8;
        this.key = ByteEncoding.ToByteArray(secret, keyLength);
        this.s = new byte[sikeParam.GetMessageBytes()];
    }

    /**
     * Construct private key from bytes.
     * @param sikeParam SIKE parameters.
     * @param party Alice or Bob.
     * @param bytes Byte value of the private key.
     */
    public SidhPrivateKeyRef(SikeParam sikeParam, Party party, byte[] bytes) {
        this.sikeParam = sikeParam;
        this.party = party;
        int sLength = sikeParam.GetMessageBytes();
        int keyLength = (sikeParam.GetPrime().BitLength + 7) / 8;
        if (bytes == null || bytes.Length != sLength + keyLength) {
            throw new Exception("Invalid private key");
        }
        byte[] s = new byte[sLength];
        key = new byte[keyLength];
        Array.Copy(bytes, 0, s, 0, sLength);
        Array.Copy(bytes, sLength, key, 0, keyLength);
        BigInteger secret = ByteEncoding.FromByteArray(key);
        ValidatePrivateKey(secret);
        this.s = s;
    }

    /**
     * Construct private key from octets.
     * @param sikeParam SIKE parameters.
     * @param party Alice or Bob.
     * @param octets Octet value of the private key.
     */
    public SidhPrivateKeyRef(SikeParam sikeParam, Party party, string octets) {
        this.sikeParam = sikeParam;
        this.party = party;
        int sLength = sikeParam.GetMessageBytes();
        int keyLength = GetKeyLength(party);
        if (octets == null || octets.Length != (sLength + keyLength) * 2) {
            throw new Exception("Invalid private key");
        }
        byte[] bytes = System.Text.Encoding.ASCII.GetBytes(octets);
        byte[] s = new byte[sLength * 2];
        byte[] key = new byte[keyLength * 2];
        Array.Copy(bytes, 0, s, 0, sLength * 2);
        Array.Copy(bytes, sLength * 2, key, 0, keyLength * 2);
        BigInteger sVal = OctetEncoding.FromOctetString(System.Text.Encoding.ASCII.GetString(s));
        BigInteger secret = OctetEncoding.FromOctetString(System.Text.Encoding.ASCII.GetString(key));
        ValidatePrivateKey(secret);
        this.s = ByteEncoding.ToByteArray(sVal, sLength);
        int primeSize = (sikeParam.GetPrime().BitLength + 7) / 8;
        this.key = ByteEncoding.ToByteArray(secret, primeSize);
    }

    /**
     * Construct private key from bytes with specified parameter s for SIKE decapsulation.
     * @param sikeParam SIKE parameters.
     * @param party Alice or Bob.
     * @param key Byte value of the private key.
     * @param s Parameter s for SIKE decapsulation.
     */
    public SidhPrivateKeyRef(SikeParam sikeParam, Party party, BigInteger key, byte[] s) : this(sikeParam, party, key)
    {
        Array.Copy(s, 0, this.s, 0, s.Length);
    }

    /**
     * Validate the BigInteger value representing the private key.
     * @param secret BigInteger value representing the private key.
     */
    private void ValidatePrivateKey(BigInteger secret) {
        if (secret.CompareTo(BigInteger.Zero) <= 0) {
            throw new Exception("Invalid secret");
        }
        if (party == Party.ALICE) {
            if (secret.CompareTo(sikeParam.GetOrdA()) >= 0) {
                throw new Exception("Invalid secret");
            }
        } else if (party == Party.BOB) {
            if (secret.CompareTo(sikeParam.GetOrdB()) >= 0) {
                throw new Exception("Invalid secret");
            }
        } else {
            throw new Exception("Invalid party");
        }
    }

    /**
     * Get the private key length.
     * @param party Alice or Bob.
     * @return Private key length.
     */
    private int GetKeyLength(Party party) {
        if (party == Party.ALICE) {
            return (sikeParam.GetBitsA() + 7) / 8;
        } else if (party == Party.BOB){
            return (sikeParam.GetBitsB() - 1 + 7) / 8;
        } else {
            throw new Exception("Invalid party");
        }
    }

    /**
     * Get the private key as byte array.
     * @return Private key as byte array.
     */
    public byte[] GetKey() {
        byte[] keyBytes = new byte[key.Length];
        Array.Copy(key, 0, keyBytes, 0, key.Length);
        return keyBytes;
    }

    /**
     * Get the private key as an F(p) element.
     * @return Private key as an F(p) element.
     */
    public FpElementRef GetFpElement() {
        BigInteger secret = ByteEncoding.FromByteArray(key);
        return sikeParam.GetFp2ElementFactory().Generate(secret).GetX0();
    }

    /**
     * Get the private key as a number.
     * @return Private key as a number.
     */
    public BigInteger GetM() {
        return ByteEncoding.FromByteArray(key);
    }

    /**
     * Get parameter s for decapsulation.
     * @return Parameter s for decapsulation.
     */
    public byte[] GetS() {
        return s;
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
     * Get the private key encoded as bytes.
     * @return Private key encoded as bytes.
     */
    public byte[] GetEncoded() {
        byte[] output = new byte[s.Length + key.Length];
        Array.Copy(s, 0, output, 0, s.Length);
        Array.Copy(key, 0, output, s.Length, key.Length);
        return output;
    }

    /**
     * Convert private key into an octet string.
     * @return Octet string.
     */
    public string ToOctetString() {
        string prefix = OctetEncoding.ToOctetString(s, sikeParam.GetMessageBytes());
        int length = GetKeyLength(party);
        return prefix + OctetEncoding.ToOctetString(key, length);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public override string ToString() {
        return GetM().ToString();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="that"></param>
    /// <returns></returns>
    public bool Equals(SidhPrivateKeyRef that) {
        // Use constant time comparison to avoid timing attacks
        return sikeParam.Equals(that.sikeParam)
                && SideChannelUtil.ConstantTimeAreEqual(GetEncoded(), that.GetEncoded());
    }
}
