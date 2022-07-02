using Api.Signatures;
using Api.SocketServerLibrary;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Macs;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Crypto.Tls;
using Org.BouncyCastle.Math.EC.Rfc7748;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Api.WebRTC.Ciphers;


/// <summary>
/// Cipher used by TLS/ DTLS.
/// </summary>
public partial class Cipher
{
	/// <summary>
	/// Hash algorithm to use.
	/// </summary>
	public HashAlgorithm Hash;
	/// <summary>
	/// Key exchange algorithm to use.
	/// </summary>
	public KeyExchangeAlgorithm KeyExchange;
	/// <summary>
	/// Signature algorithm to use.
	/// </summary>
	public SignatureAlgorithm Signature;
	/// <summary>
	/// Encryption algorithm to use.
	/// </summary>
	public EncryptionAlgorithm Encryption;
	/// <summary>
	/// IANA name
	/// </summary>
	public string Name;
	/// <summary>
	/// ID of this cipher in TLS protocol
	/// </summary>
	public ushort TlsId;
	/// <summary>
	/// The lower this is, the more favourable the cipher is. Directly follows Mozillas current prio: https://wiki.mozilla.org/Security/Cipher_Suites
	/// </summary>
	public int Priority;

	/// <summary>
	/// True if this cipher has key exchange args (they all do).
	/// </summary>
	public bool HasKeyExchangeArgs;

	/// <summary>
	/// Creates a new cipher. Must set the various algorithms.
	/// </summary>
	/// <param name="ianaName"></param>
	/// <param name="tlsId"></param>
	/// <param name="priority"></param>
	public Cipher(string ianaName, ushort tlsId, int priority)
	{
		// Name is typically of the form TLS_{HANDSHAKE}_{SIGNATURE}_WITH_{ENCRYPTION-AND-ENCRYPTION-OPTIONS}_{HASH}
		TlsId = tlsId;
		Priority = priority;
		Name = ianaName;
	}

	/// <summary>
	/// Copies of this cipher which have been configured with the given numbered curve.
	/// </summary>
	private ConcurrentDictionary<ushort, Cipher> _curveVariants;

	/// <summary>
	/// Creates a clone of this cipher object.
	/// </summary>
	/// <returns></returns>
	public Cipher Copy()
	{
		return new Cipher(Name, TlsId, Priority) {
			HasKeyExchangeArgs = HasKeyExchangeArgs,
			Encryption = Encryption.Copy(),
			Hash = Hash.Copy(),
			KeyExchange = KeyExchange.Copy(),
			Signature = Signature.Copy()
		};
	}

	/// <summary>
	/// Gets a preallocated variant of this cipher using the given named curve for its handshake.
	/// </summary>
	/// <returns></returns>
	public Cipher GetCurveVariant(CurveInfo curve)
	{
		if (_curveVariants == null)
		{
			_curveVariants = new ConcurrentDictionary<ushort, Cipher>();
		}

		if (!_curveVariants.TryGetValue(curve.TlsId, out var cipher))
		{
			cipher = Copy();
			cipher.KeyExchange.SetCurve(curve);
			_curveVariants[curve.TlsId] = cipher;
		}

		return cipher;
	}

	/// <summary>
	/// Inits the cipher, setting up various important values like the key exchange length.
	/// </summary>
	public void Init()
	{
		// All the supported key exchange mechanisms do require the server_key_exchange mechanism, meaning they all have key exchange args.
		HasKeyExchangeArgs = true;
	}

	/// <summary>
	/// Get a certificate in the correct TLS format for this cipher. Cached and reused.
	/// </summary>
	/// <returns></returns>
	public Certificate GetCertificate(RtpClient client)
	{
		return Signature.GetCertificate(client);
	}

	/// <summary>
	/// Hash size for this cipher.
	/// </summary>
	public int HashSize => Hash.HashSize;
}



/// <summary>
/// 
/// </summary>
public partial class EncryptionAlgorithm
{
	/// <summary>
	/// Makes a clone of this object.
	/// </summary>
	public virtual EncryptionAlgorithm Copy()
	{
		throw new NotImplementedException();
	}

