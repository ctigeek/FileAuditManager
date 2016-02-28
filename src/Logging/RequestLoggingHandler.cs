using System;
using System.Diagnostics;
using System.Threading.Tasks;
using log4net;
using Microsoft.Owin;
using Owin;

namespace FileAuditManager.Logging
{
    public class RequestLoggingHandler: AbstractHandler
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(RequestLoggingHandler));
        private readonly ApiRequestLogger apiRequestLogger = new ApiRequestLogger();

        public async Task LogRequest(IOwinContext env, Func<Task> next)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                await next();
                stopwatch.Stop();
                apiRequestLogger.Log(env.Request, env.Response, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                Log.Error("Unknown error in OWIN pipeline.", ex);
                WriteServerError("Unknown error.", env.Response, Log);
            }
        }

        public async Task ConsoleLogger(IOwinContext env, Func<Task> next)
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
                WriteServerError("Unknown error.", env.Response, Log);
            }
        }
    }

    public static class RequestLoggingHandlerHelper
    {
        public static IAppBuilder UseRequestLogging(this IAppBuilder appBuilder)
        {
            var requestLoggingHandler = new RequestLoggingHandler();
            appBuilder.Use(async (env, next) =>
            {
                await requestLoggingHandler.LogRequest(env, next);
            });
            return appBuilder;
        }

        public static IAppBuilder UseConsoleLogging(this IAppBuilder appBuilder)
        {
            var requestLoggingHandler = new RequestLoggingHandler();
            appBuilder.Use(async (env, next) =>
            {
                await requestLoggingHandler.ConsoleLogger(env, next);
            });
            return appBuilder;
        }
    }
}
