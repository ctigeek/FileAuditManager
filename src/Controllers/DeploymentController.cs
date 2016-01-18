using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
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
        private readonly IApplicationHashingManager applicationHashingManager;
        

        public DeploymentController(IApplicationRepository applicationRepository, IDeploymentRepository deploymentRepository, IApplicationHashingManager applicationHashingManager)
        {
            this.applicationRepository = applicationRepository;
            this.deploymentRepository = deploymentRepository;
            this.applicationHashingManager = applicationHashingManager;
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

                if (string.IsNullOrWhiteSpace(serverName) || 
                    string.IsNullOrWhiteSpace(networkPath))
                {
                    return BadRequest("You must include the fields `ServerName` and `NetworkPath` in the body.");
                }
                var application = await applicationRepository.GetApplicationAsync(name);
                if (application == null)
                {
                    await applicationRepository.InsertApplicationAsync(new Application {Name = name});
                }

                if (!Directory.Exists(networkPath))
                {
                    return BadRequest("The path `" + networkPath + "` is invalid or inaccessible.");
                }

                var deployment = new Deployment
                {
                    ApplicationName = name,
                    ServerName = serverName,
                    NetworkPath = networkPath
                };

                var auditHash = await applicationHashingManager.HashDeployment(deployment, application.GetRegularExpressions());
                deployment.Hash = auditHash.Hash;

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
