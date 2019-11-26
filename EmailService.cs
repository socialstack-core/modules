using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Api.Database;
using Microsoft.Extensions.Configuration;
using System.Text;
using Microsoft.AspNetCore.Http;
using System.ComponentModel;
using Api.Configuration;
using Api.CanvasRenderer;
using Api.Permissions;

namespace Api.Emails
{
	/// <summary>
	/// Handles sending out emails.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class EmailService : IEmailService
    {
        private EmailConfig _configuration;
        private IDatabaseService _database;
		private ICanvasRendererService _canvasRenderer;
		private readonly Query<EmailTemplate> selectQuery;
		private readonly Query<EmailTemplate> selectByKeyQuery;
		private readonly Query<EmailTemplate> listQuery;

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public EmailService(IDatabaseService database, ICanvasRendererService canvasRenderer)
        {
            _configuration = AppSettings.GetSection("Email").Get<EmailConfig>();
            _database = database;
			_canvasRenderer = canvasRenderer;

			listQuery = Query.List<EmailTemplate>();
			selectQuery = Query.Select<EmailTemplate>();
			selectByKeyQuery = Query.Select<EmailTemplate>();
			selectByKeyQuery.Where().EqualsArg("Key", 0);
		}

		/// <summary>
		/// Renders an email template by its ID.
		/// </summary>
		/// <param name="templateId">The ID of the template to render</param>
		/// <param name="context">The context to use. Provides values which will be substituted into the template.</param>
		/// <returns></returns>
		public async Task<RenderedCanvas> RenderTemplate(int templateId, CanvasContext context)
        {
            var template = await _database.Select(selectQuery, templateId);

            if (template == null)
            {
                throw new Exception("Email template with ID '" + templateId + "' doesn't exist in your EmailTemplate database table.");
            }

            return await RenderTemplate(template, context);
        }

		/// <summary>
		/// Renders an email template by its key.
		/// </summary>
		/// <param name="templateKey">The key of the template to render, e.g. "forgot_password".</param>
		/// <param name="context">The context to use. Provides values which will be substituted into the template.</param>
		/// <returns></returns>
		public async Task<RenderedCanvas> RenderTemplate(string templateKey, CanvasContext context)
        {
            var template = await _database.Select(selectByKeyQuery, templateKey);

			if (template == null)
            {
                throw new Exception("Email template with Key '" + templateKey + "' doesn't exist in your email_templates database table.");
            }

            return await RenderTemplate(template, context);
        }

        /// <summary>
        /// Renders an email template by its ID.
        /// </summary>
        /// <param name="template">The name of the template to render</param>
        /// <param name="context">The context to use. Provides values which will be substituted into the template.</param>
        /// <returns></returns>
        public async Task<RenderedCanvas> RenderTemplate(EmailTemplate template, CanvasContext context)
        {
			return await _canvasRenderer.Render(template.BodyJson, context);
        }

		/// <summary>
		/// Sends a templated email to the given address.
		/// </summary>
		/// <param name="toAddress">The email address to send the email to.</param>
		/// <param name="templateName">The template name to use.</param>
		/// <param name="context">The email context.</param>
		/// <param name="messageId">Optional value to use as the Message-Id header.</param>
		/// <param name="fromAccount">Optional account the email will be set as from. 
		/// Uses the default one from the config otherwise.</param>
		public async Task SendTemplate(string toAddress, string templateName, CanvasContext context, string messageId = null, EmailAccount fromAccount = null)
        {
            // Render the email:
            var renderedEmail = await RenderTemplate(templateName, context);

            // Send it:
            Send(toAddress, renderedEmail.Title, renderedEmail.Body, messageId, fromAccount);
        }

        private EmailAccount GetSender(string shortcode)
        {
            if (string.IsNullOrEmpty(shortcode))
            {
                // Default should be assumed elsewhere
                return null;
            }

            _configuration.Accounts.TryGetValue(shortcode, out EmailAccount result);
            return result;
        }

		/// <summary>
		/// Direct sends an email to the given address. Doesn't block this thread.
		/// </summary>
		/// <param name="toAddress">Target email address.</param>
		/// <param name="subject">Email subject.</param>
		/// <param name="body">Email body (HTML).</param>
		/// <param name="messageId">Optional value to use as the Message-Id header.</param>
		/// <param name="fromAccount">Optional account the email will be set as from. 
		/// Uses the default one from the config otherwise.</param>
		public void Send(string toAddress, string subject, string body, string messageId = null, EmailAccount fromAccount = null)
        {
            if (fromAccount == null)
            {
                fromAccount = _configuration.Accounts["default"];
            }

            SmtpClient client = new SmtpClient(fromAccount.Server);
            client.UseDefaultCredentials = false;
            client.Credentials = new NetworkCredential(fromAccount.User, fromAccount.Password);
            client.SendCompleted += OnSendComplete;
            client.Port = fromAccount.Port;
            client.EnableSsl = fromAccount.Encrypted;

            MailMessage mailMessage = new MailMessage();
            mailMessage.IsBodyHtml = true;

            if (!string.IsNullOrEmpty(messageId))
            {
                // Got a message ID:
                mailMessage.Headers.Add("Message-Id", messageId);
            }
            
            mailMessage.DeliveryNotificationOptions = DeliveryNotificationOptions.OnSuccess | DeliveryNotificationOptions.OnFailure | DeliveryNotificationOptions.Delay;
            mailMessage.From = new MailAddress(fromAccount.FromAddress);
            mailMessage.To.Add(toAddress);
            mailMessage.Body = body;

            if (!string.IsNullOrEmpty(fromAccount.ReplyTo))
            {
                mailMessage.ReplyToList.Add(fromAccount.ReplyTo);
            }

            mailMessage.Subject = subject;
            
            client.SendAsync(mailMessage, null);
        }

        private void OnSendComplete(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
               System.IO.File.AppendAllText("log.txt", "\r\nEmail sending cancelled");
            }
            else if (e.Error != null)
            {
                System.IO.File.AppendAllText("log.txt", "\r\nEmail send failed: " + e.Error.ToString());
            }
            else
            {
                System.IO.File.AppendAllText("log.txt", "\r\nEmail successfully sent");
            }
        }

        /// <summary>
        /// Resolves the given URL into an absolute URL suitable for use within emails.
        /// Note the siteUrl must start with a forward slash. E.g. /hello.
        /// </summary>
        /// <param name="siteUrl">The site relative URL, starting with /</param>
        /// <returns></returns>
        public string ResolveUrl(string siteUrl)
        {
            return Api.Configuration.AppSettings.Configuration["PublicUrl"] + siteUrl;
        }

		/// <summary>
		/// List all email templates.
		/// </summary>
		/// <returns></returns>
		public async Task<List<EmailTemplate>> List(Filter filter)
		{
			return await _database.List(listQuery, filter);
		}

	}

}
