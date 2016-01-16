using System;
using System.Configuration;
using System.Threading;
using log4net;
using Microsoft.Owin.Hosting;
using FileAuditManager.Controllers;

namespace FileAuditManager
{
    internal class WebAppManager
    {
        private static ILog Log = LogManager.GetLogger(typeof(WebAppManager));
        public bool Running { get; private set; }
        private bool IsPaused { get; set; } = false;
        private IDisposable webApp;
        private readonly string uri;

        public WebAppManager(string uri = null)
        {
            this.uri = uri ?? ConfigurationManager.AppSettings["uri"];
            Running = false;
        }

        public void Start()
        {
            try
            {
                if (Running) throw new InvalidOperationException("WebAppManager already running.");
                PreRunValidationTest();
                webApp = WebApp.Start<WebAppStartup>(uri);
                Running = true;
                Log.Debug("Listening on " + uri);
                PostRunValidationTest();
                ThreadPool.QueueUserWorkItem(PutServiceIntoRotationAfterTenSeconds);
            }
            catch (Exception ex)
            {
                Log.Error("Could not start WebAppManager.", ex);
                Running = false;
                throw;
            }
        }
        public void Stop()
        {
            try
            {
                if (webApp != null)
                {
                    HealthController.IsHealthy = false;
                    //sleep so that health monitor will see that service is shutting down?
                    //Thread.Sleep(7000);
                    webApp.Dispose();
                    webApp = null;
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error while stopping WebAppManager.", ex);
            }
            Running = false;
        }
        public void Pause()
        {
            HealthController.IsHealthy = false;
            IsPaused = true;
        }
        public void Continue()
        {
            HealthController.IsHealthy = true;
            IsPaused = false;
        }
        private void PutServiceIntoRotationAfterTenSeconds(object o)
        {
            Thread.Sleep(10000);
            if (!IsPaused)
            {
                HealthController.IsHealthy = true;
            }
        }

        private void PreRunValidationTest()
        {
            ValidateConnectionString(ConnectionStringPolicy.ConnectionStringName);
            ValidateApplicationSetting("uri");
            ValidateApplicationSetting("UseWindowsAuth");
        }

        private void ValidateConnectionString(string connectionStringName)
        {
            var connectionStringSettings = ConfigurationManager.ConnectionStrings[connectionStringName];
            if (connectionStringSettings == null)
            {
                throw new ConfigurationErrorsException("The connection string " + connectionStringName + " does not exist.");
            }
            if (string.IsNullOrWhiteSpace(connectionStringSettings.ConnectionString))
            {
                throw new ConfigurationErrorsException("The connection string " + connectionStringName + " is not valid.");
            }
        }

        private void ValidateApplicationSetting(string name)
        {
            var value = ConfigurationManager.AppSettings[name];
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ConfigurationErrorsException("The appSettings for " + name + " is invalid.");
            }
        }

        private void PostRunValidationTest()
        {
            try
            {
                // test connection string and any other external resources here....
            }
            catch (Exception ex)
            {
                Log.Error("Validation test error:", ex);
                throw;
            }
        }
    }
}
