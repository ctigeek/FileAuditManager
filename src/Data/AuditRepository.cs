using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FileAuditManager.Data.Models;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace FileAuditManager.Data
{
    class AuditRepository : MongoRepositoryBase, IAuditRepository
    {
        private const string AuditCollection = "DeploymentAudit";
        private readonly IMongoCollection<DeploymentAudit> collection;
        private readonly IDeploymentRepository deploymentRepository;

        public AuditRepository(string connectionString, IDeploymentRepository deploymentRepository) 
            : this(connectionString, deploymentRepository, null)
        {
        }

        public AuditRepository(string connectionString, IDeploymentRepository deploymentRepository, IMongoDatabase database) 
            : base(connectionString, database)
        {
            this.deploymentRepository = deploymentRepository;
            collection = MongoDatabase.GetCollection<DeploymentAudit>(AuditCollection);
        }

        public async Task<IList<DeploymentAudit>> GetAuditsAsync(IList<Guid> deploymentAuditIds)
        {
            var list = await collection.AsQueryable()
                .Where(d => deploymentAuditIds.Contains(d.DeploymentAuditId))
                .ToListAsync();
            return list;
        }

        public async Task<IList<DeploymentAudit>> GetAllAuditsAsync(IList<Guid> deploymentIds)
        {
            var list = await collection.AsQueryable()
                .Where(d => deploymentIds.Contains(d.DeploymentId))
                .ToListAsync();
            return list;
        }

        public async Task CreateAuditAsync(DeploymentAudit deploymentAudit)
        {
            var updateDeploymentTask = deploymentRepository.UpdateMostRecentAudit(deploymentAudit.DeploymentId, deploymentAudit.DeploymentAuditId);
            var insertAuditTask = collection.InsertOneAsync(deploymentAudit);
            await Task.WhenAll(updateDeploymentTask, insertAuditTask);
        }
    }
}
