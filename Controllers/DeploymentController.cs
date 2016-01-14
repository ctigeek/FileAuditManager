using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using FileAuditManager.Data;
using FileAuditManager.Data.Models;
using log4net;

namespace FileAuditManager.Controllers
{
    public class DeploymentController : ApiController
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ApplicationController));
        private readonly IDeploymentRepository deploymentRepository;
        private readonly IApplicationRepository applicationRepository;

        public DeploymentController(IApplicationRepository applicationRepository, IDeploymentRepository deploymentRepository)
        {
            this.applicationRepository = applicationRepository;
            this.deploymentRepository = deploymentRepository;
        }

        public async Task<IHttpActionResult> Get(string name, [FromUri] bool includeInactive = false)
        {
            try
            {
                var appTask = applicationRepository.GetApplicationAsync(name);
                var depTask = includeInactive
                    ? deploymentRepository.GetAllDeploymentsAsync(name)
                    : deploymentRepository.GetActiveDeploymentsAsync(name);
                await Task.WhenAll(appTask, depTask);
                
                if (appTask.Result == null)
                {
                    return BadRequest("Application " + name + " was not found.");
                }
                if (depTask.Result == null)
                {
                    return NotFound();
                }
                return Ok(BuildDeploymentResponse(depTask.Result, name));
            }
            catch (Exception ex)
            {
                Log.Error("Error in deployment controller:", ex);
                return InternalServerError();
            }
        }

        public async Task<IHttpActionResult> Get(string name, Guid deploymentId)
        {
            try
            {
                var appTask = applicationRepository.GetApplicationAsync(name);
                var depTask = deploymentRepository.GetDeploymentAsync(deploymentId);
                await Task.WhenAll(appTask, depTask);

                if (appTask.Result == null)
                {
                    return BadRequest("Application " + name + " was not found.");
                }
                if (depTask.Result == null)
                {
                    return NotFound();
                }
                return Ok(depTask.Result);
            }
            catch (Exception ex)
            {
                Log.Error("Error in deployment controller:", ex);
                return InternalServerError();
            }
        }

        public async Task<IHttpActionResult> Post(string name, [FromBody] Deployment deployment)
        {
            try
            {
                if (deployment.DeploymentId != null ||
                    !string.IsNullOrWhiteSpace(deployment.ApplicationName) ||
                    deployment.StartDateTime != null ||
                    deployment.EndDateTime != null)
                {
                    return BadRequest("The only fields you can include for a new deployment is `ServerName`, `NetworkPath`, and `Hash`.");
                }
                if (string.IsNullOrWhiteSpace(deployment.ServerName) || 
                    string.IsNullOrWhiteSpace(deployment.NetworkPath) || 
                    string.IsNullOrWhiteSpace(deployment.Hash))
                {
                    return BadRequest("You must include the fields `ServerName`, `NetworkPath`, and `Hash`.");
                }
                var application = await applicationRepository.GetApplicationAsync(name);

                if (application == null)
                {
                    return BadRequest("Application " + name + " was not found.");
                }

                deployment.ApplicationName = name;
                deployment.DeploymentId = Guid.NewGuid();
                deployment.StartDateTime = DateTime.UtcNow;
                deployment.EndDateTime = DateTime.MaxValue;
                await deploymentRepository.InsertDeploymentAsync(deployment);
                return Ok();
            }
            catch (Exception ex)
            {
                Log.Error("Error in deployment controller:", ex);
                return InternalServerError();
            }
        }

        public async Task<IHttpActionResult> Delete(string name, Guid? deploymentId, [FromUri] bool all = false)
        {
            try
            {
                if (deploymentId.HasValue)
                {
                    await deploymentRepository.DeleteDeploymentAsync(deploymentId.Value);
                }
                if (all)
                {
                    var deployments = await deploymentRepository.GetAllDeploymentsAsync(name);
                    foreach (var deployment in deployments)
                    {
                        await deploymentRepository.DeleteDeploymentAsync(deployment.DeploymentId.Value);
                    }
                }
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
                DeploymentCount = deployments.Count,
                Deployments = deployments.OrderByDescending(d=>d.StartDateTime)
            };
        }
    }
}
