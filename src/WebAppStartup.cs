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
            AddRequestLogging(appBuilder);
            AddConsoleLogging(appBuilder);
            AddBaseValidation(appBuilder);
            RunWebApiConfiguration(appBuilder);
        }

        private void AddRequestLogging(IAppBuilder appBuilder)
        {
            appBuilder.UseRequestLogging();
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

        private void AddBaseValidation(IAppBuilder appBuilder)
        {
            appBuilder.UseRequestValidator();
        }

        private void RunWebApiConfiguration(IAppBuilder appBuilder)
        {
            appBuilder.UseWebApi(new ApiHttpConfiguration());
            Log.Debug("Registered WebApi route configuration.");
        }
    }
}
