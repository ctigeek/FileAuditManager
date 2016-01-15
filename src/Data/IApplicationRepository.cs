using System.Collections.Generic;
using System.Threading.Tasks;
using FileAuditManager.Data.Models;

namespace FileAuditManager.Data
{
    public interface IApplicationRepository
    {
        Task<IList<Application>> GetAllApplicationsAsync();
        Task<Application> GetApplicationAsync(string name);
        Task InsertApplicationAsync(Application application);
        Task<long> EnableDisableApplication(string name, bool enabled);
    }
}