using System;
using System.Configuration;
using MongoDB.Driver;

namespace FileAuditManager.Data
{
    abstract class MongoRepositoryBase
    {
        private static string mongoServerName;
        private static string mongoDatabaseName;

        protected readonly IMongoClient MongoClient;
        protected readonly IMongoDatabase MongoDatabase;

        protected MongoRepositoryBase(string connectionString, IMongoDatabase database)
        {
            if (database != null)
            {
                this.MongoDatabase = database;
            }
            else
            {
                ParseConnectionString(connectionString);
                MongoClient = new MongoClient(mongoServerName);
                MongoDatabase = MongoClient.GetDatabase(mongoDatabaseName);
            }
        }

        private static void ParseConnectionString(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(mongoServerName) || string.IsNullOrWhiteSpace(mongoDatabaseName))
            {
                var parts = connectionString.Split(';');
                foreach (var part in parts)
                {
                    var halves = part.Split('=');
                    if (halves.Length != 2) throw new ConfigurationErrorsException("Invalid mongodb connection string.");
                    if (halves[0].Equals("server", StringComparison.InvariantCultureIgnoreCase))
                    {
                        mongoServerName = halves[1];
                    }
                    else if (halves[0].Equals("database", StringComparison.InvariantCultureIgnoreCase))
                    {
                        mongoDatabaseName = halves[1];
                    }
                }

                if (string.IsNullOrWhiteSpace(mongoServerName) || string.IsNullOrWhiteSpace(mongoDatabaseName))
                {
                    throw new ConfigurationErrorsException("Invalid mongodb connection string. You must specify a server and a database.");
                }
            }
        }
    }
}
