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
	/// True if this is a private project whose blocks should not be publicly downloadable.
	/// </summary>
	public bool IsPrivate;

	/// <summary>
	/// The name of this chain.
	/// </summary>
	public string BlockchainName;

	/// <summary>
	/// This is the hex hash of the very first block and is essentially the project identifier. HEX(SHA-3(FirstBlock)) lowercase.
	/// </summary>
	public string PublicHash;

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
	public bool IsAssembler = false;

	/// <summary>
	/// The node ID of "this" node.
	/// </summary>
	[JsonIgnore]
	public ulong SelfNodeId = ulong.MaxValue;

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
	/// The primary chain of the project.
	/// </summary>
	[JsonIgnore]
	private BlockChain _chain;

	/// <summary>
	/// The primary chain of this project.
	/// </summary>
	public BlockChain Chain => _chain;

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
	public void SetupStorage(string localStoragePath, DistributionConfig remoteConfig)
	{
		if (localStoragePath != null)
		{
			localStoragePath = System.IO.Path.GetFullPath(localStoragePath);

			if (!localStoragePath.EndsWith('/') && !localStoragePath.EndsWith('\\'))
			{
				// Must end with fwdslash:
				localStoragePath += '/';
			}
		}

		if (remoteConfig != null && !string.IsNullOrEmpty(remoteConfig.PublicHash) && string.IsNullOrEmpty(PublicHash))
		{
			PublicHash = remoteConfig.PublicHash;
		}

		LocalStorageDirectory = localStoragePath;

		if (localStoragePath != null && string.IsNullOrEmpty(PublicHash))
		{
			// Collect the hash, if there is one:
			var infoPath = localStoragePath + "info.json";

			if (System.IO.File.Exists(infoPath))
			{
				var infoJson = System.IO.File.ReadAllText(infoPath);

				// Try to deserialise the json:
				var deserialisedInfo = Newtonsoft.Json.JsonConvert.DeserializeObject(infoJson) as Newtonsoft.Json.Linq.JObject;

				if (deserialisedInfo != null && deserialisedInfo["Hash"] != null)
				{
					var hash = deserialisedInfo["Hash"].ToString();

					if (hash.Length != 64)
					{
						throw new Exception(
							"Incorrect project hash length. It is HEX(SHA-3(FirstBlock)) and should be 64 characters long. It was " + 
							hash.Length + " characters, and is located in " + infoPath
						);
					}

					// Set hash field:
					PublicHash = hash;
				}
			}
		}

		// Create a distributor if one is needed:
		if (remoteConfig != null)
		{
			Distributor = new BlockDistributor(remoteConfig, this);
		}
	}

	/// <summary>
	/// Writes the project hash to the info.json file. Requires that local storage has been setup.
	/// </summary>
	/// <exception cref="Exception"></exception>
	public void WriteHashToInfoFile()
	{
		if (LocalStorageDirectory == null)
		{
			throw new Exception("Local storage directory not set. Use SetupStorage before this.");
		}

		// Simple JSON file:
		var json = "{\r\n\t\"Hash\": \"" + PublicHash + "\"\r\n}";

		System.IO.File.WriteAllText(LocalStorageDirectory + "info.json", json);
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

		// Update the chain.
		_chain.Update(timestamp);
	}

	/// <summary>
	/// Sets the selfNodeId on this project so the BAS state can be easily identified.
	/// </summary>
	/// <param name="selfNodeId"></param>
	/// <param name="selfPublicKey"></param>
	/// <param name="selfPrivateKey">Optional. Provide this if the node can act as a BAS.</param>
	public void SetSelfNodeId(ulong selfNodeId, byte[] selfPublicKey, byte[] selfPrivateKey)
	{
		SelfNodeId = selfNodeId;
		IsAssembler = AssemblerId == SelfNodeId;

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

		if (_chain != null)
		{
			_chain.ProjectUpdated();
		}

		IsAssembler = AssemblerId == SelfNodeId;

		Console.WriteLine("- Project updated - " + AssemblerId + ", " + SelfNodeId);

	}

	/// <summary>
	/// Gets the blockchain schema.
	/// </summary>
	/// <returns></returns>
	public Schema GetSchema()
	{
		return _chain.Schema;
	}

	/// <summary>
	/// Gets the blockchain of a given type.
	/// </summary>
	/// <param name="type"></param>
	/// <returns></returns>
	public BlockChain GetChain(ChainType type)
	{
		if (type == ChainType.Public)
		{
			return _chain;
		}

		return null;
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
	/// Generates a secp256k1 key pair for "this" node.
	/// </summary>
	/// <returns></returns>
	public void GenerateSelfKeyPair(ulong selfNodeId)
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

		var pubKey = publicKey.Q.GetEncoded(false);
		var privKey = privateKey.D.ToByteArrayUnsigned();

		SetSelfNodeId(selfNodeId, pubKey, privKey);
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
	/// May be a slow operation. Loads the project from the given path.
	/// </summary>
	public async ValueTask Load()
	{
		if (_loaded)
		{
			return;
		}

		// Load the 4 chains:
		_chain = new BlockChain(this, ChainType.Public, _readerType);
		await _chain.LoadOrCreate();

		// Start the timer:
		if (RunBuiltInMaintenance)
		{
			// Must be started after load as we need to make sure the block boundary meta is accurate (i.e. the state is correct)
			// and we don't want maintenance ticks happening whilst we have partially loaded state.
			StartMaintenanceTimer();
		}

		_loaded = true;

		if (IsAssembler)
		{
			// Require the block server.
			if (BlockServer.SharedBlockServer == null)
			{
				BlockServer.SharedBlockServer = new BlockServer(BlockServer.SharedBlockServerSafeLanMode);
			}
		}
	}

	/// <summary>
	/// True if this project has been loaded.
	/// </summary>
	private bool _loaded;

	/// <summary>
	/// Listens for future transactions using the configured channels.
	/// </summary>
	public void Watch(bool cdnMode = true)
	{
		if (!_loaded)
		{
			throw new Exception("Must load the project (and wait for the load to complete) before watching.");
		}

		_chain.Watch(cdnMode);
	}
}