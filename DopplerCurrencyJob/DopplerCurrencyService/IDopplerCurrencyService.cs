using System.Collections.Generic;
using System.Threading.Tasks;
using CrossCutting.DopplerSapService.Entities;

namespace Doppler.Currency.Job.DopplerCurrencyService
{
    public interface IDopplerCurrencyService
    {
        public Task<IList<CurrencyResponse>> GetCurrencyByCode();
    }
}