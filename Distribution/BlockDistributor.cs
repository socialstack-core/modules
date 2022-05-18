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
	/// Creates a new block distributor.
	/// </summary>
	/// <param name="_project"></param>
	public BlockDistributor(BlockChainProject _project)
	{
		Project = _project;
		
		// The distributor retains knowledge about what it has successfully uploaded so far.
	}

	/// <summary>
	/// Starts the distributor.
	/// </summary>
	public void Start()
	{
		var dist = Project.Distribution;

		if (dist == null)
		{
			return;
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

		if (platforms == null)
		{
			return;
		}

		// For each chain in the project, collect information from the target CDN(s) about where we're up to.
		// This is the index.json file which must have a very short cache lifespan.
		Task.Run(async () => {

			var chains = Project.Chains;

			for (var i = 0; i < chains.Length; i++)
			{
				var chain = chains[i];

				var cdnPath = chain.BlockCdnPath; // Does not start or end with /
				var indexPath = cdnPath + "/index.json";

				for (var p = 0; p < platforms.Count; p++)
				{
					var platform = platforms[p];

					Console.WriteLine("Attempting to read index file.." + indexPath);

					try
					{
						// Attempt to read the index file.
						var index = await platform.ReadFile(indexPath, chain.IsPrivate);

						var ms = new MemoryStream();
						await index.CopyToAsync(ms);

						var jsonInfo = System.Text.Encoding.UTF8.GetString(ms.ToArray());

						_ = Task.Run(async () => {
							await DistributeChain(chain, platform, jsonInfo);
						});
						
					}
					catch(PublicException ex)
					{
						if (ex.Response.Code == "not_found")
						{
							// Not found - this is fine.
							_ = Task.Run(async () => {
								await DistributeChain(chain, platform, null);
							});
							
						}
						else
						{
							throw;
						}
					}

				}

			}

		});
		
	}

	private async Task DistributeChain(BlockChain chain, DistributionPlatform platform, string indexJson)
	{
		// The given json indicates the status of the block files on the given platform. If it is null, the platform is empty.
		DistributorJsonIndex index = null;
		
		try
		{
			if (string.IsNullOrEmpty(indexJson))
			{
				index = new DistributorJsonIndex();
			}
			else
			{
				index = Newtonsoft.Json.JsonConvert.DeserializeObject<DistributorJsonIndex>(indexJson);
			}
		}
		catch(Exception e)
		{
			Console.WriteLine("Unable to start distribution of target platform due to errors with json file: " + e.ToString());
			return;
		}

		// Next, seek to the start offset and then scan along the chain until the next transaction boundary is found.
		ulong blockchainOffset = index.LatestEndByteOffset;
		ulong currentBlockId = index.LatestBlockId + 1;

		await chain.FindBlocks(async (Writer block, ulong blockId) => {

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