	/// <summary>
	/// Sets up the cipher for the given client.
	/// </summary>
	/// <param name="client"></param>
	/// <returns></returns>
	/// <exception cref="NotImplementedException"></exception>
	public virtual TlsCipher SetupCipher(RtpClient client)
	{
		throw new NotImplementedException();
	}
}

/// <summary>
/// AES_256_GCM
/// </summary>
public class Aes256Gcm : EncryptionAlgorithm
{
	/// <summary>
	/// Makes a clone of this object.
	/// </summary>
	public override EncryptionAlgorithm Copy()
	{
		return new Aes256Gcm();
	}

	/// <summary>
	/// Sets up the cipher for the given client.
	/// </summary>
	/// <param name="client"></param>
	/// <returns></returns>
	/// <exception cref="NotImplementedException"></exception>
	public override TlsCipher SetupCipher(RtpClient client)
	{
		// new BcTlsAeadCipherImpl(new GcmBlockCipher(new AesEngine()), true)
		return new Org.BouncyCastle.Crypto.Tls.TlsAeadCipher(
			new DTLS.DTLSContext(client),
			new CcmBlockCipher(new AesEngine()),
			new CcmBlockCipher(new AesEngine()),
			32,
			16
		);
	}
}

/// <summary>
/// AES_128_GCM
/// </summary>
public class Aes128Gcm : EncryptionAlgorithm
{
	/// <summary>
	/// Makes a clone of this object.
	/// </summary>
	public override EncryptionAlgorithm Copy()
	{
		return new Aes128Gcm();
	}

	/// <summary>
	/// Sets up the cipher for the given client.
	/// </summary>
	/// <param name="client"></param>
	/// <returns></returns>
	/// <exception cref="NotImplementedException"></exception>
	public override TlsCipher SetupCipher(RtpClient client)
	{
		return new Org.BouncyCastle.Crypto.Tls.TlsAeadCipher(
			new DTLS.DTLSContext(client),
			new GcmBlockCipher(new AesEngine()),
			new GcmBlockCipher(new AesEngine()),
			16,
			16
		);
	}

}

/// <summary>
/// CHACHA20_POLY1305
/// </summary>
public class ChaCha20Poly1305 : EncryptionAlgorithm
{
	/// <summary>
	/// Sets up the cipher for the given client.
	/// </summary>
	/// <param name="client"></param>
	/// <returns></returns>
	/// <exception cref="NotImplementedException"></exception>
	public override TlsCipher SetupCipher(RtpClient client)
	{
		return new Chacha20Poly1305(new DTLS.DTLSContext(client));
	}

	/// <summary>
	/// Makes a clone of this object.
	/// </summary>
	public override EncryptionAlgorithm Copy()
	{
		return new ChaCha20Poly1305();
	}
}

/// <summary>
/// Algorithm to use during the key exchange.
/// </summary>
public partial class KeyExchangeAlgorithm
{
	/// <summary>
	/// True if this handshake algorithm has curves.
	/// </summary>
	public bool HasCurves;

	/// <summary>
	/// Makes a clone of this object.
	/// </summary>
	public virtual KeyExchangeAlgorithm Copy()
	{
		throw new NotImplementedException();
	}

	/// <summary>
	/// Generates a private key for the key exchange.
	/// </summary>
	/// <returns></returns>
	/// <exception cref="NotImplementedException"></exception>
	public virtual KeyPair GeneratePrivateKey()
	{
		throw new NotImplementedException();
	}

	/// <summary>
	/// Writes the public key into the given writer.
	/// </summary>
	/// <param name="into"></param>
	/// <param name="privateKey"></param>
	public virtual void WritePublicKey(Writer into, KeyPair privateKey)
	{
		throw new NotImplementedException();
	}

	/// <summary>
	/// Sets the curve to use on this handshake algo.
	/// </summary>
	/// <param name="curve"></param>
	public virtual void SetCurve(CurveInfo curve)
	{
	}

	/// <summary>
	/// Calculates the ECDHE secret value (the agreement).
	/// </summary>
	/// <param name="client"></param>
	/// <param name="buffer"></param>
	/// <param name="pkStart"></param>
	/// <param name="pkLength"></param>
	/// <returns></returns>
	public virtual byte[] CalculateAgreement(RtpClient client, byte[] buffer, int pkStart, int pkLength)
	{
		throw new NotImplementedException();
	}

