using Api.CanvasRenderer;
using Api.Contexts;
using Api.Database;
using Api.Pages;
using Api.SocketServerLibrary;
using Api.Startup;
using System;
using System.Threading.Tasks;

namespace Api.Emails;


/// <summary>
/// A cached canvas generator for email templates which stores multiple generators (one per locale).
/// </summary>
public class EmailCanvasGenerator
{
	/// <summary>
	/// The time it was cached. 
	/// If the template was edited more recently than this, the cached entry is stale.
	/// </summary>
	public DateTime CachedUtc;

	/// <summary>
	/// The email template ID.
	/// </summary>
	public uint TemplateId;

	/// <summary>
	/// The cached generators (per locale, indexed by localeId-1).
	/// </summary>
	public CanvasGenerator[] Generators;

	/// <summary>
	/// The primary type in the email. Is only ever set if it's actually a content type.
	/// </summary>
	public Type PrimaryType;

	/// <summary>
	/// The PC service if primaryType is a content type.
	/// </summary>
	public AutoService PrimaryContentService;

	/// <summary>
	/// Creates a new generator for the given template.
	/// </summary>
	/// <param name="template"></param>
	public EmailCanvasGenerator(EmailTemplate template)
	{
		TemplateId = template.Id;
		CachedUtc = template.EditedUtc;
		
		if (!string.IsNullOrEmpty(template.PrimaryContentType))
		{
			PrimaryContentService = Services.Get(template.PrimaryContentType.ToLower() + "service");

			if (PrimaryContentService != null)
			{
				PrimaryType = PrimaryContentService.ServicedType;
			}
		}
	}

	/// <summary>
	/// Generate the canvas for the body of the email in to the given writer.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="latest"></param>
	/// <param name="writer"></param>
	/// <param name="primaryObject"></param>
	/// <returns></returns>
	public async ValueTask Generate(Context context, EmailTemplate latest, Writer writer, object primaryObject)
	{
		var localeId = context.LocaleId;

		if (localeId == 0)
		{
			// Default site locale
			localeId = 1;
		}

		if (localeId > 400)
		{
			// Excessive locale ID - reject
			throw new PublicException("LocaleId is too high", "locale/excessive_id");
		}

		// Are the generators stale?
		if (latest.EditedUtc != CachedUtc)
		{
			CachedUtc = latest.EditedUtc;
			Generators = null;
		}

		// First ensure the canvas generator exists:
		var gens = Generators;

		if (gens == null)
		{
			gens = new CanvasGenerator[localeId > 10 ? localeId : 10];
			Generators = gens;
		}
		else if (gens.Length < localeId)
		{
			Array.Resize(ref gens, (int)localeId);
			Generators = gens;
		}

		var generator = gens[localeId];

		if (generator == null)
		{
			// Create the generator:
			generator = new CanvasGenerator(latest.BodyJson.Get(localeId).ValueOf(), PrimaryType);
			gens[localeId] = generator;
		}

		await generator.Generate(context, writer, new PageWithTokens() { // struct
			// NB: PrimaryService is not needed - it is only used for pulling meta fields 
			// which emails don't use.
			// Similarly, they don't use any other field - url tokens etc.

			// PO is only usable in graphs if it is actually a content type.
			PrimaryObject = PrimaryType != null ? primaryObject : null
		});
	}
}