using System.Collections.Generic;
using System.Threading.Tasks;
using FileAuditManager.Data.Models;

namespace FileAuditManager.Data
{
    public interface IApplicationRepository
    {
        Task<IList<Application>> GetAllApplicationsAsync(bool activeOnly = true);
        Task<Application> GetApplicationAsync(string name);
        Task InsertApplicationAsync(Application application);
        Task UpdateApplication(Application application);
    }
}