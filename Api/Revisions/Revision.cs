using Api.AutoForms;
using Api.Database;
using Api.Startup;
using Api.Users;
using System;

namespace Api.Revisions;


/// <summary>
/// A specific version of a piece of content.
/// </summary>
[HasVirtualField("realUser", typeof(User), "ImpersonatorUserId")]
public partial class Revision<T, ID> : UserCreatedContent<ID>
	where T : Content<ID>, new()
	where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
{

	/// <summary>
	/// The underlying content.
	/// </summary>
	public string ContentJson;

	/// <summary>
	/// The revision number (incremental for a given contentId).
	/// </summary>
	public int RevisionNumber;

	/// <summary>
	/// The ID of the content.
	/// </summary>
	public ID ContentId;

	/// <summary>
	/// There is only 1 draft of a given piece of content at a time.
	/// </summary>
	public bool IsDraft;

	/// <summary>
	/// If populated then auto publish this content on the required date 
	/// </summary>
	public DateTime? PublishDraftDate;

	/// <summary>
	/// 1 = Create, 2 = Update, 3 = Delete.
	/// </summary>
	public uint ActionType;

	/// <summary>
	/// Non-zero if this revision was created whilst someone was being impersonated.
	/// </summary>
	public uint ImpersonatorUserId;
}