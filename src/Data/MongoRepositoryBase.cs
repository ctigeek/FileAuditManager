using MongoDB.Driver;

namespace FileAuditManager.Data
{
    abstract class MongoRepositoryBase
    {
        private readonly IMongoClient MongoClient;
        protected readonly IMongoDatabase MongoDatabase;

        protected MongoRepositoryBase(string connectionString, IMongoDatabase database)
        {
            if (database != null)
            {
                this.MongoDatabase = database;
            }
            else
            {
                var mongoUri = new MongoUrl(connectionString);
                MongoClient = new MongoClient(mongoUri);
                MongoDatabase = MongoClient.GetDatabase(mongoUri.DatabaseName);
            }
        }
    }
}
