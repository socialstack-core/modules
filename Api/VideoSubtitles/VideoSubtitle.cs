using System;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.VideoSubtitles
{
	
	/// <summary>
	/// A VideoSubtitle
	/// </summary>
	public partial class VideoSubtitle : VersionedContent<uint>
	{
		/// <summary>
		/// The upload the subtitle file is attached to.
		/// </summary>
		public uint UploadId;

		/// <summary>
		/// vtt or srt file.
		/// </summary>
		public string SubtitleFileRef;

		/// <summary>
		/// Locale that these subtitles are for.
		/// </summary>
		public uint LocaleId;

        /// <summary>
        /// The subtitle name which will appear in the player.
        /// </summary>
        [DatabaseField(Length = 200)]
		public string Name;

	}

}