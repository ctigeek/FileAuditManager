using System.Configuration;
using System.ServiceProcess;

namespace FileAuditManager
{
    internal class ServiceManager : ServiceBase
    {
        private readonly WebAppManager webAppManager;
        public ServiceManager()
        {
            var serviceName = ConfigurationManager.AppSettings["ApplicationName"];
            if (string.IsNullOrEmpty(serviceName)) throw new ConfigurationErrorsException("You must define an app setting called ApplicationName.");
            this.ServiceName = serviceName;
            webAppManager = new WebAppManager();
        }

        protected override void OnStart(string[] args)
        {
            webAppManager.Start();
        }

        protected override void OnStop()
        {
            webAppManager.Stop();
        }

        protected override void OnPause()
        {
            webAppManager.Pause();
        }

        protected override void OnContinue()
        {
            webAppManager.Continue();
        }
    }
}
