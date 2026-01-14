using MongoDB.Bson.IO;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Api.CanvasRenderer;


/// <summary>
/// A cache for built files across one or more UIBundles.
/// </summary>
public class UIBuildCache
{
	/// <summary>
	/// True if any files have been compiled since the cache was last loaded.
	/// </summary>
	public bool Changed;
	private readonly string _cacheDir;
	private readonly string _fileName = "ui-cache.json";
	private readonly string _path;
	/// <summary>
	/// Number of cache hits.
	/// </summary>
	public int Hit;

	/// <summary>
	/// Number of cache misses.
	/// </summary>
	public int Miss;

	private CacheData _data;

	private class CacheData
	{
		public List<CachedFileMeta> GlobalFileMap { get; set; } = new();

		/// <summary>
		/// The CRC of the SCSS header.
		/// </summary>
		public uint ScssHeaderCrc { get; set; }

		/// <summary>
		/// The length of the SCSS header.
		/// </summary>
		public int ScssHeaderLength { get; set; }

		public Dictionary<string, BundleCacheData> Bundles { get; set; } = new();
	}

	/// <summary>
	/// Creates a new cache for the given directory
	/// </summary>
	/// <param name="cacheDir"></param>
	public UIBuildCache(string cacheDir)
	{
		_cacheDir = cacheDir;
		_path = cacheDir != null ? Path.Combine(cacheDir, _fileName) : null;
		Reset();
	}

	/// <summary>
	/// Resets the cache data to an empty slate.
	/// </summary>
	public void Reset()
	{
		_data = new CacheData();
	}

	/// <summary>
	/// Loads the cached data.
	/// </summary>
	/// <returns></returns>
	public async Task StartAsync()
	{
		if (_path == null)
			return;

		try
		{
			string text = await LoadTextFileAsync(_path);
			_data = Newtonsoft.Json.JsonConvert.DeserializeObject<CacheData>(text) ?? new CacheData();
		}
		catch (FileNotFoundException)
		{
			// ignore missing file
		}
		catch (DirectoryNotFoundException)
		{
			// ignore missing directory
		}
		catch (Exception e)
		{
			Console.WriteLine($"Cache load failure, ignoring it. {e}");
		}
	}

	/// <summary>
	/// True if the SCSS header has changed.
	/// </summary>
	/// <param name="globalMap"></param>
	/// <returns></returns>
	public bool ScssHeaderChanged(GlobalSourceFileMap globalMap)
	{
		return _data == null || _data.ScssHeaderCrc != globalMap.GetScssCrc() || _data.ScssHeaderLength != globalMap.GetScssGlobals().Length;
	}

	/// <summary>
	/// Writes out the cache data.
	/// </summary>
	/// <param name="globalMap"></param>
	/// <param name="bundles"></param>
	/// <returns></returns>
	public async Task SaveAsync(GlobalSourceFileMap globalMap, List<UIBundle> bundles)
	{
		var chg = Changed;

		if (_path == null || !chg)
			return;

		Changed = false;

		_data = new CacheData
		{
			ScssHeaderLength = globalMap.GetScssGlobals().Length,
			ScssHeaderCrc = globalMap.GetScssCrc(),
			GlobalFileMap = new List<CachedFileMeta>(),
			Bundles = new Dictionary<string, BundleCacheData>()
		};

		foreach (var entry in globalMap.SortedGlobalFiles)
		{
			if (entry.ModifiedTicksTs == 0)
			{
				continue;
			}

			_data.GlobalFileMap.Add(new CachedFileMeta
			{
				ModifiedTicksTs = entry.ModifiedTicksTs,
				FileSize = entry.FileSize,
				Path = entry.Path,
				Content = entry.TranspiledContent,
				Templates = entry.Templates,
				CustomTypeData = entry.CustomTypeData
			});
		}

		foreach (var bundle in bundles)
		{
			if (!_data.Bundles.TryGetValue(bundle.RootName, out BundleCacheData bundleCacheData))
			{
				// Bundles can use the same root name
				bundleCacheData = new BundleCacheData();
				_data.Bundles[bundle.RootName] = bundleCacheData;
			}

			foreach (var kvp in bundle.FileMap)
			{
				var entry = kvp.Value;
				if (entry.TranspiledContent == null || entry.ModifiedTicksTs == 0)
				{
					continue;
				}

				bundleCacheData.FileMap[kvp.Key] = new CachedFileMeta
				{
					ModifiedTicksTs = entry.ModifiedTicksTs,
					FileSize = entry.FileSize,
					Path = entry.Path,
					Content = entry.TranspiledContent,
					Templates = entry.Templates,
					CustomTypeData = entry.CustomTypeData
				};
			}
		}

		await WriteToFileAsync();
	}

