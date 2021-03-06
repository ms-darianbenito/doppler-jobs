﻿using System;
using Doppler.Billing.Job;
using Doppler.Currency.Job;
using Doppler.Currency.Job.DopplerCurrencyService;
using Microsoft.Extensions.Logging;
using Moq;

namespace Doppler.Jobs.Test
{
    public static class HelperExtension
    {
        public static void VerifyLogger(this Mock<ILogger<DopplerBillingJob>> logger, LogLevel logLevel, string textCheck, Times times)
        {
            logger.Verify(
                x => x.Log(
                    logLevel,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Equals(textCheck)),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                times);
        }

        public static void VerifyLogger(this Mock<ILogger<DopplerCurrencyJob>> logger, LogLevel logLevel, string textCheck, Times times)
        {
            logger.Verify(
                x => x.Log(
                    logLevel,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Equals(textCheck)),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                times);
        }

        public static void VerifyLogger(this Mock<ILogger<DopplerCurrencyService>> logger, LogLevel logLevel, string textCheck, Times times)
        {
            logger.Verify(
                x => x.Log(
                    logLevel,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Equals(textCheck)),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                times);
        }
    }
}
