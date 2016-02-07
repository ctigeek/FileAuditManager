using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using FileAuditManager.Data;
using FileAuditManager.Data.Models;
using FileAuditManager.Hashing;
using log4net;

namespace FileAuditManager.Controllers
{
    public class AuditController : ApiController
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ApplicationController));
        private readonly IDeploymentRepository deploymentRepository;
        private readonly IApplicationRepository applicationRepository;
        private readonly IAuditRepository auditRepository;
        private readonly IApplicationHashingService applicationHashingService;

        public AuditController(IApplicationRepository applicationRepository, 
                                IDeploymentRepository deploymentRepository,
                                IAuditRepository auditRepository,
                                IApplicationHashingService applicationHashingService)
        {
            this.applicationRepository = applicationRepository;
            this.deploymentRepository = deploymentRepository;
            this.auditRepository = auditRepository;
            this.applicationHashingService = applicationHashingService;
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

                var audits = await auditRepository.GetAuditsAsync(activeDeployments.Select(d => d.MostRecentAudit).ToList());
                var response = BuildAuditResponseObject(name, activeDeployments, audits);
                return Ok(response);
            }
            catch (Exception ex)
            {
                Log.Error("Error in audit controller:", ex);
                return InternalServerError();
            }
        }

        public async Task<IHttpActionResult> Get(string name, string serverName)
        {
            try
            {
                var deployment = (await deploymentRepository.GetActiveDeploymentsAsync(name, serverName)).FirstOrDefault();
                if (deployment == null)
                {
                    return BadRequest("No active deployment for application `" + name + "` on server `" + serverName + "`.");
                }
                if (deployment.MostRecentAudit == Guid.Empty)
                {
                    return NotFound();
                }
                var audits = (await auditRepository.GetAllAuditsAsync(new[] {deployment.DeploymentId})).ToList();
                if (audits.Count == 0)
                {
                    return NotFound();
                }
                var response = BuildAuditResponseObject(name, new[] {deployment}, audits.OrderByDescending(a=>a.AuditDateTime));
                return Ok(response);
            }
            catch (Exception ex)
            {
                Log.Error("Error in audit controller:", ex);
                return InternalServerError();
            }
        }

        public async Task<IHttpActionResult> Post(string name, string serverName)
        {
            try
            {
                var getAppTask = applicationRepository.GetApplicationAsync(name);
                var getDeploymentTask = deploymentRepository.GetActiveDeploymentsAsync(name);
                await Task.WhenAll(getAppTask, getDeploymentTask);
                var application = getAppTask.Result;
                var activeDeployments = getDeploymentTask.Result;

                if (application == null)
                {
                    return BadRequest("Unknown application name: `" + name + "`.");
                }
                if (activeDeployments == null || activeDeployments.Count == 0 || activeDeployments.All(d => d.ServerName != serverName))
                {
                    return BadRequest("No deployment found for application `" + name + "` with server `" + serverName + "`.");
                }

                var activeDeployment = activeDeployments.FirstOrDefault(d => d.ServerName == serverName);
                var deploymentAudit = await applicationHashingService.HashDeployment(activeDeployment, application.GetRegularExpressions(), application.HashHiddenFiles);
                await auditRepository.CreateAuditAsync(deploymentAudit);
                return Ok();
            }
            catch (Exception ex)
            {
                Log.Error("Error in audit controller:", ex);
                return InternalServerError();
            }
        }

        private object BuildAuditResponseObject(string applicationName, IList<Deployment> deployments, IEnumerable<DeploymentAudit> deploymentAudits)
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
