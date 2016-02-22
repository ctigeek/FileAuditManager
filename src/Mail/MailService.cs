using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using log4net;

namespace FileAuditManager.Mail
{
    internal class MailService : IMailService
    {
        private static ILog log = LogManager.GetLogger(typeof(MailService));
        private readonly bool sendMailOnAuditFailure;
        private readonly string mailgunUrl;
        private readonly string mailgunApiKey;
        private readonly string auditEmailToAddress;
        private readonly string auditEmailFromAddress;

        public MailService(bool sendMailOnAuditFailure, string mailgunUrl, string mailgunApiKey, string auditEmailToAddress, string auditEmailFromAddress)
        {
            this.sendMailOnAuditFailure = sendMailOnAuditFailure;
            this.mailgunUrl = mailgunUrl;
            this.mailgunApiKey = mailgunApiKey;
            this.auditEmailToAddress = auditEmailToAddress;
            this.auditEmailFromAddress = auditEmailFromAddress;
        }

        public async Task SendAuditEmail(string subject, string message )
        {
            try
            {
                if (!sendMailOnAuditFailure) return;

                await SendMailgunEmail(subject, message);
            }
            catch (Exception ex)
            {
                log.Error("Error sending email:", ex);
            }
        }

        private async Task SendMailgunEmail(string subject, string body)
        {
            var sendTo = auditEmailToAddress.Split(',');
            var formContentData = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("from", auditEmailFromAddress),
                new KeyValuePair<string, string>("subject", subject),
                new KeyValuePair<string, string>("text", body),
            };
            formContentData.AddRange(sendTo.Select(s => new KeyValuePair<string, string>("to", s)).ToList());

            using (var httpclient = new HttpClient())
            {
                var byteArray = Encoding.ASCII.GetBytes("api:" + mailgunApiKey);
                httpclient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
                var request = new HttpRequestMessage(HttpMethod.Post, mailgunUrl)
                {
                    Content = new FormUrlEncodedContent(formContentData.ToArray())
                };
                var response = await httpclient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    throw new ApplicationException("The call to mailgun returned " + response.StatusCode);
                }
            }
        }
    }
}
