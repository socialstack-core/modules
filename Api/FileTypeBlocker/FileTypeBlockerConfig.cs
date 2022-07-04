using Api.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.FileTypeBlocker
{
	/// <summary>
	/// The appsettings.json config block for the file type blocker.
	/// </summary>
    public class FileTypeBlockerConfig : Config
    {
		/// <summary>
		/// Filetypes to reject uploading. If neither this or whitelist is specified then a default set is used instead.
		/// </summary>
		public string[] Blacklist {get; set;} = new string[]{
			"exe",
			"msi",
			"pif",
			"application",
			"app",
			"com",
			"scr",
			"hta",
			"cpl",
			"msc",
			"jar",
			"js",
			"jse",
			"html",
			"css",
			"bat",
			"cmd",
			"sh",
			"php",
			"ps1",
			"ps1xml",
			"ps2",
			"scf",
			"lnk",
			"inf",
			"reg",
			"ps2xml",
			"psc1",
			"psc2",
			"htaccess",
			"aspx",
			"asp",
			"zip",
			"rar",
			"tar",
			"gz",
			"img",
			"iso",
			"bin"
		};

		/// <summary>
		/// Filetypes to only permit. If neither this or whitelist is specified then a default set is used instead.
		/// </summary>
		public string[] Whitelist {get; set;} = new string[]{
			"apng",
			"avif",
			"png",
			"jpg",
			"jpeg",
			"tiff",
			"tif",
			"bmp",
			"heif",
			"heic",
			"gif",
			"webp",
			"webm",
			"mp3",
			"pdf",
			"svg",
			"svgz",
			"jp2",
			"j2k",
			"jpf",
			"jpx",
			"jpm",
			"mj2",
			"wav",
			"mp4",
			"webm",
			"mpeg",
			"mpg",
			"mp2",
			"avi",
			"mov",
			"mkv"
		};

		/// <summary>
		/// True if it should use the whitelist technique, but with the default set.
		/// </summary>
		public bool UseWhitelist {get; set;}
	}
	
}
