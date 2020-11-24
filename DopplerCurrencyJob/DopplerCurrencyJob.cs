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
            LogInformationAndAppendToStringBuilder(resultBuffer, "Getting currency per each code enabled.");

            var currencyDto = await _dopplerCurrencyService.GetCurrencyByCode();

            if (!currencyDto.Any())
            {
                LogWarningAndAppendToStringBuilder(resultBuffer, "Non-existent currencies for this date, please check for errors.");
                return new DopplerCurrencyJobResponse()
                {
                    CurrencyList = new List<CurrencyResponse>(),
                    Message = resultBuffer.ToString()
                };
            }

            LogInformationAndAppendToStringBuilder(resultBuffer, "Currency retrieved by code:");
            AppendSerializedToStringBuilder(resultBuffer, currencyDto);

            try
            {
                LogInformationAndAppendToStringBuilder(resultBuffer, "Sending currency data to Doppler SAP system.");
                var result = await _dopplerSapService.SendCurrency(currencyDto);

                if (!result.IsSuccessStatusCode)
                {
                    LogErrorAndAppendToStringBuilder(resultBuffer, $"{result.ReasonPhrase}. Error sending currency to Doppler SAP Api.", null);
                    taskFailed = true;
                }
                else
                {
                    LogInformationAndAppendToStringBuilder(resultBuffer, "Sent currency data to Doppler SAP system.");
                }
            }
            catch (Exception e)
            {
                LogErrorAndAppendToStringBuilder(resultBuffer, $"An exception occurred when sending currency to Doppler SAP Api.", e);
                taskFailed = true;
            }

            try
            {
                LogInformationAndAppendToStringBuilder(resultBuffer, "Insert currency data into Doppler Database.");

                await _dopplerCurrencyService.InsertCurrencyIntoDataBase(currencyDto);

                LogInformationAndAppendToStringBuilder(resultBuffer, "Inserted currency data into Doppler Database.");
            }
            catch (Exception e)
            {
                LogErrorAndAppendToStringBuilder(resultBuffer, $"An exception occurred when Insert currency data into Doppler Database.", e);
                taskFailed = true;
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

        private void LogInformationAndAppendToStringBuilder(StringBuilder buffer, string message)
        {
            _logger.LogInformation(message);
            buffer.Append(message).AppendLine();
        }
        private void LogWarningAndAppendToStringBuilder(StringBuilder buffer, string message)
        {
            _logger.LogWarning(message);
            buffer.Append(message).AppendLine();
        }
        private void LogErrorAndAppendToStringBuilder(StringBuilder buffer, string message, Exception exception)
        {
            _logger.LogError(exception, message);
            buffer.Append(message).AppendLine();
        }

        private void AppendSerializedToStringBuilder(StringBuilder buffer, IList<CurrencyResponse> currencyDto)
        {
            buffer.Append(JsonSerializer.Serialize(currencyDto)).AppendLine();
        }
    }
}
