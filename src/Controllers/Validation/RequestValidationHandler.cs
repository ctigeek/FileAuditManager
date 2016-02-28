using System;
using System.Threading.Tasks;
using log4net;
using Microsoft.Owin;
using Owin;

namespace FileAuditManager.Controllers.Validation
{
    public class RequestValidationHandler : AbstractHandler
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof (RequestValidationHandler));

        public async Task ValidateUrl(IOwinContext env, Func<Task> next)
        {
            try
            {
                var path = env.Request.Uri.AbsolutePath;
                if (!path.Equals(path.ToLowerInvariant(), StringComparison.InvariantCulture))
                {
                    await WriteBadrequestResponse("Sorry, but the path in the url must be all lower-case. It's just easier this way. Easier for me anyway.", env.Response);
                }
                else if ((env.Request.Method == "PUT" || env.Request.Method == "POST") &&
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
                WriteServerError("Unknown error.", env.Response, Log);
            }
        }
    }

    public static class RequestValidationHandlerHelper
    {
        public static IAppBuilder UseRequestValidator(this IAppBuilder appBuilder)
        {
            var requestValidationHandler = new RequestValidationHandler();
            appBuilder.Use(async (env, next) =>
            {
                await requestValidationHandler.ValidateUrl(env, next);
            });
            return appBuilder;
        }
    }
}
