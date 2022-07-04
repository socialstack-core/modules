namespace Lumity.SikeIsogeny;

/// <summary>
/// A public/ private keypair for SIDH
/// </summary>
public class SidhKeyPairOpti
{
	/// <summary>
	/// The private key
	/// </summary>
	public SidhPrivateKeyOpti PrivateKey;
	/// <summary>
	/// The public key
	/// </summary>
	public SidhPublicKeyOpti PublicKey;
	
	/// <summary>
	/// Create a new key pair.
	/// </summary>
	/// <param name="pubKey"></param>
	/// <param name="privKey"></param>
	public SidhKeyPairOpti(SidhPublicKeyOpti pubKey, SidhPrivateKeyOpti privKey){
		PrivateKey = privKey;
		PublicKey = pubKey;
	}
}

/// <summary>
/// A public/ private keypair for SIDH
/// </summary>
public class SidhKeyPairRef
{
	/// <summary>
	/// The private key
	/// </summary>
	public SidhPrivateKeyRef PrivateKey;
	/// <summary>
	/// The public key
	/// </summary>
	public SidhPublicKeyRef PublicKey;
	
	/// <summary>
	/// Create a new key pair.
	/// </summary>
	/// <param name="pubKey"></param>
	/// <param name="privKey"></param>
	public SidhKeyPairRef(SidhPublicKeyRef pubKey, SidhPrivateKeyRef privKey){
		PrivateKey = privKey;
		PublicKey = pubKey;
	}
}