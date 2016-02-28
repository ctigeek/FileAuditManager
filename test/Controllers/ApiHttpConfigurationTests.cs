using System;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Dispatcher;
using System.Web.Http.Hosting;
using System.Web.Http.Routing;
using FileAuditManager.Controllers;
using FileAuditManager.Controllers.Registration;
using NUnit.Framework;

namespace test.Controllers
{
    [TestFixture]
    public class ApiHttpConfigurationTests
    {
        private ApiHttpConfiguration configuration;

        [SetUp]
        public void Setup()
        {
            configuration = new ApiHttpConfiguration();
        }

        //-------------------------- Application

        [TestCase("http://localhost/application")]
        [TestCase("http://localhost/application?activeOnly=true")]
        [TestCase("http://localhost/application/name")]
        public void GET_ApplicationUrlCallsApplicationGet(string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            var routeTester = new RouteTester(configuration, request);

            Assert.AreEqual(routeTester.GetControllerType(), typeof(ApplicationController));
            Assert.AreEqual(routeTester.GetActionName(), nameof(ApplicationController.Get));
        }

        [Test]
        public void POST_ApplicationUrlCallsApplicationPost()
        {
            var url = "http://localhost/application/name";
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            var routeTester = new RouteTester(configuration, request);

            Assert.AreEqual(routeTester.GetControllerType(), typeof(ApplicationController));
            Assert.AreEqual(routeTester.GetActionName(), nameof(ApplicationController.Post));
        }

        [Test]
        public void PUT_ApplicationUrlCallsApplicationPost()
        {
            var url = "http://localhost/application/name";
            var request = new HttpRequestMessage(HttpMethod.Put, url);
            var routeTester = new RouteTester(configuration, request);

            Assert.AreEqual(routeTester.GetControllerType(), typeof(ApplicationController));
            Assert.AreEqual(routeTester.GetActionName(), nameof(ApplicationController.Put));
        }

        // --------------------------------- Deployment
        [TestCase("http://localhost/application/name/deployment")]
        [TestCase("http://localhost/application/name/deployment/name")]
        [TestCase("http://localhost/application/name/deployment?includeInactive=true&includeFiles=true")]
        [TestCase("http://localhost/application/name/deployment/name?includeInactive=true&includeFiles=true")]
        public void GET_DeploymentCallDeploymentGet(string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            var routeTester = new RouteTester(configuration, request);

            Assert.AreEqual(routeTester.GetControllerType(), typeof(DeploymentController));
            Assert.AreEqual(routeTester.GetActionName(), nameof(DeploymentController.Get));
        }

        [Test]
        public void POST_DeploymentCallDeploymentPost()
        {
            var url = "http://localhost/application/name/deployment/name";
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            var routeTester = new RouteTester(configuration, request);

            Assert.AreEqual(routeTester.GetControllerType(), typeof(DeploymentController));
            Assert.AreEqual(routeTester.GetActionName(), nameof(DeploymentController.Post));
        }
        [Test]
        public void DELETE_DeploymentCallDeploymentPost()
        {
            var url = "http://localhost/application/name/deployment/name";
            var request = new HttpRequestMessage(HttpMethod.Delete, url);
            var routeTester = new RouteTester(configuration, request);

            Assert.AreEqual(routeTester.GetControllerType(), typeof(DeploymentController));
            Assert.AreEqual(routeTester.GetActionName(), nameof(DeploymentController.Delete));
        }

        [TestCase("http://localhost/application/name/audit")]
        [TestCase("http://localhost/application/name/audit/name")]
        public void GET_AuditCallAuditGet(string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            var routeTester = new RouteTester(configuration, request);

            Assert.AreEqual(routeTester.GetControllerType(), typeof(AuditController));
            Assert.AreEqual(routeTester.GetActionName(), nameof(AuditController.Get));
        }
        [Test]
        public void POST_AuditCallPost()
        {
            var url = "http://localhost/application/name/audit/name";
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            var routeTester = new RouteTester(configuration, request);

            Assert.AreEqual(routeTester.GetControllerType(), typeof(AuditController));
            Assert.AreEqual(routeTester.GetActionName(), nameof(AuditController.Post));
        }

        [Test]
        public void POST_AuditCallAddComment()
        {
            var url = "http://localhost/application/name/audit/id/comments";
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            var routeTester = new RouteTester(configuration, request);

            Assert.AreEqual(routeTester.GetControllerType(), typeof(AuditController));
            Assert.AreEqual(routeTester.GetActionName(), nameof(AuditController.AddComment));
        }

        [Test]
        public void GET_HealthCallsGet()
        {
            var url = "http://localhost/health";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            var routeTester = new RouteTester(configuration, request);

            Assert.AreEqual(routeTester.GetControllerType(), typeof(HealthController));
            Assert.AreEqual(routeTester.GetActionName(), nameof(HealthController.Get));
        }

        [Test]
        public void PUT_HealthCallsPut()
        {
            var url = "http://localhost/health?healthy=true";
            var request = new HttpRequestMessage(HttpMethod.Put, url);
            var routeTester = new RouteTester(configuration, request);

            Assert.AreEqual(routeTester.GetControllerType(), typeof(HealthController));
            Assert.AreEqual(routeTester.GetActionName(), nameof(HealthController.Put));
        }
    }

    public class RouteTester
    {
        //http://www.strathweb.com/2012/08/testing-routes-in-asp-net-web-api/
        HttpConfiguration config;
        HttpRequestMessage request;
        IHttpRouteData routeData;
        IHttpControllerSelector controllerSelector;
        HttpControllerContext controllerContext;

        public RouteTester(HttpConfiguration conf, HttpRequestMessage req)
        {
            config = conf;
            request = req;
            routeData = config.Routes.GetRouteData(request);
            request.Properties[HttpPropertyKeys.HttpRouteDataKey] = routeData;
            controllerSelector = new DefaultHttpControllerSelector(config);
            controllerContext = new HttpControllerContext(config, routeData, request);
        }
        public Type GetControllerType()
        {
            var descriptor = controllerSelector.SelectController(request);
            controllerContext.ControllerDescriptor = descriptor;
            return descriptor.ControllerType;
        }
        public string GetActionName()
        {
            if (controllerContext.ControllerDescriptor == null)
                GetControllerType();

            var actionSelector = new ApiControllerActionSelector();
            var descriptor = actionSelector.SelectAction(controllerContext);

            return descriptor.ActionName;
        }
    }
}