	/// <summary>
	/// Writes the key exchange args into the given writer using private data in the given client, as well as our own certificate.
	/// </summary>
	/// <param name="writer"></param>
	/// <param name="client"></param>
	/// <param name="certificate"></param>
	public virtual void WriteKeyExchangeArgs(Writer writer, RtpClient client, Certificate certificate)
	{
		throw new NotImplementedException();
	}
}

/// <summary>
/// The ECDHE key exchange algorithm.
/// </summary>
public class Ecdhe : KeyExchangeAlgorithm
{
	/// <summary>
	/// The curve to use. This is set by creating a "curve variant" of a given cipher 
	/// such that ultimately everything gets reused and a minimal amount of per-client memory usage happens.
	/// </summary>
	public CurveInfo Curve;

	/// <summary>
	/// Creates a basic ECDHE algorithm.
	/// </summary>
	public Ecdhe():base(){
		HasCurves = true;
	}

	/// <summary>
	/// Makes a clone of this object.
	/// </summary>
	public override KeyExchangeAlgorithm Copy()
	{
		return new Ecdhe() {
			Curve = Curve
		};
	}

	/// <summary>
	/// Writes the key exchange args into the given writer using private data in the given client, as well as our own certificate.
	/// </summary>
	/// <param name="writer"></param>
	/// <param name="client"></param>
	/// <param name="certificate"></param>
	public override void WriteKeyExchangeArgs(Writer writer, RtpClient client, Certificate certificate)
	{
		var paramsStart = writer.Length;
		writer.Write((byte)3); // Curve type, named curve (1)
		writer.WriteBE((ushort)Curve.TlsId); // The named curve ID
		writer.Write((byte)0); // PK length (1) - Populated after writing the PK
		var lengthBuff = writer.LastBuffer.Bytes;
		var fill = writer.CurrentFill - 1;

		// Write the public key for this session.
		var len = writer.Length;
		client.HandshakeMeta.DtlsPrivate = GeneratePrivateKey();
		WritePublicKey(writer, client.HandshakeMeta.DtlsPrivate);
		lengthBuff[fill] = (byte)(writer.Length - len);

		var paramsSize = writer.Length - paramsStart;
		
		// Signature algorithm:
		writer.WriteBE((ushort)Curve.SignatureAlgorithmTlsId);
		writer.WriteBE((ushort)0); // Signature length (2) - Populated after writing the signature
		lengthBuff = writer.LastBuffer.Bytes;
		fill = writer.CurrentFill - 2;

		len = writer.Length;
		SignExchange(writer, paramsStart, paramsSize, client, certificate);
		var pkLen = (ushort)(writer.Length - len);
		lengthBuff[fill] = (byte)(pkLen >> 8);
		lengthBuff[fill + 1] = (byte)pkLen;
	}

	/// <summary>
	/// Calculates the ECDHE secret value (the agreement).
	/// </summary>
	/// <param name="client"></param>
	/// <param name="buffer"></param>
	/// <param name="pkStart"></param>
	/// <param name="pkLength"></param>
	/// <returns></returns>
	public override byte[] CalculateAgreement(RtpClient client, byte[] buffer, int pkStart, int pkLength)
	{
		var peerPublic = new byte[pkLength];
		Array.Copy(buffer, pkStart, peerPublic, 0, pkLength);

		// Load the public key:
		ECPublicKeyParameters peerPublicKey = TlsEccUtilities.DeserializeECPublicKey(null, Curve.DomainParameters, peerPublic);
		var secret = TlsEccUtilities.CalculateECDHBasicAgreement(peerPublicKey, client.HandshakeMeta.DtlsPrivate.PrivateKey);

		return secret;
	}

