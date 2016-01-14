using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FileAuditManager.Data.Models;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace FileAuditManager.Data
{
    class ApplicationRepository : MongoRepositoryBase, IApplicationRepository
    {
        private const string ApplicationCollection = "Application";
        private readonly IMongoCollection<Application> collection;

        public ApplicationRepository(string connectionString) : this(connectionString, null)
        {
        }

        public ApplicationRepository(string connectionString, IMongoDatabase database) 
            : base(connectionString, database)
        {
            collection = MongoDatabase.GetCollection<Application>(ApplicationCollection);
        }

        public async Task<IList<Application>> GetAllApplicationsAsync()
        {
            var list = await collection.AsQueryable().ToListAsync();
            return list;
        } 

        public async Task<Application> GetApplicationAsync(string name)
        {
            var list = await collection.AsQueryable().Where(a=>a.Name == name).ToListAsync();
            return list.FirstOrDefault();
        }

        public async Task InsertOrUpdateApplicationAsync(Application application)
        {
            var existingRecord = await GetApplicationAsync(application.Name);
            if (existingRecord == null)
            {
                await collection.InsertOneAsync(application);
            }
            else
            {
                await collection.ReplaceOneAsync(a => a.Name == application.Name, application);
            }
        }
    }
}
