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
using Api.Startup;

namespace Api.FFmpeg
{
	/// <summary>
	/// This service is used to invoke ffmpeg on the command line. You must have it in your path or installed on Linux.
	/// </summary>
	public partial class FFmpegService : AutoService
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
			_configuration = GetConfig<FFmpegConfig>();
			
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


			Events.Upload.BeforeCreate.AddEventListener(async (Context context, Upload upload) => {

				if (upload == null)
				{
					return null;
				}

				if (_configuration.MaxVideoLengthSeconds > 0 && upload.IsVideo && (context.Role == null || !context.Role.CanViewAdmin))
				{
					// We have a video length restriction.
					var duration = await GetDurationInSeconds(upload.TemporaryPath);

					if (duration.HasValue && duration > _configuration.MaxVideoLengthSeconds)
					{
						throw new PublicException("That video is too long - the max length is " + _configuration.MaxVideoLengthSeconds + " seconds.", "too_long");
					}
				}

				return upload;
			});

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

				if (_configuration.ProbeGenericContainers && upload.IsVideo) {

					// Check if we need to probe it.
					switch (upload.FileType)
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

							var result = await Probe("-v quiet -print_format json -select_streams v -show_streams -i \"" + originalPath + "\"");
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

				// Note that the transcode task does not wait for the complete transcoding - only writing of the manifest.
				await Transcode(context, upload);
				
				// File is:
				// upload.GetFilePath("original");

				return await Task.FromResult(upload);
			});
		}

		/// <summary>
		/// Creates an ffmpeg -i flag from an upload ref.
		/// </summary>
		/// <param name="uploadRef"></param>
		/// <param name="sizeName"></param>
		/// <returns></returns>
		public string InputFromRef(string uploadRef, string sizeName = "original")
		{
			return InputFromRef(FileRef.Parse(uploadRef), sizeName);
		}

		/// <summary>
		/// Gets the duration of the given file.
		/// </summary>
		/// <param name="localFilePath"></param>
		/// <returns></returns>
		public async Task<double?> GetDurationInSeconds(string localFilePath)
		{
			try
			{
				// Probe the file:
				var response = await Probe("-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1  \"" + localFilePath + "\"");

				// Format of the response is simply a textual time in seconds.
				if (!double.TryParse(response.Replace('\r', ' ').Replace('\n', ' ').Trim(), out double length))
				{
					return null;
				}

				return length;
			}
			catch (Exception e)
			{
				Console.WriteLine("Failed probing a video, but proceeding: " + e.ToString());

				return null;
			}
		}

		/// <summary>
		/// Creates an ffmpeg -i flag from an upload ref.
		/// </summary>
		/// <param name="uploadRef"></param>
		/// <param name="sizeName"></param>
		/// <returns></returns>
		public string InputFromRef(FileRef uploadRef, string sizeName = "original")
		{
			var fsPath = uploadRef.GetFilePath(sizeName);
			return "-i \"" + fsPath + "\" ";
		}

		/// <summary>
		/// Gets an output filesys path for the given ref and sizename.
		/// </summary>
		/// <param name="uploadRef"></param>
		/// <param name="sizeName"></param>
		/// <returns></returns>
		public string OutputForRef(string uploadRef, string sizeName = "transcoded")
		{
			return OutputForRef(FileRef.Parse(uploadRef), sizeName);
		}

		/// <summary>
		/// Gets an output filesys path for the given ref and sizename.
		/// </summary>
		/// <param name="outputRef"></param>
		/// <param name="sizeName"></param>
		/// <returns></returns>
		public string OutputForRef(FileRef outputRef, string sizeName)
		{
			var fsPath = outputRef.GetFilePath(sizeName);
			return " \"" + fsPath + "\"";
		}

		private string AddRendition(string chunkDirectory, int w, int h, int bitVid, int maxRate, int bufSize, int bitAudio)
		{
			return "-vf scale=w=" + w + ":h=" + h + ":force_original_aspect_ratio=decrease -c:a aac -ar 48000 -c:v h264 -profile:v main -crf 20 -sc_threshold 0 " +
			"-g 48 -keyint_min 48 -hls_time 4 -hls_playlist_type vod -b:v " + bitVid + "k -maxrate " + maxRate + "k -bufsize " + bufSize + "k -b:a " + bitAudio + "k " +
			"-hls_segment_filename \"" + chunkDirectory + h + "p_%03d.ts\" \"" + chunkDirectory + h + "p.m3u8\"";
		}

		/// <summary>
		/// Transcodes the given upload now.
		/// </summary>
		public async ValueTask<bool> Transcode(Context context, Upload upload)
		{
			// get the file
            var tempPathName = Path.GetTempPath();
            var tempUniqueId = Guid.NewGuid().ToString();
			var tempSourceFileName = tempPathName + tempUniqueId + "." + upload.FileType;

            if (!string.IsNullOrWhiteSpace(upload.TemporaryPath) && File.Exists(upload.TemporaryPath))
            {
				// try and use the local copy 
                File.Copy(upload.TemporaryPath, tempSourceFileName);
            }
            else
            {
				// get the file (may be in the cloud)
				var content = await upload.ReadFile();
                File.WriteAllBytes(tempSourceFileName, content);
            }

            string targetPath;
			if (upload.IsVideo)
			{
				// Is a video

				/*
				 
				  var args = [
					'-hide_banner',
					'-y',
					'-i', transcodeTask.inputPath
				  ];
				  
				 */

				if (hlsTranscode)
                {
					// Create a temporary directory:
                    var chunkDirectory = Path.Combine(tempPathName, tempUniqueId) + Path.DirectorySeparatorChar;
					Directory.CreateDirectory(chunkDirectory);

					var cmd = "-hide_banner -y -i \"" + tempSourceFileName + "\"";

					// Currently fixed at 4 predefined sizes:
					cmd += " " + AddRendition(chunkDirectory, 640, 360, 800, 856, 1200, 96);
					cmd += " " + AddRendition(chunkDirectory, 842, 480, 1400, 1498, 2100, 128);
					cmd += " " + AddRendition(chunkDirectory, 1280, 720, 2800, 2996, 4200, 128);
					cmd += " " + AddRendition(chunkDirectory, 1920, 1080, 5000, 5350, 7500, 192);

					// -i \"" + originalPath + "\" -c:v h264 -c:a aac -f ssegment -segment_list \"" + baseDir + "manifest.m3u8\" -segment_time 1 -hls_time 1 -g 30 \"" + baseDir + "chunk%d.ts\"

					var manifestContent = "#EXTM3U\n#EXT-X-VERSION:3\n#EXT-X-STREAM-INF:BANDWIDTH=800000,RESOLUTION=640x360\n360p.m3u8\n#EXT-X-STREAM-INF:BANDWIDTH=1400000,RESOLUTION=842x480\n480p.m3u8\n#EXT-X-STREAM-INF:BANDWIDTH=2800000,RESOLUTION=1280x720\n720p.m3u8\n#EXT-X-STREAM-INF:BANDWIDTH=5000000,RESOLUTION=1920x1080\n1080p.m3u8";

					await File.WriteAllTextAsync(chunkDirectory + "manifest.m3u8", manifestContent);
					
					Run(cmd, async () => {
						
						// Done! (NB can trigger twice if multi-transcoding)
						upload = await _uploads.Update(context, upload, (Context c, Upload upl, Upload orig) => {
							upl.TranscodeState = 2;
						}, DataOptions.IgnorePermissions);

						// now copy to local or cloud storage
                        DirectoryInfo d = new DirectoryInfo(chunkDirectory);
                        
                        foreach (var file in d.GetFiles())
                        {
							await Events.Upload.StoreFile.Dispatch(context, upload, file.FullName, $"chunks/{file.Name}");
						}

						// now tidy up 
						Directory.Delete(chunkDirectory, true);

						await Events.UploadAfterTranscode.Dispatch(context, upload);
					});
				}
				
				if(h264Transcode){
					targetPath = tempPathName + tempUniqueId + ".mp4";
					
					Run("-i \"" + tempSourceFileName + "\" \"" + targetPath + "\"", async () => {
						
						// Done! (NB can trigger twice if multi-transcoding)
						upload = await _uploads.Update(context, upload, (Context c, Upload upl, Upload orig) => {
							upl.TranscodeState = 2;
						}, DataOptions.IgnorePermissions);

						// now copy to local or cloud storage
						await Events.Upload.StoreFile.Dispatch(context, upload, targetPath, "original.mp4");

						await Events.UploadAfterTranscode.Dispatch(context, upload);
					});
				}
				
			}else if(upload.IsAudio){
				
				// Is audio
				targetPath = tempPathName + tempUniqueId + ".mp3";
				Run("-i \"" + tempSourceFileName + "\" \"" + targetPath + "\"", async () => {

					// Done!
					upload = await _uploads.Update(context, upload, (Context c, Upload upl, Upload orig) => {
						upl.TranscodeState = 2;
					}, DataOptions.IgnorePermissions);

					// now copy to local or cloud storage
					await Events.Upload.StoreFile.Dispatch(context, upload, targetPath, "original.mp3");

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

			var cmd = "ffmpeg -threads 1 " + cmdArgs;
			
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
					 Console.WriteLine(e.Data);
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

            var cmd = "ffprobe " + cmdArgs;

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