	/// <summary>
	/// Signs an exchange into the given writer.
	/// </summary>
	/// <param name="writer"></param>
	/// <param name="client"></param>
	/// <param name="paramsStart"></param>
	/// <param name="paramsSize"></param>
	/// <param name="certificate"></param>
	/// <exception cref="NotImplementedException"></exception>
	public void SignExchange(Writer writer, int paramsStart, int paramsSize, RtpClient client, Certificate certificate)
	{
		// https://www.ietf.org/rfc/rfc4492.txt page 19
		// It is SHA(ClientHello.random + ServerHello.random + ServerKeyExchange.params)

		var digest = (IDigest)((Org.BouncyCastle.Utilities.IMemoable)(Curve.Digest)).Copy();
		digest.BlockUpdate(client.HandshakeMeta.DtlsRandom, 0, 64);

		// The params:
		digest.BlockUpdate(writer.LastBuffer.Bytes, paramsStart, paramsSize);

		byte[] hash = new byte[digest.GetDigestSize()];
		digest.DoFinal(hash, 0);

		var dsa = new ECDsaSigner(new HMacDsaKCalculator((IDigest)((Org.BouncyCastle.Utilities.IMemoable)(Curve.Digest)).Copy()));
		var signer = new DsaDigestSigner(dsa, new NullDigest());

		signer.Init(true, new ParametersWithRandom(certificate.PrivateKey, _rng));
		signer.BlockUpdate(hash, 0, hash.Length);
		var sig = signer.GenerateSignature();

		writer.WriteNoLength(sig);
	}

	/// <summary>
	/// The keypair generator to use.
	/// </summary>
	private Org.BouncyCastle.Crypto.Generators.ECKeyPairGenerator _generator;
	private static SecureRandom _rng = new SecureRandom();

	/// <summary>
	/// Generates a key pair.
	/// </summary>
	/// <returns></returns>
	private KeyPair Generate()
	{
		if(_generator == null)
		{
			var keyParams = new ECKeyGenerationParameters(Curve.DomainParameters, _rng);

			_generator = new Org.BouncyCastle.Crypto.Generators.ECKeyPairGenerator("ECDSA");
			_generator.Init(keyParams);
		}

		var keyPair = _generator.GenerateKeyPair();

		var privateKey = keyPair.Private as ECPrivateKeyParameters;
		var publicKey = keyPair.Public as ECPublicKeyParameters;

		return new KeyPair()
		{
			PrivateKeyBytes = privateKey.D.ToByteArrayUnsigned(),
			PublicKey = publicKey,
			PrivateKey = privateKey
		};
	}

	/// <summary>
	/// Generate a private key to use.
	/// </summary>
	/// <returns></returns>
	public override KeyPair GeneratePrivateKey()
	{
		return Generate();

		/*
		// x25519 private key
		var k = new byte[X25519.PointSize];
		RtpClient.Rng.GetBytes(k, 0, k.Length);
		return k;
		*/
	}

	/// <summary>
	/// Writes the public key into the given writer.
	/// </summary>
	/// <param name="into"></param>
	/// <param name="privateKey"></param>
	public override void WritePublicKey(Writer into, KeyPair privateKey)
	{
		/*
		byte[] publicKey = new byte[X25519.PointSize];
		X25519.ScalarMultBase(privateKey, 0, publicKey, 0);
		into.WriteNoLength(publicKey);
		*/
		var bytes = privateKey.PublicKey.Q.GetEncoded(false);
		into.WriteNoLength(bytes);
	}

	/// <summary>
	/// Sets the curve to use on this handshake algo.
	/// </summary>
	/// <param name="curve"></param>
	public override void SetCurve(CurveInfo curve)
	{
		Curve = curve;
	}
}

/// <summary>
/// The DHE key exchange algorithm.
/// </summary>
public class Dhe : KeyExchangeAlgorithm
{
	/// <summary>
	/// 
	/// </summary>
	public Dhe():base(){
		
	}

	/// <summary>
	/// Makes a clone of this object.
	/// </summary>
	public override KeyExchangeAlgorithm Copy()
	{
		return new Dhe();
	}
}




/// <summary>
/// 
/// </summary>
public partial class SignatureAlgorithm
{
	/// <summary>
	/// 
	/// </summary>
	public SignatureAlgorithm() {
	}

	/// <summary>
	/// Makes a clone of this object.
	/// </summary>
	public virtual SignatureAlgorithm Copy()
	{
		throw new NotImplementedException();
	}

	/// <summary>
	/// Gets a cert to use for the signing process.
	/// </summary>
	public virtual Certificate GetCertificate(RtpClient client)
	{
		throw new NotImplementedException();
	}
}

/// <summary>
/// 
/// </summary>
public class Ecdsa : SignatureAlgorithm
{
	/// <summary>
	/// 
	/// </summary>
	public Ecdsa():base(){
	}

