using Api.SocketServerLibrary;
using Api.Startup;
using Lumity.BlockChains.Distributors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Lumity.BlockChains;

/// <summary>
/// Distributes files via uploading them to one or more CDNs.
/// </summary>
public class BlockDistributor
{

	/// <summary>
	/// Converts a block ID to a hex nibble path used by CDNs. Does not start or end with fwdslashes.
	/// </summary>
	/// <param name="blockId">14 becomes "e". "45" becomes "2/d"</param>
	/// <param name="builder">If you provide a builder, the result is outputted into that and null is returned.</param>
	/// <returns></returns>
	public static string GetBlockPath(ulong blockId, System.Text.StringBuilder builder = null)
	{
		var output = true;

		if (builder == null)
		{
			builder = new System.Text.StringBuilder();
		}
		else
		{
			output = false;
		}

		var first = true;

		while (blockId > 0)
		{
			// Get the bottom nibble:
			var nibble = blockId & 15;
			blockId = blockId >> 4;

			// Convert to hex lowercase charcode:
			char current = nibble < 10 ? (char)(48 + nibble) : (char)(87 + nibble); // 97 - 10 + nibble

			if (first)
			{
				first = false;
			}
			else
			{
				builder.Append('/');
			}

			builder.Append(current);
		}

		return output ? builder.ToString() : null;
	}

	/// <summary>
	/// The project that this is ditributing.
	/// </summary>
	public BlockChainProject Project;

	/// <summary>
	/// Configuration for this distributor.
	/// </summary>
	public DistributionConfig Config;

	/// <summary>
	/// Creates a new block distributor.
	/// </summary>
	/// <param name="config"></param>
	/// <param name="project"></param>
	public BlockDistributor(DistributionConfig config, BlockChainProject project)
	{
		Config = config;
		Project = project;
		
		// The distributor retains knowledge about what it has successfully uploaded so far (if it is actively publishing blocks).
	}

	/// <summary>
	/// Gets the given range of blocks inclusive. FirstId can equal LastId if you only want one.
	/// </summary>
	/// <param name="chain"></param>
	/// <param name="firstId"></param>
	/// <param name="lastId"></param>
	/// <param name="onBlocks">Called when a segment of the chain is available.</param>
	/// <param name="platform">Optionally specify a platform.</param>
	/// <returns></returns>
	public async Task GetBlockRange(BlockChain chain, ulong firstId, ulong lastId, Func<Stream, ulong, ulong, ValueTask> onBlocks, DistributionPlatform platform = null)
	{
		if (platform == null)
		{
			var platforms = GetPlatforms();

			if (platforms == null || platforms.Count == 0)
			{
				// Not configured!
				throw new Exception("Block distributor partially configured - it doesn't have any CDN platforms to check.");
			}

			// Just uses the first one for now.
			platform = platforms[0];
		}

		// This is currently a very simplistic mechanism in that it is
		// 1 block at a time vs using pre-concatted block files when possible.
		var sb = new System.Text.StringBuilder();

		for (ulong blockId = firstId; blockId <= lastId; blockId++)
		{
			sb.Clear();
			sb.Append(chain.BlockCdnPath);
			sb.Append('/');
			GetBlockPath(blockId, sb);

			// Append type:
			sb.Append(".block");

			var filePath = sb.ToString();
			
			// Get the stream:
			var stream = await platform.ReadFile(filePath, chain.IsPrivate);

			// Process it:
			await onBlocks(stream, blockId, blockId);
		}

	}

	/// <summary>
	/// Updates the configuration for this distributor.
	/// </summary>
	/// <param name="config"></param>
	public void UpdateConfig(DistributionConfig config)
	{
		Config = config;
		_platforms = null;
	}

	/// <summary>
	/// Parsed distribution platforms.
	/// </summary>
	private List<DistributionPlatform> _platforms;

	/// <summary>
	/// Collects the configured distribution platforms.
	/// </summary>
	/// <returns></returns>
	private List<DistributionPlatform> GetPlatforms()
	{
		if (_platforms != null)
		{
			return _platforms;
		}

		var dist = Config;

		if (dist == null)
		{
			return null;
		}

		List<DistributionPlatform> platforms = null;

		if (dist.Aws != null)
		{
			if (platforms == null)
			{
				platforms = new List<DistributionPlatform>();
			}

			platforms.Add(new AwsHost(dist.Aws));
		}

		if (dist.DigitalOcean != null)
		{
			if (platforms == null)
			{
				platforms = new List<DistributionPlatform>();
			}

			platforms.Add(new DigitalOceanHost(dist.DigitalOcean));
		}

		if (dist.Azure != null)
		{
			if (platforms == null)
			{
				platforms = new List<DistributionPlatform>();
			}

			platforms.Add(new AzureHost(dist.Azure));
		}

		_platforms = platforms;
		return platforms;
	}

