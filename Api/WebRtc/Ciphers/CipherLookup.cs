using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.Parameters;
using System.Collections.Generic;

namespace Api.WebRTC.Ciphers;


/// <summary>
/// A lookup for obtaining active ciphers.
/// </summary>
public static class CipherLookup
{
	/// <summary>
	/// IANA cipher name list for the ciphers which are active on this server.
	/// </summary>
	public static string[] Active = new string[]{
		"TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256",
		// "TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256",
		// "TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384",    TODO: Handle the hash
		// "TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384",      TODO: Handle ECDHE+RSA exchange
		// "TLS_ECDHE_ECDSA_WITH_CHACHA20_POLY1305_SHA256",
		// "TLS_ECDHE_RSA_WITH_CHACHA20_POLY1305_SHA256",
		// "TLS_DHE_RSA_WITH_AES_128_GCM_SHA256",        TODO: Handle DHE+RSA exchange
		// "TLS_DHE_RSA_WITH_AES_256_GCM_SHA384"
	};
	
	// The default is Mozilla's latest recommended intermediate compatibility set.
	// Ideal for 1.3: "TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256","TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384","TLS_ECDHE_ECDSA_WITH_CHACHA20_POLY1305_SHA256"     (DTLS 1.3 currently draft)
	private static Dictionary<ushort, Cipher> _cipherLookup;
	
	/// <summary>
	/// Loads active ciphers into the lookup
	/// </summary>
	private static void LoadCiphers()
	{
		var cu = new Dictionary<ushort, Cipher>();
		
		for(var i=0;i<Active.Length;i++){
			var cipher = LoadCipher(Active[i]);
			cipher.Init();

			if (cipher == null){
				System.Console.WriteLine("Warning: Unrecognised TLS cipher - " + Active[i]);
				continue;
			}
			
			cu[cipher.TlsId] = cipher;
		}
		
		_cipherLookup = cu;
	}
	
	/// <summary>
	/// Loads a cipher info from the IANA name. Must call Init on the returned cipher to get it to setup internal values.
	/// </summary>
	/// <param name="ianaName"></param>
	/// <returns></returns>
	private static Cipher LoadCipher(string ianaName){
		
		switch(ianaName){
			case "TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256":
				return new Cipher("TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256", 0xC02B, 7){
					KeyExchange = new Ecdhe(),
					Signature = new Ecdsa(),
					Encryption = new Aes128Gcm(),
					Hash = new Sha256()
				};
			case "TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256":
				return new Cipher("TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256", 0xC02F, 6){
					KeyExchange = new Ecdhe(),
					Signature = new Rsa(),
					Encryption = new Aes128Gcm(),
					Hash = new Sha256()
				};
			case "TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384":
				return new Cipher("TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384", 0xC02C, 9){
					KeyExchange = new Ecdhe(),
					Signature = new Ecdsa(),
					Encryption = new Aes256Gcm(),
					Hash = new Sha384()
				};
			case "TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384":
				return new Cipher("TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384", 0xC030, 8){
					KeyExchange = new Ecdhe(),
					Signature = new Rsa(),
					Encryption = new Aes256Gcm(),
					Hash = new Sha384()
				};
			case "TLS_ECDHE_ECDSA_WITH_CHACHA20_POLY1305_SHA256":
				return new Cipher("TLS_ECDHE_ECDSA_WITH_CHACHA20_POLY1305_SHA256", 0xCCA9, 4){
					KeyExchange = new Ecdhe(),
					Signature = new Ecdsa(),
					Encryption = new ChaCha20Poly1305(),
					Hash = new Sha256()
				};
			case "TLS_ECDHE_RSA_WITH_CHACHA20_POLY1305_SHA256":
				return new Cipher("TLS_ECDHE_RSA_WITH_CHACHA20_POLY1305_SHA256", 0xCCA8, 5){
					KeyExchange = new Ecdhe(),
					Signature = new Rsa(),
					Encryption = new ChaCha20Poly1305(),
					Hash = new Sha256()
				};
			case "TLS_DHE_RSA_WITH_AES_128_GCM_SHA256":
				return new Cipher("TLS_DHE_RSA_WITH_AES_128_GCM_SHA256", 0x009E, 10){
					KeyExchange = new Dhe(),
					Signature = new Rsa(),
					Encryption = new Aes128Gcm(),
					Hash = new Sha256()
				};
			case "TLS_DHE_RSA_WITH_AES_256_GCM_SHA384":
				return new Cipher("TLS_DHE_RSA_WITH_AES_256_GCM_SHA384", 0x009F, 13){
					KeyExchange = new Dhe(),
					Signature = new Rsa(),
					Encryption = new Aes256Gcm(),
					Hash = new Sha384()
				};
		}
		
		// Unsupported cipher
		return null;
	}
	
	/// <summary>
	/// Get a cipher by its TLS ID.
	/// </summary>
	/// <param name="tlsCipherId"></param>
	/// <returns></returns>
	public static Cipher GetCipher(ushort tlsCipherId){
		
		if(_cipherLookup == null)
		{
			// Only loads the ones we have active though:
			LoadCiphers();
		}
		
		_cipherLookup.TryGetValue(tlsCipherId, out Cipher result);
		return result;
	}
	
}
