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
		/// Set this to true if uploads should be automatically transcoded to h264/aac.
		/// </summary>
		public bool TranscodeUploads { get; set; }
	}
	
}
