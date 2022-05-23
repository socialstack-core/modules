using Api.Configuration;
using Microsoft.Extensions.Configuration;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Math;
using System.Security.Cryptography;
using System;
using Api.SocketServerLibrary;
using Api.Startup;

namespace Api.Signatures
{
	/// <summary>
	/// Handles generation and validation of signatures used for e.g. serving of private files.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class SignatureService : AutoService
    {

		private KeyPair _keyPair;
		private SignatureServiceConfig _config;

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public SignatureService()
		{
			// Generate or load keypair. First check if it's in appsettings:
			_config = GetConfig((SignatureServiceConfig newConfig) => {

				// Create a new key now:
				var keypair = KeyPair.Generate();

				newConfig.Public = keypair.PublicKeyBase64();
				newConfig.Private = keypair.PrivateKeyBase64();

			});

			_config.OnChange += () => {
				LoadKey();
				return new System.Threading.Tasks.ValueTask();
			};

			LoadKey();
		}

		private void LoadKey()
		{
			if(string.IsNullOrEmpty(_config.Private))
			{
				throw new PublicException("Invalid signature service configuration. signatureService.key is now never generated or used and instead the key always comes from the database, but your database entry doesn't have a private key set. Delete the SignatureService config from your database to let it be recreated.", "key_invalid");
			}

			_keyPair = KeyPair.FromSerialized(_config.Public, _config.Private);

			// Test the integrity of the key:
			var roundTrip = "signatureServiceTest";
			var sig = Sign(roundTrip);
			var result = ValidateSignature(roundTrip, sig);

			if (!result)
			{
				throw new PublicException("You have a broken SignatureService config - delete it, and restart the API to generate a new one.", "key_invalid_no_rt");
			}
		}

		private int PoolSize;
		private PooledHMac _pool;
		private object poolLock = new object();

		private void ReturnToPool(PooledHMac mac)
		{
			lock (poolLock)
			{
				if (PoolSize > 1000)
				{
					// Prevent excessive pool growth
					return;
				}

				PoolSize++;
				mac.Next = _pool;
				_pool = mac;
			}
		}

		/// <summary>
		/// Gets a hmac helper, which may come from a pool.
		/// </summary>
		/// <returns></returns>
		public PooledHMac GetHmac()
		{
			PooledHMac result = null;

			lock (poolLock)
			{
				if (_pool != null)
				{
					PoolSize--;
					result = _pool;
					_pool = _pool.Next;
				}
			}

			if (result == null)
			{
				// Create an instance:
				var hmacSha256 = new Org.BouncyCastle.Crypto.Macs.HMac(new Org.BouncyCastle.Crypto.Digests.Sha256Digest());
				hmacSha256.Init(new Org.BouncyCastle.Crypto.Parameters.KeyParameter(_keyPair.PrivateKeyBytes));

				result = new PooledHMac()
				{
					Mac = hmacSha256
				};

			}
			else
			{
				result.Next = null;
			}

			return result;
		}

		/// <summary>
		/// Validates that the given string ends with an alphachar signature.
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		public bool ValidateHmac256AlphaChar(string str)
		{
			var hmac = GetHmac();

			var writer = Writer.GetPooled();
			writer.Start(null);
			writer.WriteASCII(str);

			var input = writer.FirstBuffer.Bytes;

			// Write block to hmac:
			hmac.Mac.BlockUpdate(input, 0, writer.Length - 64);

			// Write resulting hmac to a scratch area of the writer buffer:
			hmac.Mac.DoFinal(input, 256);

			// Compare bytes of the generated hmac to the alphachar bytes located at writer.Length-64
			var offset = writer.Length - 64;
			var success = true;

			for (var i = 0; i < 32; i++)
			{
				var hmacLocalByte = input[256 + i];

				if (input[offset++] != (byte)((hmacLocalByte & 15) + 97))
				{
					success = false;
					break;
				}

				if (input[offset++] != (byte)((hmacLocalByte >> 4) + 97))
				{
					success = false;
					break;
				}
			}

			writer.Release();
			ReturnToPool(hmac);
			return success;
		}

		/// <summary>
		/// Signs the given writer content with a sha256 HMAC using the internal private key.
		/// The outputted HMAC goes into the writer's buffer as hex.
		/// </summary>
		/// <param name="writer"></param>
		public void SignHmac256AlphaChar(Writer writer)
		{
			if (writer.Length > 256)
			{
				// Writer content is longer than the length of the hash. 
				// Whilst this is usually handled by just compounding the blocks together, this is an error condition here.
				// That's in part because no signed context should ever be this long anyway, but also because collision attacks become possible
				// (even though with sha256 they're practically impossible, but better safe than sorry!)
				throw new Exception("Unable to sign content as it is too long.");
			}

			// Note that also due to the above length check, we know for certain that we have 
			// enough space in the writer's buffer for the complete hex encoding of the hmac, meaning we can short out buffer overrun checks as well.
			// We'll use the writer's buffer as a scratch space too.

			var hmac = GetHmac();

			var input = writer.FirstBuffer.Bytes;

			// Write block to hmac:
			hmac.Mac.BlockUpdate(input, 0, writer.Length);

			// Write resulting hmac to a scratch area of the writer buffer:
			hmac.Mac.DoFinal(input, 256);

			// Write alphachar bytes:
			writer.WriteAlphaChar(input, 256, 32);

			ReturnToPool(hmac);
		}

		/// <summary>
		/// Generates a signature for the given piece of text.
		/// The timestamp will be appended to the end of the valueToSign as ?t={timestamp}.
		/// </summary>
		/// <returns></returns>
		public string Sign(string valueToSign, long timestamp)
		{
			return _keyPair.SignBase64(valueToSign + "?t=" + timestamp);
		}

		/// <summary>
		/// Validates a signature for a given signed value.
		/// </summary>
		/// <param name="signedValue">The value - usually a URL - being signed.</param>
		/// <param name="signature">Signature value</param>
		/// <param name="hostName">Host name to look up the key for. Host name is case sensitive, and will not be trimmed. Lowercase recommended.
		/// If null, uses the main keypair.</param>
		public bool ValidateSignatureFromHost(string signedValue, string signature, string hostName)
		{
			if (hostName == null)
			{
				// No host - use default:
				return _keyPair.Verify(signedValue, signature);
			}

			// Lookup key:
			if (_config.Hosts != null && _config.Hosts.TryGetValue(hostName, out SignatureServiceHostConfig host))
			{
				var key = host.GetKey();
				return key.Verify(signedValue, signature);
			}

			// A host was specified but it is not in the config here.
			return false;
		}
			
		/// <summary>
		/// Validates a signature for a given signed value.
		/// </summary>
		/// <param name="signedValue">The value - usually a URL - being signed.</param>
		/// <param name="signatureB64">Base64</param>
		/// <param name="publicKeyHex">Hex formatted public key</param>
		/// <returns>True if the signature is valid.</returns>
		public bool ValidateSignature(string signedValue, string signatureB64, string publicKeyHex)
		{
			var _verifier = new ECDsaSigner();
			_verifier.Init(false, KeyPair.LoadPublicKeyHex(publicKeyHex));

			var signature = System.Convert.FromBase64String(signatureB64);

			try
			{
				var rLength = (int)signature[0]; // first byte contains length of r array
				var r = new BigInteger(1, signature, 1, rLength);
				var s = new BigInteger(1, signature, rLength + 1, signature.Length - (rLength + 1));

				var messageBytes = System.Text.Encoding.UTF8.GetBytes(signedValue);

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
		/// Validates a signature for a given signed value. The timestamp will be appended to the end as ?t={timestamp}.
		/// </summary>
		/// <param name="signedValue">The value - usually the URL itself - being signed.</param>
		/// <param name="timestamp">The timestamp.</param>
		/// <param name="signature"></param>
		/// <returns>True if the signature is valid.</returns>
		public bool ValidateSignature(string signedValue, long timestamp, string signature)
		{
			return _keyPair.Verify(signedValue + "?t=" + timestamp, signature);
		}

		/// <summary>
		/// Generates a signature for the given piece of text as-is.
		/// </summary>
		/// <returns></returns>
		public string Sign(string valueToSign)
		{
			return _keyPair.SignBase64(valueToSign);
		}

		/// <summary>
		/// Validates a signature for a given signed value as-is.
		/// </summary>
		/// <param name="signedValue">The value - usually the URL itself - being signed.</param>
		/// <param name="signature"></param>
		/// <returns>True if the signature is valid.</returns>
		public bool ValidateSignature(string signedValue, string signature)
		{
			return _keyPair.Verify(signedValue, signature);
		}

	}

	/// <summary>
	/// A pooled reusable hmac helper.
	/// </summary>
	public class PooledHMac
	{
		/// <summary>
		/// The hmac engine.
		/// </summary>
		public Org.BouncyCastle.Crypto.Macs.HMac Mac;
		/// <summary>
		/// Next in pool.
		/// </summary>
		public PooledHMac Next;
	}

}
