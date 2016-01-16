using System;
using System.Configuration;
using System.Linq;
using FileAuditManager.Data;
using FileAuditManager.Hashing;
using StructureMap;
using StructureMap.Pipeline;

namespace FileAuditManager
{
    public class StructureMapRegistry : Registry
    {
        public StructureMapRegistry()
        {
            this.Policies.Add<ConnectionStringPolicy>();
            this.For<IApplicationRepository>().Use<ApplicationRepository>();
            this.For<IDeploymentRepository>().Use<DeploymentRepository>();
            this.For<IAuditRepository>().Use<AuditRepository>();
            this.For<IApplicationHashingManager>().Use<ApplicationHashingManager>();
        }
    }

    public class ConnectionStringPolicy : ConfiguredInstancePolicy
    {
        public const string ConnectionStringName = "fileaudit";
        private readonly string connectionString = ConfigurationManager.ConnectionStrings[ConnectionStringName].ConnectionString;

        protected override void apply(Type pluginType, IConfiguredInstance instance)
        {
            var parameter = instance.Constructor.GetParameters().FirstOrDefault(x => x.Name == "connectionString");
            if (parameter != null)
            {
                instance.Dependencies.AddForConstructorParameter(parameter, connectionString);
            }
        }
    }
}
