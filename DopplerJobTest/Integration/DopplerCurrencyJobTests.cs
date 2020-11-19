using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using CrossCutting.DopplerSapService;
using CrossCutting.DopplerSapService.Entities;
using Doppler.Currency.Job;
using Doppler.Currency.Job.DopplerCurrencyService;
using Doppler.Currency.Job.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Doppler.Jobs.Test.Integration
{
    public class DopplerCurrencyJobTests
    {
        private readonly Mock<IDopplerCurrencyService> _dopplerCurrencyServiceMock;
        private readonly Mock<ILogger<DopplerCurrencyJob>> _loggerMock;
        private readonly Mock<IDopplerSapService> _dopplerSapServiceMock;

        public DopplerCurrencyJobTests()
        {
            _dopplerCurrencyServiceMock = new Mock<IDopplerCurrencyService>();
            _loggerMock = new Mock<ILogger<DopplerCurrencyJob>>();
            _dopplerSapServiceMock = new Mock<IDopplerSapService>();
        }

        [Fact]
        public void DopplerCurrencyJob_ShouldBeNoSendDataToSap_WhenListIsEmpty()
        {
            _dopplerCurrencyServiceMock.Setup(x => x.GetCurrencyByCode())
                .ReturnsAsync(new List<CurrencyResponse>());

            var job = new DopplerCurrencyJob(
                _loggerMock.Object,
                _dopplerCurrencyServiceMock.Object,
                _dopplerSapServiceMock.Object);

            job.Run();

            _loggerMock.VerifyLogger(LogLevel.Information, "Getting currency per each code enabled.", Times.Once());
            _loggerMock.VerifyLogger(LogLevel.Information, "Sending data to Doppler SAP system.", Times.Never());
        }

        [Fact]
        public void DopplerCurrencyJob_ShouldBeSendDataToSap_WhenListHasManyItems()
        {
            _dopplerCurrencyServiceMock.Setup(x => x.GetCurrencyByCode())
                .ReturnsAsync(new List<CurrencyResponse>
                {
                    new CurrencyResponse
                    {
                        CurrencyName = "ARS"
                    },
                    new CurrencyResponse
                    {
                        CurrencyName = "MNX"
                    },
                    new CurrencyResponse
                    {
                        CurrencyName = "COP"
                    }
                });

            _dopplerSapServiceMock.Setup(x => x.SendCurrency(It.IsAny<IList<CurrencyResponse>>()))
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.OK
                });

            var job = new DopplerCurrencyJob(
                _loggerMock.Object,
                _dopplerCurrencyServiceMock.Object,
                _dopplerSapServiceMock.Object);

            var data = job.Run();

            Assert.Equal(3, data.CurrencyList.Count);
            _loggerMock.VerifyLogger(LogLevel.Information, "Getting currency per each code enabled.", Times.Once());
            _loggerMock.VerifyLogger(LogLevel.Information, "Sending currency data to Doppler SAP system.", Times.Once());
            _loggerMock.VerifyLogger(LogLevel.Information, "Insert currency data into Doppler Database.", Times.Once());
        }
    }
}
