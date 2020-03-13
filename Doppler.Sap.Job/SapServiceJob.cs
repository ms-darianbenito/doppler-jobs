using System;
using System.Threading.Tasks;
using CrossCutting;
using Microsoft.Extensions.Logging;

namespace Doppler.Sap.Job.Service
{
    public class SapServiceJob : IRecurringJob
    {
        private readonly ILogger<SapServiceJob> _logger;
        public string Identifier { get; }
        public string IntervalCronExpression { get; }

        public SapServiceJob(ILogger<SapServiceJob> logger, string intervalCronExpression, string identifier)
        {
            _logger = logger;
            IntervalCronExpression = intervalCronExpression;
            Identifier = identifier;
        }

        public void Run()
        {
            RunAsync().GetAwaiter().GetResult();
        }

        private async Task RunAsync()
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            await Task.Delay(5000);
        }
    }
}
