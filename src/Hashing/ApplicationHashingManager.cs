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
using log4net;

namespace FileAuditManager.Hashing
{
    class ApplicationHashingManager : IApplicationHashingManager
    {
        private static ILog log = LogManager.GetLogger(typeof(ApplicationHashingManager));
        private readonly IApplicationRepository applicationRepository;
        private readonly IDeploymentRepository deploymentRepository;
        private readonly IAuditRepository auditRepository;

        public ApplicationHashingManager(IApplicationRepository applicationRepository, IDeploymentRepository deploymentRepository, IAuditRepository auditRepository)
        {
            this.applicationRepository = applicationRepository;
            this.deploymentRepository = deploymentRepository;
            this.auditRepository = auditRepository;
        }

        public async Task AuditHashAllActiveApplications()
        {
            var activeApplications = applicationRepository.GetAllApplicationsAsync().Result;
            foreach (var application in activeApplications)
            {
                await AuditHashApplication(application);
            }
        }

        public async Task AuditHashApplication(Application application)
        {
            var activeDeployments = await deploymentRepository.GetActiveDeploymentsAsync(application.Name);
            foreach (var deployment in activeDeployments)
            {
                var audit = await HashDeployment(deployment, application.GetRegularExpressions());
                await SaveAuditAsync(audit);
            }
        }

        //public async Task AuditHashApplication(string name)
        //{
        //    var application = await applicationRepository.GetApplicationAsync(name);
        //    if (application == null)
        //    {
        //        throw new ArgumentException("Application `" + name + "` does not exist.");
        //    }
        //    AuditHashApplication(application);
        //}

        //public void AuditHashApplication(Application application)
        //{
        //    var activeDeployments = deploymentRepository.GetActiveDeploymentsAsync(application.Name).Result;
        //    var hashDeploymentTasks = GetTasksToHashDeployments(activeDeployments);
        //    hashDeploymentTasks.RunTasks<DeploymentAudit>(3, task =>
        //    {
        //        if (task.IsFaulted)
        //        {
        //            log.Error("Error hashing deployment (Application==`" + application.Name + "`:", task.Exception);
        //        }
        //        else
        //        {
        //            try
        //            {
        //                SaveAuditAsync(task.Result).Wait();
        //            }
        //            catch (Exception ex)
        //            {
        //                log.Error("Error saving audit: (Application==`" + application.Name + "`:", task.Exception);
        //            }
        //        }
        //    });
        //}

        //private IEnumerable<Task<DeploymentAudit>> GetTasksToHashDeployments(IList<Deployment> deployments)
        //{
        //    foreach (var deployment in deployments)
        //    {
        //        yield return HashDeployment(deployment);
        //    }
        //}

        public async Task<DeploymentAudit> HashDeployment(Deployment deployment, IList<Regex> fileExclusionExpressions)
        {
            var hash = await HashDirectory(deployment.NetworkPath, fileExclusionExpressions);
            return new DeploymentAudit
            {
                DeploymentId = deployment.DeploymentId,
                Hash = hash,
                ValidHash = deployment.Hash.Equals(hash, StringComparison.InvariantCultureIgnoreCase)
            };
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
