using FileAuditManager.Data.Models;

namespace FileAuditManager.Hashing
{
    internal interface IApplicationHashingManager
    {
        void HashDeployment(Deployment deployment);
    }
}