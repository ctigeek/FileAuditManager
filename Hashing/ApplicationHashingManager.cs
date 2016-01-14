using System;
using System.Threading;
using FileAuditManager.Data.Models;
using log4net;

namespace FileAuditManager.Hashing
{
    class ApplicationHashingManager : IApplicationHashingManager
    {
        private static ILog log = LogManager.GetLogger(typeof(ApplicationHashingManager));

        public void HashDeployment(Deployment deployment)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(HashFiles), deployment);
        }

        private void HashFiles(object deploymentObject)
        {
            var deployment = deploymentObject as Deployment;
            try
            {
                
                //TODO: hash stuff or send a message to a different process to hash stuff.....
            }
            catch (Exception ex)
            {
                log.ErrorFormat("Error while trying to hash files for application `{0}` on server `{1}`: {2}", deployment.ApplicationName, deployment.ServerName, ex);
            }
        }
    }
}
