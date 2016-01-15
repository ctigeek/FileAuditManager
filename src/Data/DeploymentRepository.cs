using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FileAuditManager.Data.Models;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace FileAuditManager.Data
{
    class DeploymentRepository : MongoRepositoryBase, IDeploymentRepository
    {
        private const string DeploymentCollection = "Deployment2"; //TODO: change this back
        private readonly IMongoCollection<Deployment> collection;
        public DeploymentRepository(string connectionString) : this(connectionString, null)
        {
        }

        public DeploymentRepository(string connectionString, IMongoDatabase database) 
            : base(connectionString, database)
        {
            collection = MongoDatabase.GetCollection<Deployment>(DeploymentCollection);
        }

        public async Task<IList<Deployment>> GetActiveDeploymentsAsync(string applicationName)
        {
            var list = await collection.AsQueryable()
                .Where(d => d.ApplicationName == applicationName && d.EndDateTime == DateTime.MaxValue)
                .ToListAsync();
            return list;
        }

        public async Task<IList<Deployment>> GetAllDeploymentsAsync(string applicationName)
        {
            var list = await collection.AsQueryable()
                .Where(d => d.ApplicationName == applicationName)
                .ToListAsync();
            return list;
        }

        public async Task<Deployment> GetDeploymentAsync(Guid deploymentId)
        {
            var list = await collection.AsQueryable().Where(a => a.DeploymentId == deploymentId).ToListAsync();
            return list.FirstOrDefault();
        }

        public async Task InsertDeploymentAsync(Deployment deployment)
        {
            var taskList = new List<Task>();
            var existingActiveDeployments = await GetActiveDeploymentsAsync(deployment.ApplicationName);

            var matchingDeployment = existingActiveDeployments.FirstOrDefault(ead => ead.ServerName.Equals(deployment.ServerName, StringComparison.InvariantCultureIgnoreCase));
            if (matchingDeployment != null)
            {
                taskList.Add(DeleteDeploymentAsync(matchingDeployment.DeploymentId, deployment.StartDateTime));
            }
            taskList.Add(collection.InsertOneAsync(deployment));
            await Task.WhenAll(taskList);
        }

        public async Task<long> UpdateMostRecentAudit(Guid deploymentId, Guid mostRecentDeploymentAuditId)
        {
            var updateResult = await collection.UpdateOneAsync(d => d.DeploymentId == deploymentId, Builders<Deployment>.Update.Set(d => d.MostRecentAudit, mostRecentDeploymentAuditId));
            return updateResult.ModifiedCount;
        }

        public async Task<long> DeleteDeploymentAsync(Guid deploymentId, DateTime endDateTime)
        {
            var updateResult = await collection.UpdateOneAsync(d => d.DeploymentId == deploymentId, Builders<Deployment>.Update.Set(d => d.EndDateTime, endDateTime));
            return updateResult.ModifiedCount;
        }
    }
}