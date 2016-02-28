using System;
using System.Text;
using System.Threading.Tasks;
using log4net;
using Microsoft.Owin;
using Owin;

namespace FileAuditManager.Controllers.Validation
{
    public static class RequestValidationHandler
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof (RequestValidationHandler));

        public static IAppBuilder UseRequestValidator(this IAppBuilder appBuilder)
        {
            appBuilder.Use(async (env, next) =>
            {
                await ValidateUrl(env, next);
            });
            return appBuilder;
        }

        public static async Task ValidateUrl(IOwinContext env, Func<Task> next)
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
            }
        }

        private static async Task WriteBadrequestResponse(string message, IOwinResponse response)
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            response.StatusCode = 400;
            response.ContentType = "plain/text";
            response.ContentLength = bytes.Length;
            await response.WriteAsync(bytes);
        }
    }
}
