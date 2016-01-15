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
                var applicationTask = applicationRepository.GetApplicationAsync(name);
                var activeDeploymentsTask = includeInactive
                    ? deploymentRepository.GetAllDeploymentsAsync(name)
                    : deploymentRepository.GetActiveDeploymentsAsync(name);
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

        public async Task<IHttpActionResult> Get(string name, Guid deploymentId)
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

        public async Task<IHttpActionResult> Post(string name, [FromBody] dynamic payload)
        {
            try
            {
                if (payload == null)
                {
                    return BadRequest("You must include the fields `ServerName`, `NetworkPath`, and `Hash` in the body.");
                }
                string serverName = payload.ServerName;
                string networkPath = payload.NetworkPath;
                string hash = payload.Hash;

                if (string.IsNullOrWhiteSpace(serverName) || 
                    string.IsNullOrWhiteSpace(networkPath) || 
                    string.IsNullOrWhiteSpace(hash))
                {
                    return BadRequest("You must include the fields `ServerName`, `NetworkPath`, and `Hash` in the body.");
                }
                var application = await applicationRepository.GetApplicationAsync(name);
                if (application == null)
                {
                    await applicationRepository.InsertApplicationAsync(new Application {Name = name});
                }
                var deployment = new Deployment
                {
                    ApplicationName = name,
                    ServerName = serverName,
                    NetworkPath = networkPath,
                    Hash = hash
                };
                await deploymentRepository.InsertDeploymentAsync(deployment);
                return Ok();
            }
            catch (Exception ex)
            {
                Log.Error("Error in deployment controller:", ex);
                return InternalServerError();
            }
        }

        public async Task<IHttpActionResult> Delete(string name, Guid deploymentId)
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
