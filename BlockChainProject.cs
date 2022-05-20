using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Crypto.Signers;
using System.Timers;

namespace Lumity.BlockChains;



/// <summary>
/// Represents a particular blockchain project (a group of blockchains).
/// </summary>
public class BlockChainProject
{
	/// <summary>
	/// Timestamp precision (currently never changed).
	/// </summary>
	public ulong TimestampPrecision = 1000000000;

	/// <summary>
	/// The date string which timestamps are relative to. This can be safely updated once every 500 years assuming the default timestamp precision.
	/// </summary>
	public uint StartYear = 2000;

	/// <summary>
	/// The name of this chain.
	/// </summary>
	public string BlockchainName;

	/// <summary>
	/// Hex(sha3(project-public))
	/// </summary>
	public string PublicHash;

	/// <summary>
	/// The projects public key.
	/// </summary>
	public byte[] PublicKey;

	/// <summary>
	/// The projects private key.
	/// </summary>
	public byte[] PrivateKey;

	/// <summary>
	/// The chain version, often never changes.
	/// </summary>
	public uint Version = 1;

	/// <summary>
	/// The public URL where this chain is running.
	/// </summary>
	public string ServiceUrl;

	/// <summary>
	/// The executable for this project, if there is one.
	/// </summary>
	public byte[] ExecutableArchive;

	/// <summary>
	/// Node Id of the current BAS. If it's 0, the default value is the first defined node.
	/// </summary>
	public uint AssemblerId;

	/// <summary>
	/// This is based on the StartYear.
	/// </summary>
	[JsonIgnore]
	public long TimestampTickOffset;

	/// <summary>
	/// Set to true if this node is the current BAS.
	/// </summary>
	[JsonIgnore]
	public bool IsAssembler = true;

	/// <summary>
	/// The node ID of "this" node.
	/// </summary>
	[JsonIgnore]
	public uint SelfNodeId;

	/// <summary>
	/// The private key of this node.
	/// </summary>
	[JsonIgnore]
	public byte[] SelfPrivateKey;

	/// <summary>
	/// The public key of this node.
	/// </summary>
	[JsonIgnore]
	public byte[] SelfPublicKey;

	/// <summary>
	/// The 4 chains, indexed by ChainType-1.
	/// </summary>
	[JsonIgnore]
	private BlockChain[] _chains = new BlockChain[4];

	/// <summary>
	/// The chains of this project.
	/// </summary>
	public BlockChain[] Chains => _chains;

	/// <summary>
	/// True if the built in maintenance timer should tick (once per second). See also: Update and StartMaintenanceTimer.
	/// </summary>
	public bool RunBuiltInMaintenance { get; set; } = true;

	/// <summary>
	/// Approx max wait time in milliseconds for a transaction to be confirmed by the BAS.
	/// The actual wait time is reliant on the RTT to the block assembly service.
	/// </summary>
	public uint MaxTransactionWaitTimeMs { get; set; } = 2000;

	private Timer _maintenanceTimer;

	/// <summary>
	/// A local storage directory for holding chain files. If this is null, the chain will be exclusively runtime only.
	/// </summary>
	public string LocalStorageDirectory;

	/// <summary>
	/// Used to either download remote blocks or upload them, depending on if we are the assembly service.
	/// </summary>
	public BlockDistributor Distributor;

	/// <summary>
	/// Sets up the block storage (local and remote).
	/// </summary>
	public void SetupStorage(string localStoragePath, DistributionConfig config)
	{
		LocalStorageDirectory = localStoragePath;

		// Create a distributor:
		Distributor = new BlockDistributor(config, this);
	}

	/// <summary>
	/// Starts the maintenance timer. Happens automatically if RunBuiltInMaintenance is true.
	/// </summary>
	public void StartMaintenanceTimer()
	{
		if (_maintenanceTimer != null)
		{
			return;
		}

		_maintenanceTimer = new Timer();
		_maintenanceTimer.Elapsed += (object source, ElapsedEventArgs e) => {

			// Run maintenance on the chains:
			DoMaintenance();

		};

		_maintenanceTimer.Interval = 1000;
		_maintenanceTimer.Enabled = true;
	}

	/// <summary>
	/// Perform any housekeeping on the chains of this project. Call approximately once a second if running a custom timer, otherwise an automatic timer will keep check by default.
	/// </summary>
	public void DoMaintenance()
	{
		// Local stamp first:
		var stamp = DateTime.UtcNow.Ticks;

		// Adjust to being relative to the project year:
		stamp -= TimestampTickOffset;

		// Finally convert ticks into the default project precision (which will be assumed to be nanoseconds always here):
		var timestamp = ((ulong)stamp) * 100;

		// Update each chain.
		for (var i = 0; i < _chains.Length; i++)
		{
			_chains[i].Update(timestamp);
		}
	}

