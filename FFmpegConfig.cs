using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.FFmpeg
{
	/// <summary>
	/// The appsettings.json config block for push notification config.
	/// </summary>
    public class FFmpegConfig
    {
		/// <summary>
		/// Set this to true if uploads should be automatically transcoded.
		/// </summary>
		public bool TranscodeUploads { get; set; }
		
		/// <summary>
		/// Video transcode target format(s). "hls" is the default if not specified. Can comma separate, e.g. "hls,h264/aac"
		/// </summary>
		public string TranscodeTargets { get; set; }
	}
	
}
