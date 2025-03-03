using System.IO;
using System;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;

namespace Api.Startup;

/// <summary>
/// Helper for *nix file permissions.
/// </summary>
public static class Chmod
{
	[DllImport("libc", EntryPoint="chmod", SetLastError = true)]
	[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "interop")]
	private static extern int chmod(string pathname, int mode);

	// user permissions
	/// <summary>
	/// user read
	/// </summary>
	public const int S_IRUSR = 0x100;

	/// <summary>
	/// user write
	/// </summary>
	public const int S_IWUSR = 0x80;

	/// <summary>
	/// user exec
	/// </summary>
	public const int S_IXUSR = 0x40;

	// group permission
	/// <summary>
	/// group read
	/// </summary>
	public const int S_IRGRP = 0x20;

	/// <summary>
	/// group write
	/// </summary>
	public const int S_IWGRP = 0x10;

	/// <summary>
	/// group exec
	/// </summary>
	public const int S_IXGRP = 0x8;

	// other permissions

	/// <summary>
	/// other read
	/// </summary>
	public const int S_IROTH = 0x4;

	/// <summary>
	/// other write
	/// </summary>
	public const int S_IWOTH = 0x2;

	/// <summary>
	/// other exec
	/// </summary>
	public const int S_IXOTH = 0x1;
	
	/// <summary>
	/// Sets 644 global read (+owner write) file permissions on a *nix platform
	/// </summary>
	/// <param name="filename"></param>
	public static void SetRead(string filename)
	{
		// 644
		var perms =
		S_IRUSR | S_IWUSR
		| S_IRGRP
		| S_IROTH;

		if (0 != chmod(Path.GetFullPath(filename), (int)perms))
			throw new Exception("Could not set Unix socket permissions");
	}
	
	/// <summary>
	/// Sets file permissions on a *nix platform
	/// </summary>
	/// <param name="filename"></param>
	/// <param name="perms"></param>
	public static void Set(string filename, int perms = 0)
	{
		if(perms == 0){
			// 777
			perms =
			S_IRUSR | S_IXUSR | S_IWUSR
			| S_IRGRP | S_IXGRP | S_IWGRP
			| S_IROTH | S_IXOTH | S_IWOTH;
		}
		
		if (0 != chmod(Path.GetFullPath(filename), (int)perms))
			throw new Exception("Could not set Unix socket permissions");
	}
}
