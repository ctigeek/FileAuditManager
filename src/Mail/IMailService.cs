using System.Threading.Tasks;

namespace FileAuditManager.Mail
{
    internal interface IMailService
    {
        Task SendAuditEmail(string subject, string message);
    }
}