using System.ServiceProcess;

namespace FileAuditManager
{
    internal class ServiceManager : ServiceBase
    {
        private readonly WebAppManager webAppManager;
        public ServiceManager()
        {
            var serviceName = System.Reflection.Assembly.GetExecutingAssembly().FullName;
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
