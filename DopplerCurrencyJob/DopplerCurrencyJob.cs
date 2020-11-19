using CrossCutting.DopplerSapService;
using CrossCutting.DopplerSapService.Entities;
using Doppler.Currency.Job.DopplerCurrencyService;
using Doppler.Currency.Job.Exceptions;
using Hangfire;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Doppler.Currency.Job
{
    public class DopplerCurrencyJob
    {
        private readonly ILogger<DopplerCurrencyJob> _logger;
        private readonly IDopplerCurrencyService _dopplerCurrencyService;
        private readonly IDopplerSapService _dopplerSapService;

        public DopplerCurrencyJob(
            ILogger<DopplerCurrencyJob> logger,
            IDopplerCurrencyService dopplerCurrencyService,
            IDopplerSapService dopplerSapService)
        {
            _logger = logger;
            _dopplerCurrencyService = dopplerCurrencyService;
            _dopplerSapService = dopplerSapService;
        }


        [AutomaticRetry(OnAttemptsExceeded = AttemptsExceededAction.Delete, Attempts = 0)]
        public DopplerCurrencyJobResponse Run() => RunAsync().GetAwaiter().GetResult();

        private async Task<DopplerCurrencyJobResponse> RunAsync()
        {
            var taskFailed = false;

            var resultBuffer = new StringBuilder();
            var message = "Getting currency per each code enabled.";
            _logger.LogInformation(message);
            resultBuffer.Append(message).AppendLine();

            var currencyDto = await _dopplerCurrencyService.GetCurrencyByCode();
            if (!currencyDto.Any())
            {
                message = "Non-existent currencies for this date, please check for errors.";
                _logger.LogInformation(message);
                resultBuffer.Append(message).AppendLine();
                return new DopplerCurrencyJobResponse()
                {
                    CurrencyList = new List<CurrencyResponse>(),
                    Message = resultBuffer.ToString()
                };
            }

            message = "Currency retrieved by code:";
            _logger.LogInformation(message);
            resultBuffer.Append(message).AppendLine();
            resultBuffer.Append(JsonSerializer.Serialize(currencyDto)).AppendLine();

            try
            {
                message = "Sending currency data to Doppler SAP system.";
                _logger.LogInformation(message);
                resultBuffer.Append(message).AppendLine();

                var result = await _dopplerSapService.SendCurrency(currencyDto);

                if (!result.IsSuccessStatusCode)
                {
                    message = $"{result.ReasonPhrase}. Error sending currency to Doppler SAP Api.";
                    _logger.LogError(message);
                    resultBuffer.Append(message).AppendLine();
                    taskFailed = true;
                }
            }
            catch (Exception e)
            {
                message = $"An exception occurred when sending currency to Doppler SAP Api. Exception: {e.Message}";
                _logger.LogError(message);
                resultBuffer.Append(message).AppendLine();
                taskFailed = true;
            }
            finally
            {
                message = "Sent currency data to Doppler SAP system.";
                _logger.LogInformation(message);
                resultBuffer.Append(message).AppendLine();
            }

            try
            {
                message = "Insert currency data into Doppler Database.";
                _logger.LogInformation(message);
                await _dopplerCurrencyService.InsertCurrencyIntoDataBase(currencyDto);
                resultBuffer.Append(message).AppendLine();
            }
            catch (Exception e)
            {
                message = $"An exception occurred when Insert currency data into Doppler Database. Exception: {e.Message}";
                _logger.LogError(message);
                resultBuffer.Append(message).AppendLine();
                taskFailed = true;
            }
            finally
            {
                message = "Inserted currency data into Doppler Database.";
                _logger.LogInformation(message);
                resultBuffer.Append(message).AppendLine();
            }

            if (taskFailed)
            {
                throw new DopplerCurrencyJobException(resultBuffer.ToString());
            }

            return new DopplerCurrencyJobResponse()
            {
                CurrencyList = currencyDto,
                Message = resultBuffer.ToString()
            };
        }
    }
}
