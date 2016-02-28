using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;

namespace FileAuditManager.Controllers
{
    public class HealthController : ApiController
    {
        public static bool IsHealthy { get; set; } = false;
        public static string ContentType { get; set; } = "text/plain";
        public static string ResponseFormat { get; set; } = "<%=status%>";
        public static string UpStatusString { get; set; } = "Up";
        public static string DownStatusString { get; set; } = "OOR";

        public static readonly DateTime ServiceStartTime = DateTime.UtcNow;
        public static readonly string ServerName = Environment.MachineName;
        
        public HttpResponseMessage Get()
        {
            var content = new StringContent(BuildResponse(), Encoding.UTF8, ContentType);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = content
            };
        }

        public HttpResponseMessage Put([FromUri] bool healthy)
        {
            try
            {
                IsHealthy = healthy;
                return Get();
            }
            catch (Exception)
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }
        }

        private string BuildResponse()
        {
            return ResponseFormat.Replace("<%=status%>", IsHealthy ? UpStatusString : DownStatusString)
                .Replace("<%=host%>", ServerName)
                .Replace("<%=uptime%>", DateTime.UtcNow.Subtract(ServiceStartTime).TotalSeconds.ToString());
        }
    }
}
