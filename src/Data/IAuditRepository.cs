using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FileAuditManager.Data.Models;

namespace FileAuditManager.Data
{
    public interface IAuditRepository
    {
        Task<IList<DeploymentAudit>> GetAuditsAsync(IList<Guid> deploymentAuditIds);
        Task<IList<DeploymentAudit>> GetAllAuditsAsync(IList<Guid> deploymentIds);
        Task CreateAuditAsync(DeploymentAudit deploymentAudit);
        Task UpdateCommentsAsync(Guid deploymentAuditId, IList<AuditComment> comments);
    }
}