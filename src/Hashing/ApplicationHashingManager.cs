using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
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
            var connectionString = ConfigurationManager.ConnectionStrings[FileAuditManagerHttpControllerActivator.ConnectionStringName].ConnectionString;
            this.applicationRepository = applicationRepository ?? new ApplicationRepository(connectionString);
            this.deploymentRepository = deploymentRepository ?? new DeploymentRepository(connectionString);
            this.auditRepository = auditRepository ?? new AuditRepository(connectionString, deploymentRepository);
        }

        public void HashAllActiveApplications()
        {
            var activeApplications = applicationRepository.GetAllApplicationsAsync().Result;
            foreach (var application in activeApplications)
            {
                var activeDeployments = deploymentRepository.GetActiveDeploymentsAsync(application.Name).Result;
                foreach (var deployment in activeDeployments)
                {
                    HashDeployment(deployment);
                    //pause for a few seconds so we aren't hammering every server simultanously.
                    Thread.Sleep(5000);
                }
            }
        }

        public void HashDeployment(Deployment deployment)
        {
            ThreadPool.QueueUserWorkItem(HashDeploymentWorker, deployment);
        }

        private void HashDeploymentWorker(object deploymentObject)
        {
            var deployment = deploymentObject as Deployment;
            try
            {
                var hashbytes = GetDirectoryHash(deployment.NetworkPath);
                var hash = BytesToString(hashbytes);
                SaveAudit(deployment, hash);
            }
            catch (Exception ex)
            {
                log.ErrorFormat("Error while trying to hash files for application `{0}` on server `{1}`: {2}", deployment.ApplicationName, deployment.ServerName, ex);
            }
        }

        private void SaveAudit(Deployment deployment, string hash)
        {
            var deploymentAudit = new DeploymentAudit
            {
                DeploymentId = deployment.DeploymentId,
                Hash = hash,
                ValidHash = deployment.Hash.Equals(hash, StringComparison.InvariantCultureIgnoreCase)
            };
            auditRepository.CreateAuditAsync(deploymentAudit).Wait();
        }

        private static byte[] GetDirectoryHash(string path)
        {
            var fileHasher = SHA1Managed.Create();
            var sumHasher = SHA1Managed.Create();
            sumHasher.Initialize();

            foreach (var file in Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories))
            {
                using (var str = File.OpenRead(file))
                {
                    var hash = fileHasher.ComputeHash(str);
                    sumHasher.TransformBlock(hash, 0, hash.Length, null, 0);
                    //hash the name and path of the file...
                    var filePath = Encoding.UTF8.GetBytes(file);
                    sumHasher.TransformBlock(filePath, 0, filePath.Length, null, 0);
                }
            }
            sumHasher.TransformFinalBlock(new byte[0], 0, 0);
            return sumHasher.Hash;
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