	/// <summary>
	/// Gets a signer to use when creating e.g. block signatures for the first block.
	/// </summary>
	/// <returns></returns>
	/// <exception cref="Exception"></exception>
	public ECDsaSigner GetProjectSigner()
	{
		if (_signer == null)
		{
			if (PrivateKey == null)
			{
				throw new Exception("Unable to create a signer as you haven't provided the project private key. Set PrivateKey before calling this.");
			}

			var curve = ECNamedCurveTable.GetByName("secp256k1");
			var domainParams = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H, curve.GetSeed());

			var privD = new BigInteger(PrivateKey);
			var pk = new ECPrivateKeyParameters(privD, domainParams);

			_signer = new ECDsaSigner();
			_signer.Init(true, pk);
		}

		return _signer;
	}
	
	/// <summary>
	/// Gets a verifier to use when creating e.g. block signatures for the first block.
	/// </summary>
	/// <returns></returns>
	/// <exception cref="Exception"></exception>
	public ECDsaSigner GetProjectVerifier()
	{
		if (_verifier == null)
		{
			if (PublicKey == null)
			{
				throw new Exception("Unable to create a verifier as you haven't provided the project public key. Set PublicKey before calling this.");
			}

			var curve = ECNamedCurveTable.GetByName("secp256k1");
			var domainParams = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H, curve.GetSeed());

			var ecPoint = curve.Curve.DecodePoint(PublicKey).Normalize();
			var pk = new ECPublicKeyParameters(ecPoint, domainParams);

			_verifier = new ECDsaSigner();
			_verifier.Init(false, pk);
		}

		return _verifier;
	}

	private ECDsaSigner _signer;
	private ECDsaSigner _nodeSigner;
	
	private ECDsaSigner _verifier;
	private ECDsaSigner _nodeVerifier;

	/// <summary>
	/// Gets a signer to use when creating e.g. block signatures from this node.
	/// </summary>
	/// <returns></returns>
	/// <exception cref="Exception"></exception>
	public ECDsaSigner GetNodeVerifier()
	{
		if (_nodeVerifier == null)
		{
			if (SelfPublicKey == null)
			{
				throw new Exception("Unable to create a verifier as you haven't provided the node public key. Use SetSelfNodeId before calling this.");
			}

			var curve = ECNamedCurveTable.GetByName("secp256k1");
			var domainParams = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H, curve.GetSeed());

			var ecPoint = curve.Curve.DecodePoint(SelfPublicKey).Normalize();
			var pk = new ECPublicKeyParameters(ecPoint, domainParams);

			_nodeVerifier = new ECDsaSigner();
			_nodeVerifier.Init(false, pk);
		}

		return _nodeVerifier;
	}
	
	/// <summary>
	/// Gets a signer to use when creating e.g. block signatures from this node.
	/// </summary>
	/// <returns></returns>
	/// <exception cref="Exception"></exception>
	public ECDsaSigner GetNodeSigner()
	{
		if (_nodeSigner == null)
		{
			if (SelfPrivateKey == null)
			{
				throw new Exception("Unable to create a signer as you haven't provided the node Id or private key. Use SetSelfNodeId before calling this.");
			}

			var curve = ECNamedCurveTable.GetByName("secp256k1");
			var domainParams = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H, curve.GetSeed());

			var privD = new BigInteger(SelfPrivateKey);
			var pk = new ECPrivateKeyParameters(privD, domainParams);

			_nodeSigner = new ECDsaSigner();
			_nodeSigner.Init(true, pk);
		}

		return _nodeSigner;
	}

	/// <summary>
	/// Sets the selfNodeId on this project so the BAS state can be easily identified.
	/// </summary>
	/// <param name="selfNodeId"></param>
	/// <param name="selfPublicKey"></param>
	/// <param name="selfPrivateKey">Optional. Provide this if the node can act as a BAS.</param>
	public void SetSelfNodeId(uint selfNodeId, byte[] selfPublicKey, byte[] selfPrivateKey)
	{
		SelfNodeId = selfNodeId;
		IsAssembler = AssemblerId == 0 || AssemblerId == SelfNodeId;

		SelfPublicKey = selfPublicKey;
		SelfPrivateKey = selfPrivateKey;

		Console.WriteLine("- Project updated (Self ready) - " + AssemblerId + ", " + SelfNodeId);

	}

	/// <summary>
	/// Set the current assembler.
	/// </summary>
	public async ValueTask SetAssembler(uint id)
	{
		var publicChain = GetChain(ChainType.Public);

		if (publicChain == null)
		{
			return;
		}

		// Submit txn:
		await publicChain.SetAssembler(id);
	}

	/// <summary>
	/// Called whenever the project is updated (or instanced).
	/// </summary>
	public void Updated()
	{
		// Set the tick offset:
		TimestampTickOffset = new DateTime((int)StartYear, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;

		if (_chains != null)
		{
			for (var i = 0; i < _chains.Length; i++)
			{
				var chain = _chains[i];

				if (chain != null)
				{
					chain.ProjectUpdated();
				}
			}
		}

		IsAssembler = AssemblerId == 0 || AssemblerId == SelfNodeId;

		Console.WriteLine("- Project updated - " + AssemblerId + ", " + SelfNodeId);

	}

	/// <summary>
	/// Gets the blockchain schema.
	/// </summary>
	/// <param name="type"></param>
	/// <returns></returns>
	public Schema GetSchema(ChainType type)
	{
		return _chains[(int)type - 1].Schema;
	}

	/// <summary>
	/// Gets the blockchain of a given type.
	/// </summary>
	/// <param name="type"></param>
	/// <returns></returns>
	public BlockChain GetChain(ChainType type)
	{
		return _chains[(int)type - 1];
	}

	/// <summary>
	/// Creates a blockchain project. Call Load when you're ready to set it up.
	/// </summary>
	public BlockChainProject()
	{
		// Ensure defaults are set:
		Updated();
	}

	/// <summary>
	/// Generates a secp256k1 key pair.
	/// </summary>
	/// <returns></returns>
	public void GenerateKeyPair()
	{
		var curve = ECNamedCurveTable.GetByName("secp256k1");
		var domainParams = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H, curve.GetSeed());

		var secureRandom = new SecureRandom();
		var keyParams = new ECKeyGenerationParameters(domainParams, secureRandom);

		var generator = new ECKeyPairGenerator("ECDSA");
		generator.Init(keyParams);
		var keyPair = generator.GenerateKeyPair();

		var privateKey = keyPair.Private as ECPrivateKeyParameters;
		var publicKey = keyPair.Public as ECPublicKeyParameters;

		PublicKey = publicKey.Q.GetEncoded(false);
		PrivateKey = privateKey.D.ToByteArrayUnsigned();
	}

	/// <summary>
	/// Sets the given type as the reader type for this project.
	/// </summary>
	/// <param name="type">A custom transaction reader type to override the built in validation rule set.
	/// Note that all nodes operating on the same chain must be using the same validation function.</param>
	public void SetReaderType(Type type)
	{
		if (type == null)
		{
			type = typeof(TransactionReader);
		}

		_readerType = type;
	}

	private Type _readerType;

	/// <summary>
	/// Called when a notable reader event occurs. Currently only one event (reader created).
	/// </summary>
	public Action<TransactionReader, TransactionReaderEvent> OnReaderEvent;

	/// <summary>
	/// Called when a notable chain event occurs. Currently only one event (chain created).
	/// </summary>
	public Action<BlockChain, BlockChainEvent> OnChainEvent;

	/// <summary>
	/// May be a slow operation. Loads the project from the given path, optionally specific chain types. If you don't specify them, all are loaded.
	/// </summary>
	public async ValueTask Load()
	{
		// Load the 4 chains:
		_chains[0] = new BlockChain(this, ChainType.Public, _readerType);
		await _chains[0].LoadOrCreate();

		_chains[1] = new BlockChain(this, ChainType.Private, _readerType);
		await _chains[1].LoadOrCreate();

		_chains[2] = new BlockChain(this, ChainType.PublicHost, _readerType);
		await _chains[2].LoadOrCreate();

		_chains[3] = new BlockChain(this, ChainType.PrivateHost, _readerType);
		await _chains[3].LoadOrCreate();

		// Start the timer:
		if (RunBuiltInMaintenance)
		{
			// Must be started after load as we need to make sure the block boundary meta is accurate (i.e. the state is correct)
			// and we don't want maintenance ticks happening whilst we have partially loaded state.
			StartMaintenanceTimer();
		}
	}

	/// <summary>
	/// Listens for future transactions using the configured channels.
	/// </summary>
	public void Watch()
	{
		for (var i = 0; i < _chains.Length; i++)
		{
			var chain = _chains[i];
			if (chain == null)
			{
				continue;
			}

			chain.Watch();
		}
	}
}