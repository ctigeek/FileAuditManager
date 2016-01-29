using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FileAuditManager.Data;
using FileAuditManager.Data.Models;
using FileAuditManager.Mail;
using log4net;

namespace FileAuditManager.Hashing
{
    internal class ApplicationHashingManager : IApplicationHashingManager
    {
        private static ILog log = LogManager.GetLogger(typeof(ApplicationHashingManager));
        private readonly IApplicationRepository applicationRepository;
        private readonly IDeploymentRepository deploymentRepository;
        private readonly IAuditRepository auditRepository;
        private readonly IMailService mailService;

        public ApplicationHashingManager(IApplicationRepository applicationRepository, IDeploymentRepository deploymentRepository, IAuditRepository auditRepository, IMailService mailService)
        {
            this.applicationRepository = applicationRepository;
            this.deploymentRepository = deploymentRepository;
            this.auditRepository = auditRepository;
            this.mailService = mailService;
        }

        public async Task AuditHashAllActiveApplications()
        {
            var activeApplications = applicationRepository.GetAllApplicationsAsync().Result;
            foreach (var application in activeApplications)
            {
                await AuditHashApplication(application);
            }
        }

        public async Task AuditHashApplication(Application application, bool sendAuditEmail = true)
        {
            var activeDeployments = await deploymentRepository.GetActiveDeploymentsAsync(application.Name);
            var failedAudits = new Dictionary<Deployment, DeploymentAudit>();
            foreach (var deployment in activeDeployments)
            {
                var audit = await HashDeployment(deployment, application.GetRegularExpressions());
                await SaveAuditAsync(audit);
                if (!audit.ValidHash) failedAudits.Add(deployment, audit);
            }
            if (failedAudits.Count > 0)
            {
                log.WarnFormat("Audits failed for the following application & servers:\r\n {0}", string.Join(",", failedAudits.Keys.Select(d => application.Name + " - " + d.ServerName)));
                await mailService.SendAuditEmail(application.Name, failedAudits);
            }
            else
            {
                log.InfoFormat("All audits passed for application {0}.", application.Name);
            }
        }

        public async Task<DeploymentAudit> HashDeployment(Deployment deployment, IList<Regex> fileExclusionExpressions)
        {
            var hash = await HashDirectory(deployment.NetworkPath, fileExclusionExpressions);
            var audit = new DeploymentAudit
            {
                DeploymentId = deployment.DeploymentId,
                Hash = hash,
                ValidHash = deployment.Hash.Equals(hash, StringComparison.InvariantCultureIgnoreCase)
            };
            log.Info($"Completed audit for application {deployment.ApplicationName} on server {deployment.ServerName}. Results: {audit.ValidHash}");
            return audit;
        }

        private async Task<string> HashDirectory(string path, IList<Regex> fileExclusionExpressions)
        {
            var hasher = SHA1Managed.Create();
            hasher.Initialize();
            foreach (var file in Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories))
            {
                if (!fileExclusionExpressions.Any(f => f.IsMatch(file)))
                {
                    await HashFile(hasher, file);
                }
            }
            hasher.TransformFinalBlock(new byte[0], 0, 0);
            var hashString = BytesToString(hasher.Hash);
            return hashString;
        }

        private async Task HashFile(SHA1 hasher, string path)
        {
            var buffer = new byte[1024]; //what is optimal here?
            using (var fileStream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                while (true)
                {
                    var bytesread = await fileStream.ReadAsync(buffer, 0, 1024);
                    if (bytesread == 0) break;
                    hasher.TransformBlock(buffer, 0, bytesread, null, 0);
                }
            }
            HashString(hasher, path);
        }

        private void HashString(SHA1 hasher, string hashThis)
        {
            var bytes = Encoding.UTF8.GetBytes(hashThis);
            hasher.TransformBlock(bytes, 0, bytes.Length, null, 0);
        }

        private async Task SaveAuditAsync(DeploymentAudit deploymentAudit)
        {
            await auditRepository.CreateAuditAsync(deploymentAudit);
        }

        private static string BytesToString(byte[] array)
        {
            var sb = new StringBuilder();
            foreach (byte t in array)
            {
                sb.AppendFormat("{0:X2}", t);
            }
            return sb.ToString();
        }
    }
}
