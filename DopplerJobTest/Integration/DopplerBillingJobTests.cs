using System.Collections.Generic;
using CrossCutting.DopplerSapService;
using CrossCutting.DopplerSapService.Entities;
using Doppler.Billing.Job;
using Doppler.Billing.Job.Database;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Doppler.Jobs.Test.Integration
{
    public class DopplerBillingJobTests
    {
        
        private readonly Mock<ILogger<DopplerBillingJob>> _loggerMock;
        private readonly Mock<IDopplerSapService> _dopplerSapServiceMock;
        private readonly Mock<IDopplerRepository> _dopplerRepositoryMock;

        public DopplerBillingJobTests()
        {
            _loggerMock = new Mock<ILogger<DopplerBillingJob>>();
            _dopplerSapServiceMock = new Mock<IDopplerSapService>();
            _dopplerRepositoryMock = new Mock<IDopplerRepository>();
        }

        [Fact]
        public void DopplerBillingJob_ShouldBeNoSendDataToSap_WhenListIsHaveOneCurrencyArs()
        {
            _dopplerRepositoryMock.Setup(x => x.GetUserBillingInformation())
                .ReturnsAsync(new List<UserBilling>());

            var job = new DopplerBillingJob(
                _loggerMock.Object,
                _dopplerSapServiceMock.Object,
                _dopplerRepositoryMock.Object);

            job.Run();

            _loggerMock.VerifyLogger(LogLevel.Information, "Getting data from Doppler database.", Times.Once());
        }

        [Fact]
        public void DopplerBillingJob_ShouldBeSendDataToSap_WhenListIsHaveOneUserBillingCreated()
        {
            _dopplerRepositoryMock.Setup(x => x.GetUserBillingInformation())
                .ReturnsAsync(new List<UserBilling>
                {
                    new UserBilling()
                });

            var job = new DopplerBillingJob(
                _loggerMock.Object,
                _dopplerSapServiceMock.Object,
                _dopplerRepositoryMock.Object);

            job.Run();

            _loggerMock.VerifyLogger(LogLevel.Information, "Getting data from Doppler database.", Times.Once());
            _loggerMock.VerifyLogger(LogLevel.Information, "Sending Billing data to Doppler SAP system 1.", Times.Once());
        }
    }
}
