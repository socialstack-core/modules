using System;
using Api.Configuration;
using Microsoft.Extensions.Configuration;
using SmtpServer;
using System.Threading;


namespace Api.EmailReceiver
{
    /// <summary>
    /// Receives emails via SMTP, optionally with TLS.
    /// Intanced automatically. Use Injection to use this service, or Startup.Services.Get.
    /// </summary>
    public partial class EmailReceiverService : IEmailReceiverService
    {
        // private PushNotificationConfig _configuration;
		
        /// <summary>
        /// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
        /// </summary>
        public EmailReceiverService()
        {
            // _configuration = AppSettings.GetSection("PushNotifications").Get<PushNotificationConfig>();

            var options = new SmtpServerOptionsBuilder()
            .ServerName("localhost")
            .Port(25, 587)
            .MessageStore(new EmailMessageHandler())
            .Build();

            var smtpServer = new SmtpServer.SmtpServer(options);
            smtpServer.StartAsync(CancellationToken.None);

        }
		
    }
}