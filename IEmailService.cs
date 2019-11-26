using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Api.CanvasRenderer;
using Api.Permissions;

namespace Api.Emails
{
	/// <summary>
	/// Handles sending out emails.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IEmailService
	{
		/// <summary>
		/// Renders an email template by its key.
		/// </summary>
		/// <param name="templateKey">The key of the template to render, e.g. "forgot_password".</param>
		/// <param name="context">The context to use. Provides values which will be substituted into the template.</param>
		/// <returns></returns>
		Task<RenderedCanvas> RenderTemplate(string templateKey, CanvasContext context);

		/// <summary>
		/// Renders an email template by its ID.
		/// </summary>
		/// <param name="templateId">The ID of the template to render</param>
		/// <param name="context">The context to use. Provides values which will be substituted into the template.</param>
		/// <returns></returns>
		Task<RenderedCanvas> RenderTemplate(int templateId, CanvasContext context);

		/// <summary>
		/// Renders an email template by its ID.
		/// </summary>
		/// <param name="template">The name of the template to render</param>
		/// <param name="context">The context to use. Provides values which will be substituted into the template.</param>
		/// <returns></returns>
		Task<RenderedCanvas> RenderTemplate(EmailTemplate template, CanvasContext context);

		/// <summary>
		/// Sends a templated email to the given address.
		/// </summary>
		/// <param name="toAddress">The email address to send the email to.</param>
		/// <param name="templateName">The template name to use.</param>
		/// <param name="context">The email context.</param>
		/// <param name="messageId">Optional value to use as the Message-Id header.</param>
		/// <param name="fromAccount">Optional account the email will be set as from. 
		/// Uses the default one from the config otherwise.</param>
		Task SendTemplate(string toAddress, string templateName, CanvasContext context, string messageId = null, EmailAccount fromAccount = null);

		/// <summary>
		/// Direct sends an email to the given address. Doesn't block this thread.
		/// </summary>
		/// <param name="toAddress">Target email address.</param>
		/// <param name="subject">Email subject.</param>
		/// <param name="body">Email body (HTML).</param>
		/// <param name="messageId">Optional value to use as the Message-Id header.</param>
		/// <param name="fromAccount">Optional account the email will be set as from. 
		/// Uses the default one from the config otherwise.</param>
		void Send(string toAddress, string subject, string body, string messageId = null, EmailAccount fromAccount = null);

        /// <summary>
        /// Resolves the given URL into an absolute URL suitable for use within emails.
        /// Note the siteUrl must start with a forward slash. E.g. /hello.
        /// </summary>
        /// <param name="siteUrl">The site relative URL, starting with /</param>
        /// <returns></returns>
        string ResolveUrl(string siteUrl);
		
		/// <summary>
		/// List a filtered set of email templates.
		/// </summary>
		/// <returns></returns>
		Task<List<EmailTemplate>> List(Filter filter);

	}
}
