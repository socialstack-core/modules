using Api.Uploader;

namespace Api.Eventing
{

    public partial class Events
    {
        /// <summary>
        /// Called after a file is transcoded
        /// </summary>
        public static EventHandler<Upload> UploadAfterTranscode;
    }
}
