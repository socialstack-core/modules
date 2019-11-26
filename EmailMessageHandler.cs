using System;
using Api.Configuration;
using Microsoft.Extensions.Configuration;
using SmtpServer;
using System.Threading;
using SmtpServer.Storage;
using SmtpServer.Protocol;
using SmtpServer.Mail;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Eventing;
using Api.Contexts;

namespace Api.EmailReceiver
{
    /// <summary>
    /// Receives emails via SMTP, optionally with TLS.
    /// Intanced automatically. Use Injection to use this service, or Startup.Services.Get.
    /// </summary>
    public partial class EmailMessageHandler : MessageStore
    {
        /// <summary>
        /// Called when an email was received.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="transaction"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override async Task<SmtpResponse> SaveAsync(ISessionContext context, IMessageTransaction transaction, CancellationToken cancellationToken)
        {
            var textMessage = (ITextMessage)transaction.Message;

            var message = MimeKit.MimeMessage.Load(textMessage.Content);

            // In an anonymous context - this runs as no particular user always:
            var ctx = new Context();

            await Events.EmailAfterReceive.Dispatch(ctx, message);

            return SmtpResponse.Ok;
        }
    }

}