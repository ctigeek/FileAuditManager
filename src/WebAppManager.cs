using System;
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

        public WebAppManager()
        {
            Running = false;
        }

        public void Start()
        {
            try
            {
                if (Running) throw new InvalidOperationException("WebAppManager already running.");
                PreRunValidationTest();
                webApp = WebApp.Start<WebAppStartup>(Configuration.ListenUrl);
                Running = true;
                Log.Debug("Listening on " + Configuration.ListenUrl);
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
            Configuration.ValidateConnectionStrings();
            Configuration.ValidateConfig();
        }

        private void PostRunValidationTest()
        {
            // test connection string and any other external resources here....
        }
    }
}
