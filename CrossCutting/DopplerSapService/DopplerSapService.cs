using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using CrossCutting.DopplerSapService.Entities;
using CrossCutting.DopplerSapService.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Rest.Serialization;
using Newtonsoft.Json;

namespace CrossCutting.DopplerSapService
{
    public class DopplerSapService : IDopplerSapService
    {
        private readonly DopplerSapServiceSettings _dopplerSapServiceSettings;
        private readonly JsonSerializerSettings _serializationSettings;
        private readonly HttpClient _httpClient;
        private readonly ILogger<DopplerSapService> _logger;

        public DopplerSapService(
            IHttpClientFactory httpClientFactory,
            HttpClientPoliciesSettings httpClientPoliciesSettings,
            IOptionsMonitor<DopplerSapServiceSettings> dopplerSapServiceSettings,
            ILogger<DopplerSapService> logger)
        {
            _dopplerSapServiceSettings = dopplerSapServiceSettings.CurrentValue;
            _httpClient = httpClientFactory.CreateClient(httpClientPoliciesSettings.ClientName);
            _logger = logger;
            _serializationSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                ContractResolver = new ReadOnlyJsonContractResolver(),
                Converters = new List<JsonConverter>
                {
                    new Iso8601TimeSpanConverter()
                }
            };
        }

        public async Task<HttpResponseMessage> SendCurrency(IList<CurrencyResponse> currencyList)
        {
            var uri = _dopplerSapServiceSettings.Url;
            _logger.LogInformation($"Building http request with url {uri}");

            var httpRequest = new HttpRequestMessage
            {
                RequestUri = new Uri(uri),
                Method = new HttpMethod("POST")
            };
            var requestContent = SafeJsonConvert.SerializeObject(currencyList, _serializationSettings);
            httpRequest.Content = new StringContent(requestContent, Encoding.UTF8);
            httpRequest.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json; charset=utf-8");

            var httpResponse = new HttpResponseMessage();
            try
            {
                _logger.LogInformation("Sending request to Doppler SAP Api.");
                httpResponse = await _httpClient.SendAsync(httpRequest).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                    $"Error occurred trying to send information to Doppler SAP return http code {httpResponse.StatusCode}.");
                throw;
            }

            return httpResponse;
        }

        public async Task<HttpResponseMessage> SendUserBillings(IList<UserBilling> userBillingList)
        {
            var uri = _dopplerSapServiceSettings.Url;
            _logger.LogInformation($"Building http request with url {uri}");

            var httpRequest = new HttpRequestMessage
            {
                RequestUri = new Uri(uri),
                Method = new HttpMethod("POST")
            };
            var requestContent = SafeJsonConvert.SerializeObject(userBillingList, _serializationSettings);
            httpRequest.Content = new StringContent(requestContent, Encoding.UTF8);
            httpRequest.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json; charset=utf-8");

            var httpResponse = new HttpResponseMessage();
            try
            {
                _logger.LogInformation("Sending request to Doppler SAP Api.");
                httpResponse = await _httpClient.SendAsync(httpRequest).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                    $"Error occurred trying to send information to Doppler SAP return http code {httpResponse.StatusCode}.");
                throw;
            }

            return httpResponse;
        }
    }
}
