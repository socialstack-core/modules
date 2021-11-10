using Api.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.FFmpeg
{
	/// <summary>
	/// The appsettings.json config block for push notification config.
	/// </summary>
    public class FFmpegConfig : Config
    {
		/// <summary>
		/// Set this to true if uploads should be automatically transcoded.
		/// </summary>
		public bool TranscodeUploads { get; set; }

		/// <summary>
		/// True if files like mp4 should be checked if they are audio only.
		/// </summary>
		public bool ProbeGenericContainers { get; set; }

		/// <summary>
		/// Video transcode target format(s). "hls" is the default if not specified. Can comma separate, e.g. "hls,h264/aac"
		/// </summary>
		public string TranscodeTargets { get; set; }
		
		/// <summary>
		/// Max video length permitted. If not set, there is no limit.
		/// Only applies to non-admin users.
		/// </summary>
		public uint MaxVideoLengthSeconds { get; set; }
	}
	
}
