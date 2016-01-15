using FileAuditManager.Data.Models;

namespace FileAuditManager.Hashing
{
    internal interface IApplicationHashingManager
    {
        void HashAllActiveApplications();
        void HashDeployment(Deployment deployment);
    }
}