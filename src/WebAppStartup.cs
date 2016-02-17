using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Dispatcher;
using System.Web.Http.Routing;
using FileAuditManager.Controllers;
using log4net;
using Owin;
using FileAuditManager.Logging;
using Microsoft.Owin;

namespace FileAuditManager
{
    class WebAppStartup
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(WebAppStartup));
        public void Configuration(IAppBuilder appBuilder)
        {
            AddWindowsAuth(appBuilder);
            AddRequestLogging(appBuilder);
            AddConsoleLogging(appBuilder);
            AddBaseValidation(appBuilder);
            RunWebApiConfiguration(appBuilder);
        }

        private void AddRequestLogging(IAppBuilder appBuilder)
        {
            ILog requestLog = LogManager.GetLogger(ApiRequestLogger.RequestLogName);
            if (requestLog.IsWarnEnabled)
            {
                appBuilder.Use(async (env, next) =>
                {
                    try
                    {
                        var stopwatch = Stopwatch.StartNew();
                        await next();
                        stopwatch.Stop();
                        ApiRequestLogger.Log(env.Request, env.Response, stopwatch.ElapsedMilliseconds);
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Exception while writing to request log.", ex);
                    }
                });
            }
        }

        private void AddWindowsAuth(IAppBuilder appBuilder)
        {
            if (FileAuditManager.Configuration.UseWindowsAuth)
            {
                var listener = (HttpListener) appBuilder.Properties[typeof (HttpListener).FullName];
                listener.AuthenticationSchemes = AuthenticationSchemes.IntegratedWindowsAuthentication;
            }
        }

        private void AddConsoleLogging(IAppBuilder appBuilder)
        {
            if (Log.IsDebugEnabled)
            {
                appBuilder.Use(async (env, next) =>
                {
                    try
                    {
                        Log.DebugFormat("Http method: {0}, path {1}", env.Request.Method, env.Request.Path);
                        await next();
                        Log.DebugFormat("Response code: {0}", env.Response.StatusCode);
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Unknown error in OWIN pipeline.", ex);
                    }
                });
                Log.Debug("Http request/response debug output enabled.");
            }
        }

        private void AddBaseValidation(IAppBuilder appBuilder)
        {
            appBuilder.Use(async (env, next) =>
            {
                try
                {
                    if ((env.Request.Method == "PUT" || env.Request.Method == "POST") &&
                        !env.Request.ContentType.Equals("application/json", StringComparison.InvariantCultureIgnoreCase))
                    {
                        await WriteBadrequestResponse("Content-Type for POST and PUT must be application/json.", env.Response);
                    }
                    else
                    {
                        await next();
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Unknown error in OWIN pipeline.", ex);
                }
            });
        }

        private async Task WriteBadrequestResponse(string message, IOwinResponse response) 
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            response.StatusCode = 400;
            response.ContentType = "plain/text";
            response.ContentLength = bytes.Length;
            await response.WriteAsync(bytes);
        }

        private void RunWebApiConfiguration(IAppBuilder appBuilder)
        {
            var httpConfiguration = new HttpConfiguration();
            httpConfiguration.Services.Replace(typeof(IHttpControllerActivator), new FileAuditManagerHttpControllerActivator());

            //------------------------------------------  Application
            httpConfiguration.Routes.MapHttpRoute(
                name: "Application",
                routeTemplate: "application/{name}",
                defaults: new
                {
                    name = RouteParameter.Optional,
                    controller = "Application"
                });
            //------------------------------------------  Deployment
            httpConfiguration.Routes.MapHttpRoute(
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
            httpConfiguration.Routes.MapHttpRoute(
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

            //-----------------------------------------Audit
            httpConfiguration.Routes.MapHttpRoute(
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
            httpConfiguration.Routes.MapHttpRoute(
                name: "Audit",
                routeTemplate: "application/{name}/audit/{serverName}",
                defaults: new
                {
                    serverName = RouteParameter.Optional,
                    controller = "Audit"
                });
            // --------------------------------------- Health
            httpConfiguration.Routes.MapHttpRoute(
                name: "Health",
                routeTemplate: "{controller}",
                defaults: new
                {
                    controller = "Health"
                });

            appBuilder.UseWebApi(httpConfiguration);
            Log.Debug("Registered WebApi route configuration.");
        }
    }
}