	/// <summary>
	/// Makes a clone of this object.
	/// </summary>
	public override SignatureAlgorithm Copy()
	{
		return new Ecdsa();
	}

	/// <summary>
	/// Gets a cert to use for the signing process.
	/// </summary>
	public override Certificate GetCertificate(RtpClient client)
	{
		return client.HandshakeMeta.ServerCertificates.GetByType(false);
	}
	
}

/// <summary>
/// 
/// </summary>
public class Rsa : SignatureAlgorithm
{
	/// <summary>
	/// 
	/// </summary>
	public Rsa() : base() {
	}

	/// <summary>
	/// Makes a clone of this object.
	/// </summary>
	public override SignatureAlgorithm Copy()
	{
		return new Rsa();
	}

	/// <summary>
	/// Gets a cert to use for the signing process.
	/// </summary>
	public override Certificate GetCertificate(RtpClient client)
	{
		return client.HandshakeMeta.ServerCertificates.GetByType(true);
	}

}





/// <summary>
/// A hash generating algorithm.
/// </summary>
public partial class HashAlgorithm
{
	/// <summary>
	/// The size of the hash in bytes.
	/// </summary>
	public readonly int HashSize;

	/// <summary>
	/// Generates a PRF
	/// </summary>
	/// <param name="secret"></param>
	/// <param name="asciiLabel"></param>
	/// <param name="seed"></param>
	/// <param name="size"></param>
	/// <returns></returns>
	public static byte[] PRF(byte[] secret, string asciiLabel, byte[] seed, int size)
	{
		byte[] array = System.Text.Encoding.ASCII.GetBytes(asciiLabel);
		byte[] array2 = Concat(array, seed);
		var digest = new Sha256Digest();
		byte[] array3 = new byte[size];
		HMacHash(digest, secret, array2, array3);
		return array3;
	}

	private static void HMacHash(IDigest digest, byte[] secret, byte[] seed, byte[] output)
	{
		var hMac = new HMac(digest);
		hMac.Init(new KeyParameter(secret));
		byte[] array = seed;
		int digestSize = digest.GetDigestSize();
		int num = (output.Length + digestSize - 1) / digestSize;
		byte[] array2 = new byte[hMac.GetMacSize()];
		byte[] array3 = new byte[hMac.GetMacSize()];
		for (int i = 0; i < num; i++)
		{
			hMac.BlockUpdate(array, 0, array.Length);
			hMac.DoFinal(array2, 0);
			array = array2;
			hMac.BlockUpdate(array, 0, array.Length);
			hMac.BlockUpdate(seed, 0, seed.Length);
			hMac.DoFinal(array3, 0);
			Array.Copy(array3, 0, output, digestSize * i, System.Math.Min(digestSize, output.Length - digestSize * i));
		}
	}

	private static byte[] Concat(byte[] a, byte[] b)
	{
		byte[] c = new byte[a.Length + b.Length];
		Array.Copy(a, 0, c, 0, a.Length);
		Array.Copy(b, 0, c, a.Length, b.Length);
		return c;
	}


	/// <summary>
	/// Creates a new hash algo with the given hash size in bytes.
	/// </summary>
	/// <param name="hashSize"></param>
	public HashAlgorithm(int hashSize) {
		HashSize = hashSize;
	}

	/// <summary>
	/// Makes a clone of this object.
	/// </summary>
	public virtual HashAlgorithm Copy()
	{
		throw new NotImplementedException();
	}

}


/// <summary>
/// The Sha256 hash.
/// </summary>
public class Sha256 : HashAlgorithm
{

	/// <summary>
	/// The Sha256 hash.
	/// </summary>
	public Sha256() : base(32)
	{
	}

	/// <summary>
	/// Makes a clone of this object.
	/// </summary>
	public override HashAlgorithm Copy()
	{
		return new Sha256();
	}

}

/// <summary>
/// The Sha284 hash.
/// </summary>
public class Sha384 : HashAlgorithm
{

	/// <summary>
	/// The Sha284 hash.
	/// </summary>
	public Sha384() : base(48)
	{
	}

	/// <summary>
	/// Makes a clone of this object.
	/// </summary>
	public override HashAlgorithm Copy()
	{
		return new Sha384();
	}

}