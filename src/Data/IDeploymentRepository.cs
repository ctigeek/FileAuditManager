using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FileAuditManager.Data.Models;

namespace FileAuditManager.Data
{
    public interface IDeploymentRepository
    {
        Task<IList<Deployment>> GetActiveDeploymentsAsync(string applicationName);
        Task<IList<Deployment>> GetAllDeploymentsAsync(string applicationName);
        Task<Deployment> GetDeploymentAsync(Guid deploymentId);
        Task InsertDeploymentAsync(Deployment deployment);
        Task<long> UpdateMostRecentAudit(Guid deploymentId, Guid mostRecentDeploymentAuditId);
        Task<long> DeleteDeploymentAsync(Guid deploymentId, DateTime endDateTime);
    }
}