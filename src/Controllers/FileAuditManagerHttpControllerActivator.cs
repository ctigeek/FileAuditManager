using System;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Dispatcher;

namespace FileAuditManager.Controllers
{
    class FileAuditManagerHttpControllerActivator : IHttpControllerActivator
    {
        //http://blog.ploeh.dk/2012/09/28/DependencyInjectionandLifetimeManagementwithASP.NETWebAPI/
        public IHttpController Create(HttpRequestMessage request, HttpControllerDescriptor controllerDescriptor, Type controllerType)
        {
            if (controllerType == null)
            {
                return null;
            }
            return DIContainer.Container.GetInstance(controllerType) as IHttpController;
        }
    }
}