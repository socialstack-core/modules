namespace Api.Uploader
{
	
	public partial class Upload{
		
		/// <summary>True if this is a video.</summary>
		public bool IsVideo;
		/// <summary>True if this is audio.</summary>
		public bool IsAudio;
		/// <summary>The transcode state. 2 means it's been transcoded, 1 is transcode in progress.</summary>
		public int TranscodeState;
		
	}
	
}