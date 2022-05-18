using System;
using System.Collections.Generic;
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