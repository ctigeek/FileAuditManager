using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using FileAuditManager.Data.Models;
using log4net;

namespace FileAuditManager.Hashing
{
    //This class is officially a mess... I've refactored it a few times.  I think making it a single-use class, and segregating out the check for existing audits will probably help a lot.
    //  (when most of the methods in your class could be made static without any impact, you probably need to redesign it.)
    internal class ApplicationHashingService : IApplicationHashingService
    {
        private static ILog log = LogManager.GetLogger(typeof(ApplicationHashingService));

        public async Task<DeploymentAudit> HashDeployment(Deployment deployment, IList<Regex> fileExclusionExpressions, bool hashHiddenFiles, bool overrideExistingAudit)
        {
            try
            {
                AddToHashesInProgress(deployment, overrideExistingAudit);
                var audit = await HashDeployment(deployment, fileExclusionExpressions, hashHiddenFiles);
                return audit;
            }
            finally
            {
                RemoveFromHashingInProgress(deployment);
            }
        }

        #region Lockables....
        private static readonly object LockObject = new object();
        private static readonly List<Deployment> HashesInProgress = new List<Deployment>();
        private static readonly Dictionary<Guid, bool> AbortHashing = new Dictionary<Guid, bool>();

        private static void AddToHashesInProgress(Deployment deployment, bool overrideExistingAudit)
        {
            lock (LockObject)
            {
                var existingAuditDeployment = GetExistingAuditDeployment(deployment);
                if (existingAuditDeployment != null)
                {
                    if (overrideExistingAudit)
                    {
                        AbortExistingAudit(existingAuditDeployment);
                    }
                    else
                    {
                        throw new InvalidOperationException("There is already an audit running for application " + deployment.ApplicationName + " and server " + deployment.ServerName + ".");
                    }
                }
                HashesInProgress.Add(deployment);
                AbortHashing.Add(deployment.DeploymentId, false);
            }
        }

        private static void RemoveFromHashingInProgress(Deployment deployment)
        {
            lock (LockObject)
            {
                HashesInProgress.Remove(deployment);
                AbortHashing.Remove(deployment.DeploymentId);
            }
        }

        private static void AbortHashIfNecessary(Deployment deployment)
        {
            lock (LockObject)
            {
                if (AbortHashing.ContainsKey(deployment.DeploymentId) && AbortHashing[deployment.DeploymentId])
                {
                    throw new ApplicationException("The audit running for application " + deployment.ApplicationName + " and server " + deployment.ServerName + " was aborted for a new audit request.");
                }
            }
        }

        private static Deployment GetExistingAuditDeployment (Deployment deployment)
        {
            lock (LockObject)
            {
                return HashesInProgress.FirstOrDefault(h => h.ApplicationName == deployment.ApplicationName && h.ServerName == deployment.ServerName);
            }
        }

        private static void AbortExistingAudit(Deployment existingAuditDeployment)
        {
            lock (LockObject)
            {
                if (AbortHashing.ContainsKey(existingAuditDeployment.DeploymentId)) AbortHashing[existingAuditDeployment.DeploymentId] = true;
            }
            bool abortedFlag = false;
            for (int i = 0; i < 50; i++)
            {
                Thread.Sleep(500);
                lock (LockObject)
                {
                    if (!AbortHashing.ContainsKey(existingAuditDeployment.DeploymentId))
                    {
                        abortedFlag = true;
                        break;
                    }
                }
            }
            if (!abortedFlag)
            {
                throw new InvalidOperationException("There is already an audit running for application " + existingAuditDeployment.ApplicationName + " and server " + existingAuditDeployment.ServerName + " and it could not be abandoned.");
            }
        }

        #endregion

        private IList<FileHashMismatch> DetermineHashDifferences(IList<FileHash> deploymentHashes, IList<FileHash> hashResults)
        {
            //This is all horribly inefficient... we are searching through the two lists MANY times, which is rediculous. I have an idea for an generic function that can do all of this more efficiently.
            var hashDifferences = new List<FileHashMismatch>();

            //1. find things in this audit that aren't in the deployment.
            foreach (var path in hashResults.Select(hr => hr.Path).Except(deploymentHashes.Select(dh => dh.Path), StringComparer.InvariantCultureIgnoreCase))
            {
                hashDifferences.Add(new FileHashMismatch
                {
                    AuditHash = hashResults.FirstOrDefault(hr => hr.Path.Equals(path, StringComparison.InvariantCultureIgnoreCase)),
                    OriginalHash = null
                });
            }

            //2. find things in the deployment that aren't in this audit.
            foreach (var path in deploymentHashes.Select(dh=>dh.Path).Except(hashResults.Select(hr=>hr.Path), StringComparer.InvariantCultureIgnoreCase))
            {
                hashDifferences.Add(new FileHashMismatch
                {
                    AuditHash = null,
                    OriginalHash = deploymentHashes.FirstOrDefault(dh=>dh.Path.Equals(path, StringComparison.InvariantCultureIgnoreCase))
                });
            }
            //3. find hashes that don't match.
            foreach (var fileHash in deploymentHashes.Where(dh => hashResults.Any(hr=> hr.Path == dh.Path && hr.Hash != dh.Hash)))
            {
                hashDifferences.Add(new FileHashMismatch
                {
                    AuditHash = hashResults.FirstOrDefault(hr => hr.Path.Equals(fileHash.Path, StringComparison.InvariantCultureIgnoreCase)),
                    OriginalHash = fileHash
                });
            }

            return hashDifferences;
        }

