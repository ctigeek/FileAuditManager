﻿using System.Configuration;
using FileAuditManager.Controllers;
using FileAuditManager.Data;
using FileAuditManager.Hashing;
using MongoDB.Driver;
using StructureMap;

namespace FileAuditManager
{
    public class StructureMapRegistry : Registry
    {
        public const string ConnectionStringName = "fileaudit";
        public static readonly MongoUrl MongoUrl = new MongoUrl(ConfigurationManager.ConnectionStrings[ConnectionStringName].ConnectionString);

        public StructureMapRegistry()
        {
            this.ForSingletonOf<MongoUrl>().Use(MongoUrl);
            this.ForSingletonOf<IMongoClient>().Use(() =>new MongoClient(MongoUrl));
            this.For<IApplicationRepository>().Use<ApplicationRepository>();
            this.For<IDeploymentRepository>().Use<DeploymentRepository>();
            this.For<IAuditRepository>().Use<AuditRepository>();
            this.For<IApplicationHashingManager>().Use<ApplicationHashingManager>();
            this.ForConcreteType<ApplicationController>();
            this.ForConcreteType<DeploymentController>();
            this.ForConcreteType<AuditController>();
            this.ForConcreteType<HealthController>();
        }
    }
}
