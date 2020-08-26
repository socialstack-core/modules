using Api.Contexts;
using Api.Uploader;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.FFmpeg
{
	/// <summary>
	/// This service is used to invoke ffmpeg on the command line. You must have it in your path or installed on Linux.
	/// </summary>
	public partial interface IFFmpegService
	{
		
		/// <summary>
		/// Runs ffmpeg with the given cmd args.
		/// </summary>
		void Run(string cmdArgs, Func<Task> onDone, Action<string> onData = null, Action<string> onErrorData = null);
		
		/// <summary>
		/// Transcodes the given upload now.
		/// </summary>
		bool Transcode(Context ctx, Upload upload);
		
	}
}
