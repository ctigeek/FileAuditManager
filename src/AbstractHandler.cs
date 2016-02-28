using System;
using System.Text;
using System.Threading.Tasks;
using log4net;
using Microsoft.Owin;

namespace FileAuditManager
{
    public abstract class AbstractHandler
    {
        protected async Task WriteBadrequestResponse(string message, IOwinResponse response)
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            response.StatusCode = 400;
            response.ContentType = "plain/text";
            response.ContentLength = bytes.Length;
            await response.WriteAsync(bytes);
        }

        protected void WriteServerError(string message, IOwinResponse response, ILog log)
        {
            try
            {
                var bytes = Encoding.UTF8.GetBytes(message);
                response.StatusCode = 500;
                response.ContentType = "plain/text";
                response.ContentLength = bytes.Length;
                response.Write(bytes);
            }
            catch (Exception ex)
            {
                log.Error("Error while generating ISE:", ex);
            }
        }
    }
}
