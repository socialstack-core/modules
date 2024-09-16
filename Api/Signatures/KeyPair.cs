using System;
using System.Security.Cryptography;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Security;
using Newtonsoft.Json;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;

namespace Api.Signatures
{
	/// <summary>
	/// A crypto keypair. Used for sign/ verify.
	/// </summary>
	public class KeyPair
	{
		private static readonly X9ECParameters Curve;
		private static readonly ECDomainParameters DomainParams;


		static KeyPair()
		{
			Curve = ECNamedCurveTable.GetByName("secp256k1");
			DomainParams = new ECDomainParameters(Curve.Curve, Curve.G, Curve.N, Curve.H, Curve.GetSeed());
		}

		/// <summary>
		/// Loads a public key from a base64 string
		/// </summary>
		/// <returns></returns>
		public static ECPublicKeyParameters LoadPublicKeyHex(string hex)
		{
			byte[] pubKeyBytes = new byte[hex.Length >> 1];

			for (int i = 0; i < hex.Length >> 1; ++i)
			{
				pubKeyBytes[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + (GetHexVal(hex[(i << 1) + 1])));
			}

			var ecPoint = Curve.Curve.DecodePoint(pubKeyBytes).Normalize();
			return new ECPublicKeyParameters(ecPoint, DomainParams);
		}

		private static int GetHexVal(char hex)
		{
			int val = (int)hex;
			//For uppercase A-F letters:
			//return val - (val < 58 ? 48 : 55);
			//For lowercase a-f letters:
			//return val - (val < 58 ? 48 : 87);
			//Or the two combined, but a bit slower:
			return val - (val < 58 ? 48 : (val < 97 ? 55 : 87));
		}

		/// <summary>
		/// Loads a public key only pair from a base64 string
		/// </summary>
		/// <returns></returns>
		public static KeyPair LoadPublicKey(string base64)
		{
			byte[] pubKeyBytes = Convert.FromBase64String(base64);
			var ecPoint = Curve.Curve.DecodePoint(pubKeyBytes).Normalize();
			var parameters = new ECPublicKeyParameters(ecPoint, DomainParams);

			return new KeyPair()
			{
				PublicKey = parameters
			};
		}

		/// <summary>
		/// Loads a public key only pair from its raw bytes
		/// </summary>
		/// <returns></returns>
		public static KeyPair LoadPublicKey(byte[] pubKeyBytes)
		{
			var ecPoint = Curve.Curve.DecodePoint(pubKeyBytes).Normalize();
			var parameters = new ECPublicKeyParameters(ecPoint, DomainParams);

			return new KeyPair()
			{
				PublicKey = parameters
			};
		}

