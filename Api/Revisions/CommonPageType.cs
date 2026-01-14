namespace Api.Pages;

/// <summary>
/// The types of common page.
/// </summary>
public partial class CommonPageType
{
	/// <summary>
	/// Admin pages of the form /en-admin/{CONTENT_TYPE_NAME}/revision/{ID}
	/// </summary>
	public static readonly CommonPageType AdminRevisionEdit = new CommonPageType("admin_revision_edit");
}