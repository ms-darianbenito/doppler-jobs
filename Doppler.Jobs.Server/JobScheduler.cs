using System;
using System.Collections.Generic;
using CrossCutting;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace Doppler.Service.Job.Server
{
    public class JobScheduler
    {
        private readonly TimeZoneJobConfigurations _timeZoneJobsConf;
        private readonly ICollection<IRecurringJob> _recurringJobs;
        private readonly ILogger<JobScheduler> _logger;

        public JobScheduler(
            ICollection<IRecurringJob> list, 
            ILogger<JobScheduler> logger,
            TimeZoneJobConfigurations jobsConfig)
        {
            _recurringJobs = list;
            _logger = logger;
            _timeZoneJobsConf = jobsConfig;
        }

        public void ScheduleJobs()
        {
            foreach (var recurringJob in _recurringJobs)
            {
                _logger.LogInformation($"Scheduling recurring job {recurringJob.GetType()}.");
                RecurringJob.AddOrUpdate(recurringJob.Identifier, () => recurringJob.Run(),
                    recurringJob.IntervalCronExpression, TimeZoneInfo.FindSystemTimeZoneById(_timeZoneJobsConf.TimeZoneJobs));
            }
        }
    }
}