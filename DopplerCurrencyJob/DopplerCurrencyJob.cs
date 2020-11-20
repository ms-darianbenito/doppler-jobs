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
            LogAndAppendToStringBuilder(LogLevel.Information, resultBuffer, "Getting currency per each code enabled.");

            var currencyDto = await _dopplerCurrencyService.GetCurrencyByCode();

            if (!currencyDto.Any())
            {
                LogAndAppendToStringBuilder(LogLevel.Warning, resultBuffer, "Non-existent currencies for this date, please check for errors.");
                return new DopplerCurrencyJobResponse()
                {
                    CurrencyList = new List<CurrencyResponse>(),
                    Message = resultBuffer.ToString()
                };
            }

            LogAndAppendToStringBuilder(LogLevel.Information, resultBuffer, "Currency retrieved by code:");
            AppendSerializedToStringBuilder(resultBuffer, currencyDto);

            try
            {
                LogAndAppendToStringBuilder(LogLevel.Information, resultBuffer, "Sending currency data to Doppler SAP system.");
                var result = await _dopplerSapService.SendCurrency(currencyDto);

                if (!result.IsSuccessStatusCode)
                {
                    LogAndAppendToStringBuilder(LogLevel.Error, resultBuffer, $"{result.ReasonPhrase}. Error sending currency to Doppler SAP Api.");
                    taskFailed = true;
                }
                else
                {
                    LogAndAppendToStringBuilder(LogLevel.Information, resultBuffer, "Sent currency data to Doppler SAP system.");
                }
            }
            catch (Exception e)
            {
                LogAndAppendToStringBuilder(LogLevel.Error, resultBuffer, $"An exception occurred when sending currency to Doppler SAP Api. Exception: {e.Message}");
                taskFailed = true;
            }

            try
            {
                LogAndAppendToStringBuilder(LogLevel.Information, resultBuffer, "Insert currency data into Doppler Database.");

                await _dopplerCurrencyService.InsertCurrencyIntoDataBase(currencyDto);

                LogAndAppendToStringBuilder(LogLevel.Information, resultBuffer, "Inserted currency data into Doppler Database.");
            }
            catch (Exception e)
            {
                LogAndAppendToStringBuilder(LogLevel.Error, resultBuffer, $"An exception occurred when Insert currency data into Doppler Database. Exception: {e.Message}");
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

        private void LogAndAppendToStringBuilder(LogLevel logLevel, StringBuilder buffer, string message)
        {
            switch (logLevel)
            {
                case LogLevel.Trace:
                case LogLevel.Debug:
                case LogLevel.Information:
                    _logger.LogInformation(message);
                    break;
                case LogLevel.Warning:
                    _logger.LogWarning(message);
                    break;
                case LogLevel.Error:
                case LogLevel.Critical:
                    _logger.LogError(message);
                    break;
                case LogLevel.None:
                default:
                    break;
            }

            buffer.Append(message).AppendLine();
        }

        private void AppendSerializedToStringBuilder(StringBuilder buffer, IList<CurrencyResponse> currencyDto)
        {
            buffer.Append(JsonSerializer.Serialize(currencyDto)).AppendLine();
        }
    }
}
