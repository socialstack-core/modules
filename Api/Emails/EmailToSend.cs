using System.Collections.Generic;
using System.Net.Mail;

namespace Api.Emails;


/// <summary>
/// A request to send an email.
/// </summary>
public struct EmailToSend
{
	/// <summary>
	/// True if it has been handled.
	/// </summary>
	public bool Handled;

	/// <summary>Target email address.</summary>
	public string ToAddress;

	/// <summary>
	/// To name (if any).
	/// </summary>
	public string ToName;

	/// <summary>Email subject.</summary>
	public string Subject;

	/// <summary>Email body (HTML).</summary>
	public string Body;

	/// <summary>Email body (Plain, if any).</summary>
	public string BodyPlain;

	/// <summary>Optional message ID.</summary>
	public string MessageId;

	/// <summary>Optionally select a particular from account. The default (in your appsettings.json) is used otherwise.</summary>
	public EmailAccount FromAccount;

	/// <summary>Optional attachments.</summary>
	public IEnumerable<Attachment> Attachments;

	/// <summary>Optional headers. E.g. put Reply-To in here.</summary>
	public Dictionary<string, string> AdditionalHeaders;
}