using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using CrossCutting;
using CrossCutting.DopplerSapService.Entities;
using Dapper;
using Doppler.Currency.Job.Enums;
using Doppler.Currency.Job.Settings;
using Doppler.Database;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Doppler.Currency.Job.DopplerCurrencyService
{
    public class DopplerCurrencyService : IDopplerCurrencyService
    {
        private readonly DopplerCurrencyServiceSettings _dopplerCurrencySettings;
        private readonly TimeZoneJobConfigurations _jobConfig;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<DopplerCurrencyService> _logger;
        private readonly HttpClientPoliciesSettings _httpClientPoliciesSettings;
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public DopplerCurrencyService(
            IHttpClientFactory httpClientFactory,
            HttpClientPoliciesSettings httpClientPoliciesSettings,
            IOptionsMonitor<DopplerCurrencyServiceSettings> dopplerCurrencySettings,
            ILogger<DopplerCurrencyService> logger,
            TimeZoneJobConfigurations jobConfig,
            IDbConnectionFactory dbConnectionFactory)
        {
            _dopplerCurrencySettings = dopplerCurrencySettings.CurrentValue;
            _jobConfig = jobConfig;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _httpClientPoliciesSettings = httpClientPoliciesSettings;
            _dbConnectionFactory = dbConnectionFactory;
        }

        public async Task<IList<CurrencyResponse>> GetCurrencyByCode()
        {
            var cstZone = TimeZoneInfo.FindSystemTimeZoneById(_jobConfig.TimeZoneJobs);
            var cstTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, cstZone);
            
            var returnList = new List<CurrencyResponse>();
            
            foreach (var currencyCode in _dopplerCurrencySettings.CurrencyCodeList)
            {
                try { 
                    var uri = new Uri(_dopplerCurrencySettings.Url + $"{currencyCode}/{cstTime.Year}-{cstTime.Month}-{cstTime.Day}");
                    
                    _logger.LogInformation("Building http request with url {uri}", uri); 
                    var httpRequest = new HttpRequestMessage 
                    {
                        RequestUri = uri, 
                        Method = new HttpMethod("GET")

                    };
                    
                    _logger.LogInformation("Sending request to Doppler Currency Api.");
                    var client = _httpClientFactory.CreateClient(_httpClientPoliciesSettings.ClientName);
                    var httpResponse = await client.SendAsync(httpRequest).ConfigureAwait(false);

                    if (!httpResponse.IsSuccessStatusCode)
                        continue;

                    var json = await httpResponse.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<CurrencyResponse>(json);

                    returnList.Add(result);
                }
                catch (Exception e)
                {
                    _logger.LogError(e,"Error GetCurrency for {currencyCode}.", currencyCode);
                    throw;
                }
            }

            return returnList;
        }

        public async Task InsertCurrencyIntoDataBase(IList<CurrencyResponse> currencyList)
        {
            _logger.LogInformation("Establishing database connection.");
            try
            {
                var parameters = new  List<DopplerCurrencyRate>();

                foreach (var currency in currencyList)
                {
                    var param = new DopplerCurrencyRate
                    {
                        IdCurrencyTypeFrom = (int)CurrencyTypeEnum.USD,
                        IdCurrencyTypeTo = (int)Enum.Parse(typeof(CurrencyTypeEnum), currency.CurrencyCode),
                        Rate = currency.SaleValue
                    };

                    parameters.Add(param);
                }

                var storedProcedure = string.Join("\n", _dopplerCurrencySettings.InsertCurrencyQuery);

                await using var conn = _dbConnectionFactory.GetConnection();

                _logger.LogInformation("Sending SQL sentence to database server.");
                await conn.ExecuteAsync(storedProcedure,
                    parameters.ToArray(),
                    commandType: System.Data.CommandType.StoredProcedure);

            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Error sending SQL sentence to database server.");
                throw;
            }
        }
    }
}
