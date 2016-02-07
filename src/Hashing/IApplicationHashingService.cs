using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FileAuditManager.Data.Models;

namespace FileAuditManager.Hashing
{
    public interface IApplicationHashingService
    {
        Task<DeploymentAudit> HashDeployment(Deployment deployment, IList<Regex> fileExclusionExpressions, bool hashHiddenFiles);
    }
}