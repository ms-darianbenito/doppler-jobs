using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using CrossCutting;
using CrossCutting.DopplerSapService.Entities;
using Dapper;
using Doppler.Currency.Job.Authorization;
using Doppler.Currency.Job.Enums;
using Doppler.Currency.Job.Settings;
using Doppler.Database;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace Doppler.Currency.Job.DopplerCurrencyService
{
    public class DopplerCurrencyService : IDopplerCurrencyService
    {
        private readonly DopplerCurrencyServiceSettings _dopplerCurrencySettings;
        private readonly TimeZoneJobConfigurations _jobConfig;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<DopplerCurrencyService> _logger;
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly JwtSecurityTokenHandler _tokenHandler;
        private readonly JwtOptions _jwtOptions;
        private readonly SigningCredentials _signingCredentials;

        public DopplerCurrencyService(
            IHttpClientFactory httpClientFactory,
            IOptionsMonitor<DopplerCurrencyServiceSettings> dopplerCurrencySettings,
            ILogger<DopplerCurrencyService> logger,
            TimeZoneJobConfigurations jobConfig,
            IDbConnectionFactory dbConnectionFactory,
            IOptions<JwtOptions> jwtOptions,
            SigningCredentials signingCredentials,
            JwtSecurityTokenHandler tokenHandler)
        {
            _dopplerCurrencySettings = dopplerCurrencySettings.CurrentValue;
            _jobConfig = jobConfig;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _dbConnectionFactory = dbConnectionFactory;
            _jwtOptions = jwtOptions.Value;
            _signingCredentials = signingCredentials;
            _tokenHandler = tokenHandler;
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
                    var jwtToken = CreateJwtToken();
                    var httpResponse = await GetCurrencyValue(currencyCode, cstTime, jwtToken);
                    var jsonResult = await httpResponse.Content.ReadAsStringAsync();

                    if (httpResponse.IsSuccessStatusCode)
                    {
                        var result = JsonConvert.DeserializeObject<CurrencyResponse>(jsonResult);

                        if (result.CotizationAvailable)
                        {
                            returnList.Add(result);
                        }
                        else
                        {
                            _logger.LogError("{ReasonPhrase}. Error getting currency for {currencyCode}.", httpResponse.ReasonPhrase, currencyCode);

                            ///Get the most recent previous price
                            result = await GetPreviousPrices(cstTime, currencyCode);

                            if (result == null)
                            {
                                _logger.LogError("{ReasonPhrase}. Error getting currency for {currencyCode} after trying for the last 5 business days", httpResponse.ReasonPhrase, currencyCode);
                            }
                            else
                            {
                                returnList.Add(result);
                            }
                        }
                    }
                    else
                    {
                        _logger.LogError("{ReasonPhrase}. Error getting currency for {currencyCode}.", httpResponse.ReasonPhrase, currencyCode);
                        continue;
                    }
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

        private async Task<HttpResponseMessage> GetCurrencyValue(string currencyCode , DateTime cstTime, string jwtToken)
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
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);

            return await client.SendAsync(httpRequest).ConfigureAwait(false);
        }

        private string CreateJwtToken()
        {
            var now = DateTime.UtcNow;

            var jwtToken = _tokenHandler.CreateToken(new SecurityTokenDescriptor
            {
                Expires = now.AddDays(_jwtOptions.TokenLifeTime),
                SigningCredentials = _signingCredentials
            }) as JwtSecurityToken;

            return _tokenHandler.WriteToken(jwtToken);
        }

        private async Task<CurrencyResponse> GetPreviousPrices(DateTime cstTime, string currencyCode)
        {
            var currentDate = cstTime;
            CurrencyResponse result = null;
            var jwtToken = CreateJwtToken();

            for (var count = 1;  count <= _dopplerCurrencySettings.HolidayRetryCountLimit; count++)
            {
                cstTime = cstTime.DayOfWeek == DayOfWeek.Monday ? cstTime.AddDays(-3) : cstTime.AddDays(-1);
                var httpResponse = await GetCurrencyValue(currencyCode, cstTime, jwtToken);
                var jsonResult = await httpResponse.Content.ReadAsStringAsync();

                if (httpResponse.IsSuccessStatusCode)
                {
                    var currencyResponse = JsonConvert.DeserializeObject<CurrencyResponse>(jsonResult);

                    if (currencyResponse.CotizationAvailable)
                    {
                        result = currencyResponse;
                        result.Date = $"{currentDate:yyyy-MM-dd}";
                        break;
                    }
                }
            }

            return result;
        }
    }
}
