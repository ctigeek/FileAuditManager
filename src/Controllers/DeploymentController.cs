using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using FileAuditManager.Controllers.Models;
using FileAuditManager.Data;
using FileAuditManager.Data.Models;
using FileAuditManager.Hashing;
using log4net;

namespace FileAuditManager.Controllers
{
    public class DeploymentController : ApiController
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ApplicationController));
        private readonly IDeploymentRepository deploymentRepository;
        private readonly IApplicationRepository applicationRepository;
        private readonly IAuditRepository auditRepository;
        private readonly IApplicationHashingService applicationHashingService;
        

        public DeploymentController(IApplicationRepository applicationRepository, IDeploymentRepository deploymentRepository, IApplicationHashingService applicationHashingService, IAuditRepository auditRepository)
        {
            this.applicationRepository = applicationRepository;
            this.deploymentRepository = deploymentRepository;
            this.applicationHashingService = applicationHashingService;
            this.auditRepository = auditRepository;
        }

        public async Task<IHttpActionResult> Get(string name, string serverName = null, [FromUri] bool includeInactive = false)
        {
            try
            {
                var applicationTask = applicationRepository.GetApplicationAsync(name);
                var activeDeploymentsTask = includeInactive
                    ? deploymentRepository.GetAllDeploymentsAsync(name, serverName)
                    : deploymentRepository.GetActiveDeploymentsAsync(name, serverName);
                await Task.WhenAll(applicationTask, activeDeploymentsTask);
                
                if (applicationTask.Result == null)
                {
                    return BadRequest("Application " + name + " was not found.");
                }
                if (activeDeploymentsTask.Result == null)
                {
                    return NotFound();
                }
                return Ok(BuildDeploymentResponse(activeDeploymentsTask.Result, name));
            }
            catch (Exception ex)
            {
                Log.Error("Error in deployment controller:", ex);
                return InternalServerError();
            }
        }

        public async Task<IHttpActionResult> Get(string name, [FromUri] Guid deploymentId)
        {
            try
            {
                var applicationTask = applicationRepository.GetApplicationAsync(name);
                var activeDeploymentsTask = deploymentRepository.GetDeploymentAsync(deploymentId);
                await Task.WhenAll(applicationTask, activeDeploymentsTask);

                if (applicationTask.Result == null)
                {
                    return BadRequest("Application " + name + " was not found.");
                }
                if (activeDeploymentsTask.Result == null)
                {
                    return NotFound();
                }
                return Ok(activeDeploymentsTask.Result);
            }
            catch (Exception ex)
            {
                Log.Error("Error in deployment controller:", ex);
                return InternalServerError();
            }
        }

        public async Task<IHttpActionResult> Post(string name, string serverName, [FromBody] NewDeployment payload)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(payload?.NetworkPath))
                {
                    return BadRequest("You must include the field `NetworkPath` in the body.");
                }
                var application = await applicationRepository.GetApplicationAsync(name);
                if (application == null)
                {
                    return BadRequest("The application " + name + " does not exist.");
                }

                if (!Directory.Exists(payload.NetworkPath))
                {
                    return BadRequest("The path `" + payload.NetworkPath + "` is invalid or inaccessible.");
                }

                var deployment = new Deployment
                {
                    ApplicationName = name,
                    ServerName = serverName,
                    NetworkPath = payload.NetworkPath
                };

                var deploymentAudit = await applicationHashingService.HashDeployment(deployment, application.GetRegularExpressions(), application.HashHiddenFiles);

                await deploymentRepository.InsertDeploymentAsync(deployment);
                await auditRepository.CreateAuditAsync(deploymentAudit);

                return Ok();
            }
            catch (Exception ex)
            {
                Log.Error("Error in deployment controller:", ex);
                return InternalServerError();
            }
        }

        public async Task<IHttpActionResult> Delete(Guid deploymentId)
        {
            try
            {
                await deploymentRepository.DeleteDeploymentAsync(deploymentId, DateTime.UtcNow);
                return Ok();
            }
            catch (Exception ex)
            {
                Log.Error("Error in deployment controller:", ex);
                return InternalServerError();
            }
        }

        private object BuildDeploymentResponse(IList<Deployment> deployments, string applicationName)
        {
            return new
            {
                Application = applicationName,
                Count = deployments.Count,
                Deployments = deployments.OrderByDescending(d=>d.StartDateTime)
            };
        }
    }
}
