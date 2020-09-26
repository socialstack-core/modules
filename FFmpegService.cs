using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Api.Contexts;
using Api.Eventing;
using Api.Uploader;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Configuration;
using Api.Configuration;
using Microsoft.Extensions.Hosting;
using System.IO;

namespace Api.FFmpeg
{
	/// <summary>
	/// This service is used to invoke ffmpeg on the command line. You must have it in your path or installed on Linux.
	/// </summary>
	public partial class FFmpegService
	{
		private bool Verbose = false;

		/// <summary>
		/// All active processes.
		/// </summary>
		private List<Process> All = new List<Process>();
		private bool _stopping;
		private FFmpegConfig _configuration;
		private UploadService _uploads;
		private bool hlsTranscode;
		private bool h264Transcode;
		
		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public FFmpegService(UploadService uploads, IHostApplicationLifetime lifetime)
		{
			_uploads = uploads;
			_configuration = AppSettings.GetSection("FFmpeg").Get<FFmpegConfig>();
			
			if(_configuration != null && _configuration.TranscodeUploads)
			{
				var formats = _configuration.TranscodeTargets;
				
				if(string.IsNullOrEmpty(formats)){
					formats = "hls";
				}
				
				var targetFormats = formats.Trim().Split(',');
				
				for(var i=0;i<targetFormats.Length;i++){
					var fmt = targetFormats[i].Trim().ToLower();
					
					if(fmt == "hls"){
						hlsTranscode = true;
					}else if(fmt == "h264" || fmt == "h264/aac"){
						h264Transcode = true;
					}
					
				}
				
				StartUploadTranscode();
			}

			lifetime.ApplicationStopping.Register(() => {
				StopAll();
			});

			AppDomain.CurrentDomain.ProcessExit += (object sender, EventArgs e) => {
				StopAll();
			};

		}

		/// <summary>
		/// Stops all FFMpeg processes.
		/// </summary>
		private void StopAll()
		{
			_stopping = true;

			if (All == null)
			{
				return;
			}

			foreach (var process in All)
			{
				if (process != null && !process.HasExited)
				{
					process.Kill(true);
				}
			}

			All = null;
		}

		/// <summary>
		/// Enabled upload transcoding
		/// </summary>
		public void StartUploadTranscode()
		{
			Events.Upload.BeforeCreate.AddEventListener((Context context, Upload upload) =>
            {
				// Something else might've blocked it:
				if(upload == null){
					return new ValueTask<Upload>(upload);
				}
				
				switch (upload.FileType)
                {
                    // Videos
                    case "avi":
                    case "wmv":
                    case "ts":
                    case "m3u8":
                    case "ogv":
                    case "flv":
                    case "h264": 
                    case "h265":
                        upload.IsVideo = true;
                    break;

                    // Audio
                    case "wav":
                    case "oga":
                    case "aac":
                    case "mp3":
                    case "opus":
                    case "weba":
                        upload.IsAudio = true;
                    break;

                    // Maybe A/V
                    case "webm":
                    case "ogg":
                    case "mp4":
                    case "mkv":
                    case "mpeg":
                    case "3g2":
                    case "3gp":
                    case "mov":
                    case "media":
						// Assume video for now - we can't probe it until we have the file itself.
						upload.IsVideo = true;
                    break;
					default:
						// do nothing.
					break;
                }
				
				// If A/V, we'll be transcoding shortly.
				if(upload.IsVideo || upload.IsAudio){
					
					if((upload.FileType == "mp4" && !hlsTranscode && h264Transcode) || upload.FileType == "mp3")
					{
						// it's already transcoded and available immediately.
						upload.TranscodeState = 2;
					}
					else
					{
						upload.TranscodeState = 1;
					}
				}

				return new ValueTask<Upload>(upload);
			});
			
			// On upload, auto transcode a/v files.
			Events.Upload.AfterCreate.AddEventListener(async (Context context, Upload upload) =>
            {
				// Something else might've blocked it, or we don't care about this file.
				if(upload == null || upload.TranscodeState != 1){
					return upload;
				}
				
                // If it's audio or video, transcode it.
                // Audio gets transcoded to mp3.
                // Video to mp4.
                string originalPathWithoutExt = upload.GetFilePath("original", true);
                string originalPath = originalPathWithoutExt + "." + upload.FileType;

				if (upload.IsVideo) {

					// Check if we need to probe it.
					switch (upload.Type)
					{
						case "webm":
						case "ogg":
						case "mp4":
						case "mkv":
						case "mpeg":
						case "3g2":
						case "3gp":
						case "mov":
						case "media":

							var result = await Probe("-i \"" + originalPath + "\"");
							var jobj = JObject.Parse(result);
							var streams = jobj.SelectToken("streams");

							if (streams != null && streams.HasValues)
							{
								// Yep it's definitely a video anyway.
							}
							else
							{
								// Is actually just audio - clear the video flag.
								upload.IsVideo = false;
								upload.IsAudio = true;
							}

							break;
						default:
						break;
					}
					
				}
				
				Transcode(context, upload);
				
				// File is:
				// upload.GetFilePath("original");

				return await Task.FromResult(upload);
			});
		}
		
