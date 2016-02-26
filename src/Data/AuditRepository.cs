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

        public AuditRepository(MongoUrl mongoUrl, IMongoClient mongoClient, IDeploymentRepository deploymentRepository)
            : base(mongoUrl, mongoClient)
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
            var updateDeploymentTask = deploymentRepository.UpdateMostRecentAuditAsync(deploymentAudit.DeploymentId, deploymentAudit.DeploymentAuditId);
            var insertAuditTask = collection.InsertOneAsync(deploymentAudit);
            await Task.WhenAll(updateDeploymentTask, insertAuditTask);
        }

        public async Task UpdateCommentsAsync(Guid deploymentAuditId, IList<AuditComment> comments)
        {
            await collection.UpdateOneAsync(d=>d.DeploymentAuditId == deploymentAuditId,
                            Builders<DeploymentAudit>.Update.Set(d => d.Comments, comments));
        }
    }
}
