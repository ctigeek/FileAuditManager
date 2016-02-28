using System;
using System.Configuration;
using MongoDB.Driver;

namespace FileAuditManager
{
    static class Configuration
    {
        public const string ConnectionStringName = "fileaudit";

        public static readonly string ListenUrl = ConfigurationManager.AppSettings["ListenUrl"];

        public static readonly MongoUrl MongoUrl = new MongoUrl(ConfigurationManager.ConnectionStrings[ConnectionStringName].ConnectionString);

        public static readonly bool UseWindowsAuth = ConfigurationManager.AppSettings["UseWindowsAuth"] != null &&
                                                  ConfigurationManager.AppSettings["UseWindowsAuth"].Equals("true", StringComparison.InvariantCultureIgnoreCase);

        public static readonly long AuditTimerInSeconds = ConfigurationManager.AppSettings["AuditTimerInSeconds"] != null
            ? long.Parse(ConfigurationManager.AppSettings["AuditTimerInSeconds"])
            : 0;

        public static readonly bool SendMailOnAuditFailure = ConfigurationManager.AppSettings["SendMailOnAuditFailure"] != null &&
                                                  ConfigurationManager.AppSettings["SendMailOnAuditFailure"].Equals("true", StringComparison.InvariantCultureIgnoreCase);

        public static readonly string MailgunApiKey = ConfigurationManager.AppSettings["MailgunApiKey"] ?? string.Empty;

        public static readonly string MailgunUrl = ConfigurationManager.AppSettings["MailgunUrl"] ?? string.Empty;

        public static readonly string AuditEmailToAddress = ConfigurationManager.AppSettings["AuditEmailToAddress"] ?? string.Empty;

        public static readonly string AuditEmailFromAddress = ConfigurationManager.AppSettings["AuditEmailFromAddress"] ?? string.Empty;

        public static readonly string HealthResponseContentType = ConfigurationManager.AppSettings["HealthResponseContentType"] ?? "text.plain";
        public static readonly string HealthResponseFormatString = ConfigurationManager.AppSettings["HealthResponseFormatString"] ?? "<%=status%>";
        public static readonly string HealthResponseUpStatusString = ConfigurationManager.AppSettings["HealthResponseUpStatusString"] ?? "Up";
        public static readonly string HealthResponseDownStatusString = ConfigurationManager.AppSettings["HealthResponseDownStatusString"] ?? "OOR";

        public static void ValidateConnectionStrings()
        {
            var connectionStringSettings = ConfigurationManager.ConnectionStrings[ConnectionStringName];
            if (connectionStringSettings == null)
            {
                throw new ConfigurationErrorsException("The connection string " + ConnectionStringName + " does not exist.");
            }
            if (string.IsNullOrWhiteSpace(connectionStringSettings.ConnectionString))
            {
                throw new ConfigurationErrorsException("The connection string " + ConnectionStringName + " is not valid.");
            }
        }

        public static void ValidateConfig()
        {
            ValidateConfigValueIsNotNullOrEmpty(nameof(ListenUrl), ListenUrl);
            if (SendMailOnAuditFailure)
            {
                ValidateConfigValueIsNotNullOrEmpty(nameof(MailgunApiKey), MailgunApiKey);
                ValidateConfigValueIsNotNullOrEmpty(nameof(MailgunUrl), MailgunUrl);
                ValidateConfigValueIsNotNullOrEmpty(nameof(AuditEmailToAddress), AuditEmailToAddress);
                ValidateConfigValueIsNotNullOrEmpty(nameof(AuditEmailFromAddress), AuditEmailFromAddress);
            }
        }

        private static void ValidateConfigValueIsNotNullOrEmpty(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ConfigurationErrorsException("The config item " + name + " is missing or empty. This config is require to run the service.");
            }
        }
    }
}