	/// <summary>
	/// Gets the json index by asking a random platform (or a specific one) for it.
	/// </summary>
	/// <param name="chain">The chain to get the index for.</param>
	/// <param name="platform">Optionally get the index for a specific platform.</param>
	/// <returns></returns>
	public async Task<DistributorJsonIndex> GetIndex(BlockChain chain, DistributionPlatform platform = null)
	{
		if (platform == null)
		{
			var platforms = GetPlatforms();

			if (platforms == null || platforms.Count == 0)
			{
				// Not configured!
				throw new Exception("Block distributor partially configured - it doesn't have any CDN platforms to check.");
			}

			// Just uses the first one for now.
			platform = platforms[0];
		}

		return await platform.GetIndex(chain);
	}

	/// <summary>
	/// Sets the json index for a given platform. The blocks must have been uploaded before you do this.
	/// </summary>
	/// <param name="chain">The chain to set the index for.</param>
	/// <param name="index">The index to set.</param>
	/// <param name="platform">Platform to set it on.</param>
	/// <returns></returns>
	public async Task SetIndex(BlockChain chain, DistributorJsonIndex index, DistributionPlatform platform)
	{
		await platform.SetIndex(chain, index);
	}

	/// <summary>
	/// Starts the distributor. This is only used if acting explicitly as a distributor.
	/// </summary>
	public void StartDistributing()
	{
		// For each chain in the project, collect information from the target CDN(s) about where we're up to.
		// This is the index.json file which must have a very short cache lifespan.
		Task.Run(async () => {

			var platforms = GetPlatforms();

			var chains = Project.Chains;

			for (var i = 0; i < chains.Length; i++)
			{
				var chain = chains[i];

				for (var p = 0; p < platforms.Count; p++)
				{
					var platform = platforms[p];

					// Get the index:
					var index = await GetIndex(chain, platform);

					_ = Task.Run(async () => {
						await DistributeChain(chain, platform, index);
					});
				}

			}

		});
		
	}

	private async Task DistributeChain(BlockChain chain, DistributionPlatform platform, DistributorJsonIndex index)
	{
		// The given index indicates the status of the block files on the given platform. It can be empty if the platform has nothing on it.
		
		// Next, seek to the start offset and then scan along the chain until the next transaction boundary is found.
		ulong blockchainOffset = index.LatestEndByteOffset;
		ulong currentBlockId = index.LatestBlockId + 1;

		var blockMeta = await chain.FindBlocks(async (Writer block, ulong blockId) => {

			Console.WriteLine("Distributor found block #" + blockId + ", size: " + block.Length);

			try
			{
				var sb = new System.Text.StringBuilder();
				sb.Append(chain.BlockCdnPath);
				sb.Append('/');
				GetBlockPath(blockId, sb);

				// Append type:
				sb.Append(".block");

				var filePath = sb.ToString();

				Console.WriteLine("Block path " + filePath);

				// File API requires a stream, so write the blocks to a memstream:
				var ms = block.AllocateMemoryStream();
				ms.Seek(0, SeekOrigin.Begin);

				await platform.Upload(filePath, chain.IsPrivate, ms);
			}
			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
			}

		}, blockchainOffset, currentBlockId);

		// Add distributor mechanism to the chain reader.

		// blockMeta.MaxBytes should == Chain.GetCurrentMaxByte()
		// If it doesn't, a transaction happened in the tiny gap here!

		var reader = blockMeta.Reader;

		// Write the index:
		blockchainOffset = reader.TransactionId;
		currentBlockId = reader.CurrentBlockId;

		index.LatestBlockId = currentBlockId - 1;
		index.LatestEndByteOffset = blockchainOffset;

		// Update the index:
		await SetIndex(chain, index, platform);
	}

}

/// <summary>
/// Represents the index.json file.
/// </summary>
public class DistributorJsonIndex
{
	/// <summary>
	/// Latest ID of blocks on this platform. 0 indicates none.
	/// </summary>
	public ulong LatestBlockId { get; set; } = 0;

	/// <summary>
	/// The byte offset of the end of the latest block
	/// </summary>
	public ulong LatestEndByteOffset { get; set; } = 0;
}