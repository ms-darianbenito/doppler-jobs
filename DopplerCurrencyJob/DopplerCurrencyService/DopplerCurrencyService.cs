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
            var returnList = new List<CurrencyResponse>();
            
            foreach (var currencyCode in _dopplerCurrencySettings.CurrencyCodeList)
            {
                try
                {
                    var cstTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, cstZone);
                    var retryCount = 0;

                    var httpResponse = await GetCurrencyValue(currencyCode, cstTime);
                    var jsonResult = await httpResponse.Content.ReadAsStringAsync();
                    var isHoliday = jsonResult.Contains("Holiday Error") || jsonResult.Contains("Html Error Mxn currency");

                    if (!isHoliday && !httpResponse.IsSuccessStatusCode )
                    {
                        _logger.LogError(httpResponse.ReasonPhrase,"Error getting currency for {currencyCode}.", currencyCode);
                        continue;
                    }
                    else if (isHoliday)
                    {
                        while (retryCount < _dopplerCurrencySettings.HolidayRetryCountLimit)
                        {
                            cstTime = DateTime.UtcNow.DayOfWeek == DayOfWeek.Monday ? cstTime.AddDays(-3) : cstTime.AddDays(-1);
                            httpResponse = await GetCurrencyValue(currencyCode, cstTime);
                            if (httpResponse.IsSuccessStatusCode)
                                break;
                            retryCount += 1;
                        }
                    }

                    if (!httpResponse.IsSuccessStatusCode)
                    {
                        _logger.LogError(httpResponse.ReasonPhrase,"Error getting currency for {currencyCode} after trying for the last 5 business days", currencyCode);
                        continue;
                    }

                    var result = JsonConvert.DeserializeObject<CurrencyResponse>(jsonResult);

                    returnList.Add(result);
                }
                catch (Exception e)
                {
                    _logger.LogError(e,"Unexpected error getting currency for {currencyCode}.", currencyCode);
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

        private async Task<HttpResponseMessage> GetCurrencyValue(string currencyCode , DateTime cstTime )
        {
            var uri = new Uri(_dopplerCurrencySettings.Url + $"{currencyCode}/{cstTime.Year}-{cstTime.Month}-{cstTime.Day}");

            _logger.LogInformation("Building http request with url {uri}", uri);
            var httpRequest = new HttpRequestMessage
            {
                RequestUri = uri,
                Method = new HttpMethod("GET")

            };

            _logger.LogInformation("Sending request to Doppler Currency Api.");
            var client = _httpClientFactory.CreateClient();
            return await client.SendAsync(httpRequest).ConfigureAwait(false);
        }
    }
}
