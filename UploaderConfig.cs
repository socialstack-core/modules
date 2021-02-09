using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Uploader
{
	/// <summary>
	/// The appsettings.json config block for email config.
	/// </summary>
    public partial class UploaderConfig
    {
		/// <summary>
		/// When an image is uploaded, it'll be automatically resized to each of these sizes.
		/// </summary>
		public int[] ImageSizes = new int[]{ 32, 64, 100, 128, 200, 256, 512, 1024, 2048 };
    }
	
}
