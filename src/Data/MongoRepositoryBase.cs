using MongoDB.Driver;

namespace FileAuditManager.Data
{
    abstract class MongoRepositoryBase
    {
        private readonly IMongoClient MongoClient;
        protected readonly IMongoDatabase MongoDatabase;

        protected MongoRepositoryBase(MongoUrl mongoUrl, IMongoClient mongoClient)
        {
            MongoClient = mongoClient;
            MongoDatabase = MongoClient.GetDatabase(mongoUrl.DatabaseName);
        }
    }
}
