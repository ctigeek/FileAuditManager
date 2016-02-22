using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using FileAuditManager.Data;
using FileAuditManager.Data.Models;
using FileAuditManager.Hashing;
using FileAuditManager.Mail;
using log4net;
using Timer = System.Timers.Timer;

namespace FileAuditManager
{
    class AuditManager
    {
        private static ILog Log = LogManager.GetLogger(typeof(AuditManager));
        private static readonly object LockObject = new object();
        private readonly Timer timer;
        private readonly IApplicationHashingService hashingService;
        private readonly IAuditRepository auditRepository;
        private readonly IDeploymentRepository deploymentRepository;
        private readonly IApplicationRepository applicationRepository;
        private readonly IMailService mailService;

        public AuditManager(IApplicationHashingService hashingService, IAuditRepository auditRepository,
            IDeploymentRepository deploymentRepository, IApplicationRepository applicationRepository, IMailService mailService)
        {
            this.hashingService = hashingService ?? DIContainer.Container.GetInstance<IApplicationHashingService>();
            this.auditRepository = auditRepository ?? DIContainer.Container.GetInstance<IAuditRepository>();
            this.deploymentRepository = deploymentRepository ?? DIContainer.Container.GetInstance<IDeploymentRepository>();
            this.applicationRepository = applicationRepository ?? DIContainer.Container.GetInstance<IApplicationRepository>();
            this.mailService = mailService ?? DIContainer.Container.GetInstance<IMailService>();

            var milliseconds = Configuration.AuditTimerInMilliseconds;
            if (milliseconds > 59999)
            {
                timer = new Timer(milliseconds);
                timer.Elapsed += Timer_Elapsed;
            }
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (Monitor.TryEnter(LockObject))
            {
                try
                {
                    AuditHashAllActiveApplications();
                }
                catch (Exception ex)
                {
                    Log.Error("Error in AuditManager timer handler:", ex);
                }
                Monitor.Exit(LockObject);
            }
        }

        private void AuditHashAllActiveApplications()
        {
            var activeApplications = applicationRepository.GetAllApplicationsAsync().Result;
            foreach (var application in activeApplications)
            {
                AuditHashApplication(application).Wait();
            }
        }

        private async Task AuditHashApplication(Application application, bool sendAuditEmail = true)
        {
            var activeDeployments = await deploymentRepository.GetActiveDeploymentsAsync(application.Name);
            var failedAudits = new Dictionary<Deployment, DeploymentAudit>();
            foreach (var deployment in activeDeployments)
            {
                var audit = await hashingService.HashDeployment(deployment, application.GetRegularExpressions(), application.HashHiddenFiles, false);
                await auditRepository.CreateAuditAsync(audit);
                if (!audit.ValidHash) failedAudits.Add(deployment, audit);
            }
            if (failedAudits.Count > 0)
            {
                var sb = new StringBuilder();
                sb.AppendFormat("The application {0} has a failed audit on the following servers: \r\n", application.Name);
                foreach (var failedAudit in failedAudits)
                {
                    sb.AppendFormat("Audit UTC:{0},  Server {1}:\r\n", failedAudit.Value.AuditDateTime.ToLongTimeString(), failedAudit.Key.ServerName);
                    foreach (var fileHashMismatch in failedAudit.Value.FileHashMismatches)
                    {
                        sb.AppendFormat("Deployment Hash:{0}, Audit Hash:{1} \r\n", fileHashMismatch.OriginalHash, fileHashMismatch.AuditHash);
                    }
                }

                Log.Error(sb.ToString());
                await mailService.SendAuditEmail("Audit failure for " + application.Name, sb.ToString());
            }
            else
            {
                Log.InfoFormat("All audits passed for application {0}.", application.Name);
            }
        }

        public void Start()
        {
            timer?.Start();
        }

        public void Stop()
        {
            timer?.Stop();
        }

        public void Pause()
        {
            timer?.Stop();
        }

        public void Continue()
        {
            timer?.Start();
        }
    }
}
