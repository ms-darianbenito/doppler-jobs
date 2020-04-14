using System.Collections.Generic;
using System.Threading.Tasks;
using CrossCutting.DopplerSapService.Entities;

namespace Doppler.Billing.Job.Database
{
    public interface IDopplerRepository
    {
        public Task<IList<UserBilling>> GetUserBillingInformation();
    }
}
