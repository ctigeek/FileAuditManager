using System.Net;
using FileAuditManager.Controllers.Registration;
using FileAuditManager.Controllers.Validation;
using log4net;
using Owin;
using FileAuditManager.Logging;

namespace FileAuditManager
{
    public class WebAppStartup
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(WebAppStartup));
        public void Configuration(IAppBuilder appBuilder)
        {
            AddWindowsAuth(appBuilder);
            appBuilder.UseRequestLogging();
            AddConsoleLogging(appBuilder);
            appBuilder.UseRequestValidator();
            appBuilder.UseWebApi(new ApiHttpConfiguration());
        }

        private void AddWindowsAuth(IAppBuilder appBuilder)
        {
            if (FileAuditManager.Configuration.UseWindowsAuth)
            {
                var listener = (HttpListener) appBuilder.Properties[typeof (HttpListener).FullName];
                listener.AuthenticationSchemes = AuthenticationSchemes.IntegratedWindowsAuthentication;
            }
        }

        private void AddConsoleLogging(IAppBuilder appBuilder)
        {
            if (Log.IsDebugEnabled)
            {
                appBuilder.UseConsoleLogging();
                Log.Debug("Http request/response debug output enabled.");
            }
        }
    }
}
