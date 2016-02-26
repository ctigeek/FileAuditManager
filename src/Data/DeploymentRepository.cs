using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FileAuditManager.Data.Models;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace FileAuditManager.Data
{
    internal class DeploymentRepository : MongoRepositoryBase, IDeploymentRepository
    {
        private const string DeploymentCollection = "Deployment";
        private readonly IMongoCollection<Deployment> collection;

        public DeploymentRepository(MongoUrl mongoUrl, IMongoClient mongoClient)
            : base(mongoUrl, mongoClient)
        {
            collection = MongoDatabase.GetCollection<Deployment>(DeploymentCollection);
        }

        public async Task<IList<Deployment>> GetActiveDeploymentsAsync(string name, string serverName = null)
        {
            if (string.IsNullOrWhiteSpace(serverName))
            {
                return await collection.AsQueryable()
                    .Where(d => d.ApplicationName == name && d.EndDateTime == DateTime.MaxValue)
                    .ToListAsync();
            }
            return await collection.AsQueryable()
                .Where(d => d.ApplicationName == name && d.ServerName == serverName  && d.EndDateTime == DateTime.MaxValue)
                .ToListAsync();
        }

        private async Task<Deployment> GetActiveDeployment(string name, string serverName)
        {
            return (await GetActiveDeploymentsAsync(name, serverName)).FirstOrDefault();
        }

        public async Task<IList<Deployment>> GetAllDeploymentsAsync(string name, string serverName = null)
        {
            if (string.IsNullOrWhiteSpace(serverName))
            {
                return await collection.AsQueryable()
                    .Where(d => d.ApplicationName == name)
                    .ToListAsync();
            }
            return await collection.AsQueryable()
                .Where(d => d.ApplicationName == name && d.ServerName == serverName)
                .ToListAsync();
        }

        public async Task<Deployment> GetDeploymentAsync(Guid deploymentId)
        {
            var list = await collection.AsQueryable().Where(a => a.DeploymentId == deploymentId).ToListAsync();
            return list.FirstOrDefault();
        }

        public async Task InsertDeploymentAsync(Deployment deployment)
        {
            var matchingDeployment = await GetActiveDeployment(deployment.ApplicationName, deployment.ServerName);
            if (matchingDeployment != null)
            {
                await ChangeDeploymentEndDateTime(matchingDeployment.DeploymentId, deployment.StartDateTime);
            }
            await collection.InsertOneAsync(deployment);
        }

        public async Task<long> UpdateMostRecentAuditAsync(Guid deploymentId, Guid mostRecentDeploymentAuditId)
        {
            var updateResult = await collection.UpdateOneAsync(d => d.DeploymentId == deploymentId, Builders<Deployment>.Update.Set(d => d.MostRecentAudit, mostRecentDeploymentAuditId));
            return updateResult.ModifiedCount;
        }

        public async Task<long> DeleteDeploymentAsync(string name, string serverName, DateTime endDateTime)
        {
            var deploymentToDelete = await GetActiveDeployment(name, serverName);
            if (deploymentToDelete == null) return 0;
            await ChangeDeploymentEndDateTime(deploymentToDelete.DeploymentId, endDateTime);
            return 1;
        }

        private async Task ChangeDeploymentEndDateTime(Guid deploymentId, DateTime endDateTime)
        {
            await collection.UpdateOneAsync(d => d.DeploymentId == deploymentId, Builders<Deployment>.Update.Set(d => d.EndDateTime, endDateTime));
        }
    }
}