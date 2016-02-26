using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FileAuditManager.Data.Models;

namespace FileAuditManager.Data
{
    public interface IDeploymentRepository
    {
        Task<IList<Deployment>> GetActiveDeploymentsAsync(string name, string serverName = null);
        Task<IList<Deployment>> GetAllDeploymentsAsync(string name, string serverName = null);
        Task<Deployment> GetDeploymentAsync(Guid deploymentId);
        Task InsertDeploymentAsync(Deployment deployment);
        Task<long> UpdateMostRecentAuditAsync(Guid deploymentId, Guid mostRecentDeploymentAuditId);
        Task<long> ReplaceDeployment(Deployment deployment);
        Task<long> DeleteDeploymentAsync(string name, string serverName, DateTime endDateTime);
    }
}