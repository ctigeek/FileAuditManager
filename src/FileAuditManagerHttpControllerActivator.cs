using System;
using System.Configuration;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Dispatcher;
using FileAuditManager.Controllers;
using FileAuditManager.Data;

namespace FileAuditManager
{
    class FileAuditManagerHttpControllerActivator : IHttpControllerActivator
    {
        //http://blog.ploeh.dk/2012/09/28/DependencyInjectionandLifetimeManagementwithASP.NETWebAPI/
        public IHttpController Create(HttpRequestMessage request, HttpControllerDescriptor controllerDescriptor, Type controllerType)
        {
            if (controllerType == typeof (ApplicationController))
            {
                var applicationRepository = CreateApplicationRepository();
                var applicationController = new ApplicationController(applicationRepository);
                return applicationController;
            }
            if (controllerType == typeof(DeploymentController))
            {
                var applicationRepository = CreateApplicationRepository();
                var deploymentRepository = CreateDeploymentRepository();
                var deploymentController = new DeploymentController(applicationRepository, deploymentRepository);
                return deploymentController;
            }
            if (controllerType == typeof (AuditController))
            {
                var applicationRepository = CreateApplicationRepository();
                var deploymentRepository = CreateDeploymentRepository();
                var auditRepository = CreateAuditRepository();
                var auditController = new AuditController(applicationRepository, deploymentRepository, auditRepository);
                return auditController;
            }
            if (controllerType == typeof (HealthController))
            {
                return new HealthController();
            }
            
            return null;
        }

        private IApplicationRepository CreateApplicationRepository()
        {
            return DIContainer.Container.GetInstance<IApplicationRepository>();
        }

        private IDeploymentRepository CreateDeploymentRepository()
        {
            return DIContainer.Container.GetInstance<IDeploymentRepository>();
        }

        private IAuditRepository CreateAuditRepository()
        {
            return DIContainer.Container.GetInstance<IAuditRepository>();
        }
    }
}