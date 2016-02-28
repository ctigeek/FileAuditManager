using FileAuditManager.Controllers;
using FileAuditManager.Data;
using FileAuditManager.Hashing;
using FileAuditManager.Mail;
using MongoDB.Driver;
using StructureMap;

namespace FileAuditManager
{
    public class StructureMapRegistry : Registry
    {
        public StructureMapRegistry()
        {
            this.ForSingletonOf<MongoUrl>().Use(Configuration.MongoUrl);
            this.ForSingletonOf<IMongoClient>().Use(() =>new MongoClient(Configuration.MongoUrl));
            this.For<IApplicationRepository>().Use<ApplicationRepository>();
            this.For<IDeploymentRepository>().Use<DeploymentRepository>();
            this.For<IAuditRepository>().Use<AuditRepository>();
            this.For<IApplicationHashingService>().Use<ApplicationHashingService>();
            this.For<IMailService>().Use<MailService>(() => new MailService(Configuration.SendMailOnAuditFailure,
                                                    Configuration.MailgunUrl, Configuration.MailgunApiKey, 
                                                    Configuration.AuditEmailToAddress, Configuration.AuditEmailFromAddress));
            this.ForConcreteType<ApplicationController>();
            this.ForConcreteType<DeploymentController>();
            this.ForConcreteType<AuditController>();
            HealthController.ContentType = Configuration.HealthResponseContentType;
            HealthController.ResponseFormat = Configuration.HealthResponseFormatString;
            HealthController.UpStatusString = Configuration.HealthResponseUpStatusString;
            HealthController.DownStatusString = Configuration.HealthResponseDownStatusString;
            this.ForConcreteType<HealthController>();
        }
    }
}
