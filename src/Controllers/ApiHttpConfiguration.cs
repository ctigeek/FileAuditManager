using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Dispatcher;
using System.Web.Http.Routing;

namespace FileAuditManager.Controllers
{
    public class ApiHttpConfiguration : HttpConfiguration
    {
        public ApiHttpConfiguration()
        {
            var lowercaseRouteConstraint = new LowercaseRouteConstraint();
            Services.Replace(typeof(IHttpControllerActivator), new FileAuditManagerHttpControllerActivator());

            ConfigureApplicationController();
            ConfigureDeploymentController();
            ConfigureAuditController();
            ConfigureHealthController();

            foreach (var route in Routes)
            {
                route.Constraints.Add("url", lowercaseRouteConstraint);
            }
        }

        private void ConfigureApplicationController()
        {
            Routes.MapHttpRoute(
                name: "Application",
                routeTemplate: "application/{name}",
                defaults: new
                {
                    name = RouteParameter.Optional,
                    controller = "Application"
                });
        }

        private void ConfigureDeploymentController()
        {
            Routes.MapHttpRoute(
                name: "DeploymentCreateDelete",
                routeTemplate: "application/{name}/deployment/{serverName}",
                defaults: new
                {
                    controller = "Deployment"
                },
                constraints: new
                {
                    httpMethod = new HttpMethodConstraint(HttpMethod.Post, HttpMethod.Delete)
                });
            Routes.MapHttpRoute(
                name: "DeploymentGet",
                routeTemplate: "application/{name}/deployment/{serverName}",
                defaults: new
                {
                    serverName = RouteParameter.Optional,
                    controller = "Deployment"
                },
                constraints: new
                {
                    httpMethod = new HttpMethodConstraint(HttpMethod.Get)
                });
        }

        private void ConfigureAuditController()
        {
            Routes.MapHttpRoute(
                name: "AuditPost",
                routeTemplate: "application/{name}/audit/{serverName}",
                defaults: new
                {
                    controller = "Audit"
                },
                constraints: new
                {
                    httpMethod = new HttpMethodConstraint(HttpMethod.Post)
                });
            Routes.MapHttpRoute(
                name: "AddComment",
                routeTemplate: "application/{name}/audit/{deploymentAuditId}/comments",
                defaults: new
                {
                    controller = "Audit",
                    action = "AddComment"
                },
                constraints: new
                {
                    httpMethod = new HttpMethodConstraint(HttpMethod.Post)
                });
            Routes.MapHttpRoute(
                name: "Audit",
                routeTemplate: "application/{name}/audit/{serverName}",
                defaults: new
                {
                    serverName = RouteParameter.Optional,
                    controller = "Audit"
                });
        }

        private void ConfigureHealthController()
        {
            Routes.MapHttpRoute(
                name: "Health",
                routeTemplate: "{controller}",
                defaults: new
                {
                    controller = "Health"
                });
        }
    }
}
