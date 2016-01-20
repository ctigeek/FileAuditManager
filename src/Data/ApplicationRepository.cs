﻿using System;
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

        public ApplicationRepository(MongoUrl mongoUrl, IMongoClient mongoClient)
            : base(mongoUrl, mongoClient)
        {
            collection = MongoDatabase.GetCollection<Application>(ApplicationCollection);
        }

        public async Task<IList<Application>> GetAllApplicationsAsync(bool activeOnly = true)
        {
            if (activeOnly)
            {
                var list = await collection.AsQueryable().Where(a=> a.Enabled).ToListAsync();
                return list;
            }
            else
            {
                var list = await collection.AsQueryable().ToListAsync();
                return list;
            }
        } 

        public async Task<Application> GetApplicationAsync(string name)
        {
            var list = await collection.AsQueryable().Where(a=>a.Name == name).ToListAsync();
            return list.FirstOrDefault();
        }

        public async Task InsertApplicationAsync(Application application)
        {
            var existingRecord = await GetApplicationAsync(application.Name);
            if (existingRecord == null)
            {
                await collection.InsertOneAsync(application);
            }
            else
            {
                throw new ArgumentException("The application named `" + application.Name + "` already exists.");
            }
        }

        public async Task UpdateApplication(Application application)
        {
            await collection.ReplaceOneAsync(a => a.Name == application.Name, application);
        }
    }
}
