using System.Collections.Generic;
using System.Threading.Tasks;
using FileAuditManager.Data.Models;

namespace FileAuditManager.Mail
{
    internal interface IMailService
    {
        Task SendAuditEmail(string applicationName, Dictionary<Deployment, DeploymentAudit> failedAudits);
    }
}