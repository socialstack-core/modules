using Api.Startup;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Lumity.BlockChains;

/// <summary>
/// Distribution platform.
/// </summary>
public partial class DistributionPlatform
{
	/// <summary>
	/// 
	/// </summary>
	private Dictionary<string, bool> configuredState = new Dictionary<string, bool>();

	/// <summary>
	/// Sets the given service as configured.
	/// </summary>
	/// <param name="key"></param>
	protected void SetConfigured(string key)
	{
		configuredState[key] = true;
	}

	/// <summary>
	/// Sets the json index for a given platform. The blocks must have been uploaded before you do this.
	/// </summary>
	/// <param name="chain">The chain to set the index for.</param>
	/// <param name="index">The index to set.</param>
	/// <returns></returns>
	public async Task SetIndex(BlockChain chain, DistributorJsonIndex index)
	{
		var cdnPath = chain.BlockCdnPath; // Does not start or end with /
		var indexPath = cdnPath + "/index.json";

		// Jsonify:
		var json = Newtonsoft.Json.JsonConvert.SerializeObject(index);

		// Get the bytes and put into a memorystream:
		var bytes = System.Text.Encoding.UTF8.GetBytes(json);

		// Not an ideal setup with all the allocations, but can be optimised later.
		var ms = new MemoryStream();
		ms.Write(bytes);

		await Upload(indexPath, chain.IsPrivate, ms);
	}

	/// <summary>
	/// Gets the json index.
	/// </summary>
	/// <param name="chain">The chain to get the index for.</param>
	/// <returns></returns>
	public async Task<DistributorJsonIndex> GetIndex(BlockChain chain)
	{
		var cdnPath = chain.BlockCdnPath; // Does not start or end with /
		var indexPath = cdnPath + "/index.json";
		string jsonInfo;

		try
		{
			// Attempt to read the index file.
			var index = await ReadFile(indexPath, chain.IsPrivate);

			var ms = new MemoryStream();
			await index.CopyToAsync(ms);
			jsonInfo = System.Text.Encoding.UTF8.GetString(ms.ToArray());

		}
		catch (PublicException ex)
		{
			if (ex.Response.Code == "not_found")
			{
				// Not found - use a default empty one.
				return new DistributorJsonIndex();
			}

			throw;
		}

		try
		{
			if (string.IsNullOrEmpty(jsonInfo))
			{
				return new DistributorJsonIndex();
			}
			else
			{
				return Newtonsoft.Json.JsonConvert.DeserializeObject<DistributorJsonIndex>(jsonInfo);
			}
		}
		catch (Exception e)
		{
			Console.WriteLine("Unable to start distribution of target platform due to errors with json file: " + e.ToString());
			throw;
		}

	}

	/// <summary>
	/// The URL for the upload host (excluding /content/) if this host platform is providing file services.
	/// </summary>
	/// <returns></returns>
	public virtual string GetContentUrl()
	{
		return null;
	}
	
	/// <summary>
	/// True if this host platform has the given service type configured. Key is e.g. "upload".
	/// </summary>
	public virtual bool HasService(string serviceType)
	{
		configuredState.TryGetValue(serviceType, out bool val);
		return val;
	}
	
	/// <summary>
	/// Reads a files bytes from the remote host.
	/// </summary>
	/// <param name="targetPath">The complete path to the file</param>
	/// <param name="isPrivate">True if the file is in private storage and has to be read with a signature.</param>
	/// <returns></returns>
	/// <exception cref="NotImplementedException"></exception>
	public virtual Task<System.IO.Stream> ReadFile(string targetPath, bool isPrivate)
	{
		throw new NotImplementedException();
	}

	/// <summary>
	/// Runs when uploading a file.
	/// </summary>
	/// <param name="targetPath">The complete path of the file, including the first forward slash.</param>
	/// <param name="isPrivate"></param>
	/// <param name="toUpload"></param>
	/// <returns></returns>
	public virtual Task<bool> Upload(string targetPath, bool isPrivate, System.IO.Stream toUpload)
	{
		throw new NotImplementedException();
	}
}