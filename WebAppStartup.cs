﻿using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Dispatcher;
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
            ILog requestLog = LogManager.GetLogger("RequestLog");
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
            var useWindowsAuth = (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["UseWindowsAuth"]) && ConfigurationManager.AppSettings["UseWindowsAuth"].Equals("true", StringComparison.InvariantCultureIgnoreCase));
            if (useWindowsAuth)
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

            httpConfiguration.Routes.MapHttpRoute(
                name: "Application",
                routeTemplate: "application/{name}",
                defaults: new
                {
                    name = RouteParameter.Optional,
                    controller = "Application"
                });

            httpConfiguration.Routes.MapHttpRoute(
                name: "Deployment",
                routeTemplate: "application/{name}/deployment/{deploymentId}",
                defaults: new
                {
                    deploymentId = RouteParameter.Optional,
                    controller = "Deployment"
                });

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
