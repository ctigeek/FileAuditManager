using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private readonly IList<string> listOfFilesHashed = new List<string>();

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
            var sw = Stopwatch.StartNew();
            var hash = await HashDirectory(deployment.NetworkPath, fileExclusionExpressions);
            sw.Stop();
            var audit = new DeploymentAudit
            {
                DeploymentId = deployment.DeploymentId,
                Hash = hash,
                ValidHash = deployment.Hash.Equals(hash, StringComparison.InvariantCultureIgnoreCase)
            };
            if (log.IsDebugEnabled)
            {
                log.Debug($"Completed audit for application {deployment.ApplicationName} on server {deployment.ServerName} with hash {hash} in {sw.Elapsed.TotalSeconds} seconds. \r\n Results: {audit.ValidHash} \r\n List of files included in hash: \r\n {string.Join("\r\n", listOfFilesHashed)}");
            }
            else
            {
                log.Info($"Completed audit for application {deployment.ApplicationName} on server {deployment.ServerName} with hash {hash} in {sw.Elapsed.TotalSeconds} seconds. \r\n Results: {audit.ValidHash}");
            }
            return audit;
        }

        private async Task<string> HashDirectory(string path, IList<Regex> fileExclusionExpressions)
        {
            var hasher = SHA1Managed.Create();
            hasher.Initialize();
            
            foreach (var file in Directory.GetFiles(path, "*"))
            {
                if (!fileExclusionExpressions.Any(f => f.IsMatch(file)))
                {
                    await HashFile(hasher, file);
                }
            }
            foreach (var directory in Directory.GetDirectories(path).Where(d=>!d.EndsWith("RECYCLE.BIN") && !d.EndsWith("System Volume Information")))
            {
                await HashSubDirectoryRecursive(hasher, directory, fileExclusionExpressions);
            }

            hasher.TransformFinalBlock(new byte[0], 0, 0);
            var hashString = BytesToString(hasher.Hash);
            return hashString;
        }

        private async Task HashSubDirectoryRecursive(SHA1 hasher, string directory, IList<Regex> fileExclusionExpressions )
        {
            foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
            {
                if (!fileExclusionExpressions.Any(f => f.IsMatch(file)))
                {
                    await HashFile(hasher, file);
                }
            }
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
            if (log.IsDebugEnabled)
            {
                listOfFilesHashed.Add(path);
            }
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
