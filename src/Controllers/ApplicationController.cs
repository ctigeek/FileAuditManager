using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http;
using FileAuditManager.Data;
using FileAuditManager.Data.Models;
using log4net;

namespace FileAuditManager.Controllers
{
    public class ApplicationController : ApiController
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ApplicationController));
        private readonly IApplicationRepository applicationRepository;

        public ApplicationController(IApplicationRepository applicationRepository)
        {
            this.applicationRepository = applicationRepository;
        }

        public async Task<IHttpActionResult> Get()
        {
            try
            {
                var applications = await applicationRepository.GetAllApplicationsAsync();
                if (applications.Count == 0)
                {
                    return NotFound();
                }
                return Ok(BuildApplicationResponse(applications));
            }
            catch (Exception ex)
            {
                Log.Error("Error in application controller:", ex);
                return InternalServerError();
            }
        }

        public async Task<IHttpActionResult> Get(string name)
        {
            try
            {
                var application = await applicationRepository.GetApplicationAsync(name);
                if (application == null)
                {
                    return NotFound();
                }
                return Ok(application);
            }
            catch (Exception ex)
            {
                Log.Error("Error in application controller:", ex);
                return InternalServerError();
            }
        }

        public async Task<IHttpActionResult> Post(string name)
        {
            try
            {
                var application = await applicationRepository.GetApplicationAsync(name);
                if (application != null)
                {
                    return BadRequest("The application already exists. Use PUT to modify the application.");
                }
                application = new Application
                {
                    Name = name,
                    Enabled = true
                };
                await applicationRepository.InsertApplicationAsync(application);
                return Ok();
            }
            catch (Exception ex)
            {
                Log.Error("Error in application controller:", ex);
                return InternalServerError();
            }
        }

        public async Task<IHttpActionResult> Put(string name, [FromBody] dynamic payload)
        {
            try
            {
                if (payload == null || payload)
                {
                    return BadRequest("You must include boolean variable `Enabled` in the body.");
                }
                bool? enabled = payload.Enabled;
                IList<string> fileExclusionExpressions = payload.FileExclusionExpressions;

                var existingApplication = await applicationRepository.GetApplicationAsync(name);
                if (existingApplication == null)
                {
                    return NotFound();
                }

                if (!enabled.HasValue && fileExclusionExpressions == null)
                {
                    return BadRequest("You must include either a boolean called `Enabled` or an array of string called `FileExclusionExpressions` in the body.");
                }

                if (enabled.HasValue)
                {
                    existingApplication.Enabled = enabled.Value;
                }
                if (fileExclusionExpressions != null)
                {
                    existingApplication.FileExclusionExpressions = fileExclusionExpressions;
                }

                await applicationRepository.UpdateApplication(existingApplication);
                return Ok();
            }
            catch (Exception ex)
            {
                Log.Error("Error in application controller:", ex);
                return InternalServerError();
            }
        }

        private object BuildApplicationResponse(IList<Application> applications)
        {
            return new
            {
                Count = applications.Count,
                Applications = applications
            };
        }
    }
}
