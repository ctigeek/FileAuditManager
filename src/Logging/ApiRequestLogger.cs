using System;
using System.Configuration;
using System.IO;
using log4net;
using Microsoft.Owin;

namespace FileAuditManager.Logging
{
    internal static class ApiRequestLogger
    {
        public const string RequestLogName = "RequestLog";
        internal static ILog RequestLog { get; set; } = LogManager.GetLogger(RequestLogName);
        private static readonly string ComputerName = Environment.MachineName;
        private static readonly string ApplicationName = ConfigurationManager.AppSettings["ApplicationName"] ?? Path.GetFileNameWithoutExtension(typeof (ApiRequestLogger).Assembly.Location);

        public static void LogComment(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                RequestLog.WarnFormat("#{0}", message);
            }
        }

        public static void Log(IOwinRequest request, IOwinResponse response, long responseTime)
        {
            var username = (string.IsNullOrEmpty(request.User?.Identity?.Name)) ? "-" : request.User.Identity.Name;
            var queryString = string.IsNullOrEmpty(request.QueryString.Value) ? "-" : request.QueryString.Value;
            var useragent = (request.Headers.Get("User-Agent") ?? "-").Replace(' ', '+');
            var referer = request.Headers.Get("Referer") ?? "-";
            var message = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} {ApplicationName} {ComputerName} {request.LocalIpAddress} {request.Method} {request.Uri.GetLeftPart(UriPartial.Path)} {queryString} {request.LocalPort} {username} {request.RemoteIpAddress} {useragent} {referer} {response.StatusCode} 0 0 {responseTime}";

            RequestLog.Warn(message);
        }
    }
}