using System.ServiceProcess;

namespace FileAuditManager
{
    internal class ServiceManager : ServiceBase
    {
        private readonly WebAppManager webAppManager;
        private readonly AuditManager auditManager;

        public ServiceManager()
        {
            var serviceName = System.Reflection.Assembly.GetExecutingAssembly().FullName;
            this.ServiceName = serviceName;
            webAppManager = new WebAppManager();
            auditManager = new AuditManager(null);
        }

        protected override void OnStart(string[] args)
        {
            webAppManager.Start();
            auditManager.Start();
        }

        protected override void OnStop()
        {
            webAppManager.Stop();
            auditManager.Stop();
        }

        protected override void OnPause()
        {
            webAppManager.Pause();
            auditManager.Pause();
        }

        protected override void OnContinue()
        {
            webAppManager.Continue();
            auditManager.Continue();
        }
    }
}
