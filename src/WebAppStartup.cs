using System;
using System.Diagnostics;
using System.Net;
using FileAuditManager.Controllers.Registration;
using FileAuditManager.Controllers.Validation;
using log4net;
using Owin;
using FileAuditManager.Logging;

namespace FileAuditManager
{
    public class WebAppStartup
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
            appBuilder.UseRequestValidator();
        }

        private void RunWebApiConfiguration(IAppBuilder appBuilder)
        {
            appBuilder.UseWebApi(new ApiHttpConfiguration());
            Log.Debug("Registered WebApi route configuration.");
        }
    }
}