	private async Task WriteToFileAsync()
	{
		if (_cacheDir == null || _path == null)
			return;

		Directory.CreateDirectory(_cacheDir);

		var options = new JsonSerializerOptions
		{
			WriteIndented = true
		};

		string json = JsonSerializer.Serialize(_data, options);
		await File.WriteAllTextAsync(_path, json, Encoding.UTF8);
	}

	/// <summary>
	/// Gets a file with the given key if present in the cache.
	/// </summary>
	/// <param name="bundle"></param>
	/// <param name="key"></param>
	/// <returns></returns>
	public CachedFileMeta GetFile(UIBundle bundle, string key)
	{
		if (_data == null || _data.Bundles == null)
			return null;

		string bundleName = bundle.RootName;
		if (!_data.Bundles.TryGetValue(bundleName, out var bundleSet))
			return null;

		if (!bundleSet.FileMap.TryGetValue(key, out var file))
			return null;

		return file;
	}

	/// <summary>
	/// True if the given file changed.
	/// </summary>
	/// <param name="metaA"></param>
	/// <param name="metaB"></param>
	/// <returns></returns>
	public bool FileChanged(CachedFileMeta metaA, CachedFileMeta metaB)
	{
		if (metaA == null || metaB == null)
			return true;

		return metaA.ModifiedTicksTs != metaB.ModifiedTicksTs ||
			   metaA.FileSize != metaB.FileSize ||
			   metaA.Path != metaB.Path;
	}

	/// <summary>
	/// True if the given file changed.
	/// </summary>
	/// <param name="metaA"></param>
	/// <param name="metaB"></param>
	/// <returns></returns>
	public bool FileChanged(CachedFileMeta metaA, FileMeta? metaB)
	{
		if (metaA == null || metaB == null)
			return true;

		return metaA.ModifiedTicksTs != metaB.Value.LastWriteUtc ||
			   metaA.FileSize != metaB.Value.Length;
	}

	/// <summary>
	/// Loads the given file at the specified path.
	/// </summary>
	/// <param name="path"></param>
	/// <returns></returns>
	private async Task<string> LoadTextFileAsync(string path)
	{
		string fileContent = await File.ReadAllTextAsync(path, Encoding.UTF8);
		return fileContent;
	}
}

/// <summary>
/// Metadata for a file in the cache.
/// </summary>
public class CachedFileMeta
{
	/// <summary>
	/// Unix timestamp of last modified time
	/// </summary>
	public long ModifiedTicksTs { get; set; }

	/// <summary>
	/// Relative filepath
	/// </summary>
	public string Path { get; set; } = "";
	/// <summary>
	/// File size
	/// </summary>
	public long FileSize { get; set; }

	/// <summary>
	/// The current file content
	/// </summary>
	public string Content { get; set; }
	/// <summary>
	/// The set of templates collected from the file
	/// </summary>
	public List<TemplateLiteral> Templates { get; set; }
	/// <summary>
	/// The set of custom type data collected from the file
	/// </summary>
	public JArray CustomTypeData { get; set; }
}

/// <summary>
/// Cached files for a particular bundle
/// </summary>
public class BundleCacheData
{
	/// <summary>
	/// The files in the bundle
	/// </summary>
	public Dictionary<string, CachedFileMeta> FileMap { get; set; } = new();
}