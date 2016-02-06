using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FileAuditManager.Data.Models;

namespace FileAuditManager.Hashing
{
    public interface IApplicationHashingService
    {
        Task AuditHashAllActiveApplications();
        Task AuditHashApplication(Application application, bool sendAuditEmail = true);
        Task<DeploymentAudit> HashDeployment(Deployment deployment, IList<Regex> fileExclusionExpressions);
    }
}