using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.StoryAttachments
{
	/// <summary>
	/// Implement this interface on a type to automatically add story attachment support.
	/// If you're extending a built in type, it's best to put the extension in its own module under Api/StoryAttachments/Types/{TypeName}.
	/// </summary>
	public partial interface IHaveStoryAttachments
    {
		/// <summary>
		/// The attachments on this story-like content. E.g. images in your feed, or websites attached to a channel message.
		/// </summary>
		List<StoryAttachment> Attachments {get; set;}
	}
}
