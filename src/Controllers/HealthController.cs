using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;

namespace FileAuditManager.Controllers
{
    public class HealthController : ApiController
    {
        public static bool IsHealthy { get; set; } = false;

        public HttpResponseMessage Get()
        {
            var content = new StringContent(IsHealthy ? "Healthy" : "OOR", Encoding.UTF8);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = content
            };
        }

        public IHttpActionResult Put([FromUri] bool healthy)
        {
            try
            {
                IsHealthy = healthy;
                return Ok();
            }
            catch (Exception)
            {
                return BadRequest("bummer");
            }
        }
    }
}