		/// <summary>
		/// Transcodes the given upload now.
		/// </summary>
		public bool Transcode(Context context, Upload upload)
		{
			string originalPathWithoutExt = upload.GetFilePath("original", true);
			string originalPath = originalPathWithoutExt + "." + upload.FileType;

			string targetPath;
			if (upload.IsVideo)
			{
				// Is a video
				
				if(hlsTranscode){

					// Create the video dir:
					var baseDir = AppSettings.Configuration[upload.IsPrivate ? "ContentPrivate" : "Content"] + "video/" + upload.Id + "/";
					Directory.CreateDirectory(baseDir);

					Run("-i \"" + originalPath + "\" -c:v h264 -c:a aac -f ssegment -segment_list \"" + baseDir + "manifest.m3u8\" -segment_time 1 -hls_time 1 -g 30 \"" + baseDir + "chunk%d.ts\"", async () => {
						
						// Done! (NB can trigger twice if multi-transcoding)
						upload.TranscodeState = 2;
						await _uploads.Update(context, upload);
						await Events.UploadAfterTranscode.Dispatch(context, upload);
					});
				}
				
				if(h264Transcode){
					targetPath = originalPathWithoutExt + ".mp4";
					
					Run("-i \"" + originalPath + "\" \"" + targetPath + "\"", async () => {
						
						// Done! (NB can trigger twice if multi-transcoding)
						upload.TranscodeState = 2;
						await _uploads.Update(context, upload);
						await Events.UploadAfterTranscode.Dispatch(context, upload);
					});
				}
				
			}else if(upload.IsAudio){
				
				// Is audio
				targetPath = originalPathWithoutExt + ".mp3";
				Run("-i \"" + originalPath + "\" \"" + targetPath + "\"", async () => {
					
					// Done!
					upload.TranscodeState = 2;
					await _uploads.Update(context, upload);
					await Events.UploadAfterTranscode.Dispatch(context, upload);
				});
				
			}else{
				return false;
			}
			
			return true;
		}
		
		/// <summary>
		/// Runs Ffmpeg with the given cmd args.
		/// Callback based as this can be a little slow.
		/// </summary>
		/// <returns></returns>
		public void Run(string cmdArgs, Func<Task> onDone, Action<string> onData = null, Action<string> onErrorData = null)
		{
			var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
			
			Process process = new Process();

			if (!Verbose)
			{
				cmdArgs = "-hide_banner -loglevel fatal " + cmdArgs;
			}

			var cmd = "ffmpeg " + cmdArgs;
			
			// Configure the process using the StartInfo properties.
			process.StartInfo.FileName = isWindows ? "cmd.exe" : "/bin/bash";
			process.StartInfo.Arguments = isWindows ? "/C " + cmd : "-c \"" + cmd + "\"";
			process.StartInfo.WorkingDirectory = "";
			process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;
			process.StartInfo.RedirectStandardInput = true;
			process.StartInfo.ErrorDialog = false;
			process.StartInfo.CreateNoWindow = true;
			process.EnableRaisingEvents = true;
			
			process.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
			{
				if (string.IsNullOrEmpty(e.Data))
				{
					return;
				}
				
				if(onData != null)
				{
					onData(e.Data);
				}
				else
				{
					// Forward to our output stream:
					// Console.WriteLine(e.Data);
				}
			});

			process.ErrorDataReceived += new DataReceivedEventHandler((sender, e) =>
			{
				if (string.IsNullOrEmpty(e.Data))
				{
					return;
				}

				if (onErrorData != null)
				{
					onErrorData(e.Data);
				}
				else
				{
					Console.WriteLine(e.Data);
				}
			});
			
			process.Start();

			All.Add(process);

			Task.Run(async () => {
				process.BeginOutputReadLine();
				process.BeginErrorReadLine();
				process.WaitForExit();

				if (_stopping)
				{
					return;
				}

				All.Remove(process);

				if(onDone != null){
					await onDone();
				}
			});
		}

        /// <summary>
        /// Used to probe a media to check for video streams using ffprobe
        /// </summary>
        /// <param name="cmdArgs"></param>
        /// <returns></returns>
        public async Task<string> Probe(string cmdArgs)
        {
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

            Process process = new Process();

            var cmd = "ffprobe -v quiet -print_format json -select_streams v -show_streams " + cmdArgs;

            // Configure the process using the StartInfo properties.
            process.StartInfo.FileName = isWindows ? "cmd.exe" : "/bin/bash";
            process.StartInfo.Arguments = isWindows ? "/C " + cmd : "-c \"" + cmd + "\"";
            process.StartInfo.WorkingDirectory = "";
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.ErrorDialog = false;
            process.StartInfo.CreateNoWindow = true;
            process.EnableRaisingEvents = true;
			
			var builder = new StringBuilder();

			process.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
			{
				if (string.IsNullOrEmpty(e.Data))
				{
					return;
				}

				builder.Append(e.Data);
			});

			process.ErrorDataReceived += new DataReceivedEventHandler((sender, e) =>
			{
				if (string.IsNullOrEmpty(e.Data))
				{
					return;
				}

				Console.WriteLine(e.Data);
			});

			process.Start();
			
			await Task.Run(() => {
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
				
            });

            return builder.ToString();
        }
    }
}
