using System.Net.Http;

namespace Doppler.Sap.Job.Service.DopplerCurrencyService
{
    public abstract class ExternalService
    {
        protected readonly HttpClient HttpClient;

        public ExternalService()
        {

        }
    }
}