		/// <summary>
		/// Generates a secp256k1 key pair.
		/// </summary>
		/// <returns></returns>
		public static KeyPair Generate()
		{
			var secureRandom = new SecureRandom();
			var keyParams = new ECKeyGenerationParameters(DomainParams, secureRandom);

			var generator = new ECKeyPairGenerator("ECDSA");
			generator.Init(keyParams);
			var keyPair = generator.GenerateKeyPair();

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
		/// Gets a keypair from the serialised pub/ priv pair.
		/// </summary>
		/// <param name="pub"></param>
		/// <param name="priv"></param>
		/// <returns></returns>
		public static KeyPair FromSerialized(string pub, string priv)
		{
			var result = new KeyPair()
			{
				PrivateKeyBytes = Convert.FromBase64String(priv)
			};

			// Create the D value:
			var privD = new BigInteger(1, result.PrivateKeyBytes);
			result.PrivateKey = new ECPrivateKeyParameters(privD, DomainParams);
			var q = result.PrivateKey.Parameters.G.Multiply(privD).Normalize();
			result.PublicKey = new ECPublicKeyParameters(result.PrivateKey.AlgorithmName, q, DomainParams);

			return result;
		}

		/// <summary>
		/// Gets a keypair from the serialised text version.
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public static KeyPair FromSerialized(string text)
		{
			KeyPair result;

			if (text[0] == '{')
			{
				var jsonInfo = JsonConvert.DeserializeObject<JsonKeyData>(text);

				// JSON format
				result = new KeyPair()
				{
					PrivateKeyBytes = Convert.FromBase64String(jsonInfo.Private)
				};
			}
			else
			{
				result = new KeyPair()
				{
					PrivateKeyBytes = Convert.FromBase64String(text)
				};

			}

			// Create the D value:
			var privD = new BigInteger(result.PrivateKeyBytes);
			result.PrivateKey = new ECPrivateKeyParameters(privD, DomainParams);
			var q = result.PrivateKey.Parameters.G.Multiply(privD).Normalize();
			result.PublicKey = new ECPublicKeyParameters(result.PrivateKey.AlgorithmName, q, DomainParams);

			return result;
		}
		
		/// <summary>
		/// The bytes of the private key.
		/// </summary>
		public byte[] PrivateKeyBytes;

		/// <summary>
		/// Gets this key pair as serialized json.
		/// </summary>
		/// <returns></returns>
		public string Serialize()
		{
			return "{\"private\":\"" + Convert.ToBase64String(PrivateKeyBytes) + "\", \"public\": \"" + Convert.ToBase64String(PublicKey.Q.GetEncoded(false)) + "\"}";
		}

		/// <summary>
		/// Base64 PK
		/// </summary>
		/// <returns></returns>
		public string PrivateKeyBase64()
		{
			return Convert.ToBase64String(PrivateKeyBytes);
		}

		/// <summary>
		/// Base64 PK
		/// </summary>
		/// <returns></returns>
		public string PublicKeyBase64()
		{
			return Convert.ToBase64String(PublicKey.Q.GetEncoded(false));
		}

		/// <summary>
		/// The private key parameters.
		/// </summary>
		public ECPrivateKeyParameters PrivateKey;

		/// <summary>
		/// The public key parameters.
		/// </summary>
		public ECPublicKeyParameters PublicKey;
		
		private ECDsaSigner _signer;
		private ECDsaSigner _verifier;

		/// <summary>
		/// Signs the given message, returning the signature as base 64.
		/// </summary>
		/// <param name="message">The bytes to sign.</param>
		/// <returns></returns>
		public string SignBase64(string message)
		{
			return SignBase64(System.Text.Encoding.UTF8.GetBytes(message));
		}

		/// <summary>
		/// Verifies the given base64 signature for the given message.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="signature"></param>
		/// <returns></returns>
		public bool Verify(string message, string signature)
		{
			return Verify(message, System.Convert.FromBase64String(signature));
		}
		
		/// <summary>
		/// Verifies the given base64 signature for the given message.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="signature"></param>
		/// <returns></returns>
		public bool Verify(string message, byte[] signature)
		{
			if (_verifier == null)
			{
				_verifier = new ECDsaSigner();
				_verifier.Init(false, PublicKey);
			}

			try
			{
				var rLength = (int)signature[0]; // first byte contains length of r array
				var r = new BigInteger(1, signature, 1, rLength);
				var s = new BigInteger(1, signature, rLength + 1, signature.Length - (rLength + 1));

				var messageBytes = System.Text.Encoding.UTF8.GetBytes(message);

				// Can't share this as it has internal properties which get set during ComputeHash
				var sha256 = SHA256.Create();

				// Double sha256 hash (Bitcoin compatible):
				messageBytes = sha256.ComputeHash(messageBytes, 0, messageBytes.Length);
				messageBytes = sha256.ComputeHash(messageBytes);
				lock (_verifier)
				{
					return _verifier.VerifySignature(messageBytes, r, s);
				}
			}
			catch (IndexOutOfRangeException)
			{
				return false;
			}
		}

		/// <summary>
		/// Signs the given message, returning the signature as base 64.
		/// </summary>
		/// <param name="message">The bytes to sign.</param>
		/// <returns></returns>
		public string SignBase64(byte[] message)
		{
			var bytes = Sign(message);
			return System.Convert.ToBase64String(bytes);
		}

		/// <summary>
		/// Signs the given message.
		/// </summary>
		/// <param name="message">The bytes of the message to sign.</param>
		/// <returns>A 64 byte signature.</returns>
		public byte[] Sign(string message)
		{
			return Sign(System.Text.Encoding.UTF8.GetBytes(message));
		}

		/// <summary>
		/// Signs the given message.
		/// </summary>
		/// <param name="message">The bytes of the message to sign.</param>
		/// <returns>A 64 byte signature.</returns>
		public byte[] Sign(byte[] message)
		{
			var sha256 = SHA256.Create();

			if (_signer == null)
			{
				_signer = new ECDsaSigner();
				_signer.Init(true, PrivateKey);
			}

			// Double sha256 hash (Bitcoin compatible):
			message = sha256.ComputeHash(message, 0, message.Length);
			message = sha256.ComputeHash(message);

			BigInteger[] rs;

			lock (_signer)
			{
				rs = _signer.GenerateSignature(message);
			}

			var r = rs[0].ToByteArrayUnsigned();
			var s = rs[1].ToByteArrayUnsigned();
			byte[] result = new byte[1 + r.Length + s.Length];

			result[0] = (byte)r.Length;
			Array.Copy(r, 0, result, 1, r.Length);
			Array.Copy(s, 0, result, r.Length + 1, s.Length);
			return result; // (result is 64 or 65 bytes long)
		}

	}

	/// <summary>
	/// A public/private ecdsa keypair from a JSON file.
	/// </summary>
	public class JsonKeyData
	{
		/// <summary>
		/// The public key, base64.
		/// </summary>
		public string Public;
		/// <summary>
		/// The private key, base64.
		/// </summary>
		public string Private;
	}
}
