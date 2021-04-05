using Org.BouncyCastle.Crypto.Parameters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Signatures
{
	/// <summary>
	/// The appsettings.json config for the sig service. Usually used for prod/ stage.
	/// </summary>
	public class SignatureServiceConfig
	{
		/// <summary>
		/// Private key (base 64).
		/// </summary>
		public string Private { get; set; }

		/// <summary>
		/// Public key (base 64).
		/// </summary>
		public string Public { get; set; }

		/// <summary>
		/// Host name -> public key lookup (if there are any additional keys). Host name is case sensitive, and will not be trimmed. Lowercase recommended.
		/// </summary>
		public Dictionary<string, SignatureServiceHostConfig> Hosts { get; set; }
	}

	/// <summary>
	/// Additional host config.
	/// </summary>
	public class SignatureServiceHostConfig
	{

		/// <summary>
		/// Remote host public key (base 64).
		/// </summary>
		public string Public { get; set; }

		/// <summary>
		/// The pubkey only (doesn't have a private key).
		/// </summary>
		private KeyPair _key;

		/// <summary>
		/// Get the pubkey parameters (they can be cached).
		/// </summary>
		public KeyPair GetKey()
		{
			if (_key == null)
			{
				_key = KeyPair.LoadPublicKey(Public);
			}

			return _key;
		}

	}

}
