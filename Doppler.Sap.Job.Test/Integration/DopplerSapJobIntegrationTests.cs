using Doppler.Worker.Test.Integration;
using Xunit;

namespace Doppler.Jobs.Test.Integration
{
    public class DopplerSapJobIntegrationTests : IClassFixture<TestServerFixture>
    {
        private readonly TestServerFixture _testServer;

        public DopplerSapJobIntegrationTests(TestServerFixture testServerFixture)
        {
            _testServer = testServerFixture;
        }

        [Fact]
        public void Test2()
        {
            Assert.True(true);
        }
    }
}
