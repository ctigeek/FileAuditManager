using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FileAuditManager.Data.Models;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace FileAuditManager.Data
{
    class DeploymentAuditRepository : MongoRepositoryBase
    {
        private const string DeploymentCollection = "DeploymentAudit";
        private readonly IMongoCollection<DeploymentAudit> collection;
        public DeploymentAuditRepository(string connectionString) : this(connectionString, null)
        {
        }

        public DeploymentAuditRepository(string connectionString, IMongoDatabase database) : base(connectionString, database)
        {
            collection = MongoDatabase.GetCollection<DeploymentAudit>(DeploymentCollection);
        }

        public async Task<IList<DeploymentAudit>> GetDeploymentAudits(Guid deploymentId)
        {
            var list = await collection.AsQueryable()
                .Where(d => d.DeploymentId == deploymentId)
                .ToListAsync();
            return list;
        }

        public async Task InsertNewDeploymentAuditAsync(DeploymentAudit deploymentAudit)
        {
            //TODO: validate it is not a duplicate.
            await collection.InsertOneAsync(deploymentAudit);
        } 
    }
}