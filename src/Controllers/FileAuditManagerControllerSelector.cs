using System;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Dispatcher;

namespace FileAuditManager.Controllers
{
    public class FileAuditManagerControllerSelector : DefaultHttpControllerSelector
    {
        public FileAuditManagerControllerSelector(HttpConfiguration configuration) : base(configuration)
        {
        }

        public override HttpControllerDescriptor SelectController(HttpRequestMessage request)
        {
            request.RequestUri = new Uri(request.RequestUri.ToString().ToLower());
            return base.SelectController(request);
        }
    }
}