        private string HashTheHashResults(IList<FileHash> hashResults)
        {
            var hasher = SHA1Managed.Create();
            foreach (var result in hashResults.OrderBy(fh=>fh.Path))
            {
                var bytes = Encoding.UTF8.GetBytes(result.Hash);
                hasher.TransformBlock(bytes, 0, bytes.Length, null, 0);
            }
            hasher.TransformFinalBlock(new byte[0], 0, 0);
            var hashString = BytesToString(hasher.Hash);
            return hashString;
        }

        private async Task<DeploymentAudit> HashDeployment(Deployment deployment, IList<Regex> fileExclusionExpressions, bool hashHiddenFiles)
        {
            var hashResults = new List<FileHash>();
            IList<FileHashMismatch> hashDifferences = new List<FileHashMismatch>();
            var sw = Stopwatch.StartNew();
            await HashDirectory(deployment, fileExclusionExpressions, hashHiddenFiles, hashResults);
            var hash = HashTheHashResults(hashResults);

            if (deployment.Hash == Deployment.EmptyHash) deployment.Hash = hash;
            if (deployment.FileHashes == null || deployment.FileHashes.Count == 0) deployment.FileHashes = hashResults;
            if (hash != deployment.Hash)
            {
                hashDifferences = DetermineHashDifferences(deployment.FileHashes, hashResults);
            }
            sw.Stop();

            var audit = new DeploymentAudit
            {
                DeploymentId = deployment.DeploymentId,
                Hash = hash,
                ValidHash = deployment.Hash.Equals(hash, StringComparison.InvariantCultureIgnoreCase),
                FileHashMismatches = hashDifferences
            };
            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"Completed audit for application {deployment.ApplicationName} on server {deployment.ServerName} with hash {hash} in {sw.Elapsed.TotalSeconds} seconds. \r\n Results: {audit.ValidHash} \r\n List of files included in hash: \r\n {string.Join("\r\n", hashResults)}");
            }
            else
            {
                log.Info(
                    $"Completed audit for application {deployment.ApplicationName} on server {deployment.ServerName} with hash {hash} in {sw.Elapsed.TotalSeconds} seconds. \r\n Results: {audit.ValidHash}");
            }
            return audit;
        }

        private async Task HashDirectory(Deployment deployment, IList<Regex> fileExclusionExpressions, bool hashHiddenFiles, IList<FileHash> hashResults)
        {
            foreach (var file in Directory.GetFiles(deployment.NetworkPath, "*"))
            {
                if (!fileExclusionExpressions.Any(f => f.IsMatch(file)))
                {
                    await HashFile(deployment, file, hashHiddenFiles, hashResults);
                }
            }
            foreach (var directory in Directory.GetDirectories(deployment.NetworkPath).Where(d=>!d.EndsWith("RECYCLE.BIN") && !d.EndsWith("System Volume Information")))
            {
                await HashSubDirectoryRecursive(deployment, directory, fileExclusionExpressions, hashHiddenFiles, hashResults);
            }
        }

        private async Task HashSubDirectoryRecursive(Deployment deployment, string directory, IList<Regex> fileExclusionExpressions, bool hashHiddenFiles, IList<FileHash> hashResults)
        {
            var directoryInfo = new DirectoryInfo(directory);
            if ((directoryInfo.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden && !hashHiddenFiles)
            {
                return;
            }

            foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
            {
                if (!fileExclusionExpressions.Any(f => f.IsMatch(file)))
                {
                    await HashFile(deployment, file, hashHiddenFiles, hashResults);
                }
            }
        }

        private async Task HashFile(Deployment deployment, string path, bool hashHiddenFiles, IList<FileHash> hashResults )
        {
            AbortHashIfNecessary(deployment);
            var fileInfo = new FileInfo(path);
            var fileIsHidden = (fileInfo.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden;
            if (fileIsHidden && !hashHiddenFiles)
            {
                return;
            }

            var hasher = SHA1Managed.Create();
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
            HashDateTime(hasher, fileInfo.LastWriteTimeUtc);
            HashIsHidden(hasher, fileIsHidden);

            hasher.TransformFinalBlock(new byte[0], 0, 0);
            hashResults.Add(new FileHash {Path = path, IsHidden = fileIsHidden, LastWriteTime = fileInfo.LastWriteTime, Hash = BytesToString(hasher.Hash)});
        }

        private void HashIsHidden(SHA1 hasher, bool isHidden)
        {
            var bytes = new[] {isHidden ? (byte) 1 : (byte) 0};
            hasher.TransformBlock(bytes, 0, bytes.Length, null, 0);
        }

        private void HashDateTime(SHA1 hasher, DateTime dateTime)
        {
            var bytes = BitConverter.GetBytes(dateTime.ToBinary());
            hasher.TransformBlock(bytes, 0, bytes.Length, null, 0);
        }

        private void HashString(SHA1 hasher, string hashThis)
        {
            var bytes = Encoding.UTF8.GetBytes(hashThis);
            hasher.TransformBlock(bytes, 0, bytes.Length, null, 0);
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
