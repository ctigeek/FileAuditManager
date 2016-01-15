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
    public class AuditController : ApiController
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ApplicationController));
        private readonly IDeploymentRepository deploymentRepository;
        private readonly IApplicationRepository applicationRepository;
        private readonly IAuditRepository auditRepository;

        public AuditController(IApplicationRepository applicationRepository, 
                                IDeploymentRepository deploymentRepository,
                                IAuditRepository auditRepository)
        {
            this.applicationRepository = applicationRepository;
            this.deploymentRepository = deploymentRepository;
            this.auditRepository = auditRepository;
        }

        public async Task<IHttpActionResult> Get(string name)
        {
            try
            {
                var applicationTask = applicationRepository.GetApplicationAsync(name);
                var activeDeploymentsTask = deploymentRepository.GetActiveDeploymentsAsync(name);
                await Task.WhenAll(applicationTask, activeDeploymentsTask);

                if (applicationTask.Result == null)
                {
                    return BadRequest("Application " + name + " was not found.");
                }
                var activeDeployments = activeDeploymentsTask.Result;
                if (activeDeployments == null || activeDeployments.Count == 0)
                {
                    return NotFound();
                }

                var audits = await auditRepository.GetAuditsAsync(activeDeployments.Select(d => d.MostRecentAudit.Value).ToList());
                var response = BuildAuditResponseObject(name, activeDeployments, audits);
                return Ok(response);
            }
            catch (Exception ex)
            {
                Log.Error("Error in audit controller:", ex);
                return InternalServerError();
            }
        }

        public async Task<IHttpActionResult> Get(string name, Guid deploymentId)
        {
            try
            {
                var deploymentTask = deploymentRepository.GetDeploymentAsync(deploymentId);
                var auditsTask = auditRepository.GetAllAuditsAsync(new[] { deploymentId });
                await Task.WhenAll(deploymentTask, auditsTask);

                if (deploymentTask.Result == null)
                {
                    return NotFound();
                }
                var audits = BuildAuditResponseObject(name, new[] {deploymentTask.Result}, auditsTask.Result);
                return Ok(audits);
            }
            catch (Exception ex)
            {
                Log.Error("Error in audit controller:", ex);
                return InternalServerError();
            }
        }

        public async Task<IHttpActionResult> Post(string name, [FromBody] dynamic requestBody)
        {
            try
            {
                if (requestBody == null)
                {
                    return BadRequest("You must include fields `ServerName` and `Hash` in the body. Any other fields will be ignored.");
                }
                string serverName = requestBody.ServerName;
                string hash = requestBody.Hash;
                if (string.IsNullOrWhiteSpace(serverName) || string.IsNullOrWhiteSpace(hash))
                {
                    return BadRequest("You must include fields `ServerName` and `Hash` in the body. Any other fields will be ignored.");
                }
                var activeDeployments = await deploymentRepository.GetActiveDeploymentsAsync(name);
                if (activeDeployments == null || activeDeployments.Count == 0 || activeDeployments.All(d => d.ServerName != serverName))
                {
                    return BadRequest("No deployment found for application `" + name + "` on server `" + serverName + "`.");
                }
                var activeDeployment = activeDeployments.FirstOrDefault(d => d.ServerName == serverName);
                var deploymentAudit = new DeploymentAudit
                {
                    DeploymentId = activeDeployment.DeploymentId,
                    Hash = hash,
                    ValidHash = hash.Equals(activeDeployment.Hash, StringComparison.InvariantCultureIgnoreCase)
                };
                await auditRepository.CreateAuditAsync(deploymentAudit);
                return Ok();
            }
            catch (Exception ex)
            {
                Log.Error("Error in audit controller:", ex);
                return InternalServerError();
            }
        }

        private object BuildAuditResponseObject(string applicationName, IList<Deployment> deployments, IList<DeploymentAudit> deploymentAudits)
        {
            var audits = new List<object>();
            foreach (var deploymentAudit in deploymentAudits.OrderByDescending(a=>a.AuditDateTime))
            {
                var deployment = deployments.FirstOrDefault(d => d.DeploymentId == deploymentAudit.DeploymentId);
                if (deployment != null)
                {
                    audits.Add(new
                    {
                        DeploymentId = deployment.DeploymentId,
                        ServerName = deployment.ServerName,
                        NetworkPath = deployment.NetworkPath,
                        DeploymentStartDateTime = deployment.StartDateTime,
                        DeploymentHash = deployment.Hash,
                        AuditDateTime = deploymentAudit.AuditDateTime,
                        AuditHash = deploymentAudit.Hash,
                        ValidHash = deploymentAudit.ValidHash
                    });
                }
            }
            return new
            {
                ApplicationName = applicationName,
                Count = audits.Count,
                Audits = audits
            };
        }
    }
}
