using System.Collections.Generic;
using System.IO;

namespace Api.CanvasRenderer;


/// <summary>
/// A set of source file containers.
/// </summary>
public class SourceFileContainerSet {

	/// <summary>
	/// Additional custom containers (bundle-like) in this set.
	/// </summary>
	public List<SourceFileContainer> Containers = new List<SourceFileContainer>();

	/// <summary>
	/// All UI bundles.
	/// </summary>
	public List<UIBundle> Bundles;

	/// <summary>
	/// Gets a bundle by its root name (UI, Email, ..)
	/// </summary>
	/// <param name="name"></param>
	/// <returns></returns>
	public UIBundle GetBundle(string name)
	{
		foreach (var bundle in Bundles)
		{
			if (bundle.RootName == name)
			{
				return bundle;
			}
		}

		return null;
	}

}

/// <summary>
/// A collection of source files.
/// </summary>
public class SourceFileContainer {

	/// <summary>
	/// The name of the files in this group.
	/// </summary>
	public string RootName;
	
	/// <summary>
	/// Base file path to the location of source files in this container.
	/// </summary>
	public string SourcePath;

	/// <summary>
	/// The files in the group.
	/// </summary>
	public List<SourceFile> Files = new List<SourceFile>();

	/// <summary>
	/// Create a new collection of source files.
	/// </summary>
	/// <param name="sourcePath">Must be normalised and absolute.</param>
	/// <param name="rootName"></param>
	public SourceFileContainer(string sourcePath, string rootName)
	{
		SourcePath = sourcePath;
		RootName = rootName;
	}

	/// <summary>
	/// Adds the given source file.
	/// </summary>
	/// <param name="file"></param>
	public void Add(SourceFile file)
	{
		Files.Add(file);
	}

	/// <summary>
	/// Adds the given in memory file.
	/// </summary>
	/// <param name="rootRelativePath">File path relative to the project root.</param>
	/// <param name="content">The file content.</param>
	public void Add(string rootRelativePath, string content)
	{
		var file = new SourceFile(Path.GetFullPath(rootRelativePath), RootName, SourcePath);
		file.RawSource = content;
		Files.Add(file);
	}

}