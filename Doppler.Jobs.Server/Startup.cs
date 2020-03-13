using System.Collections.Generic;
using CrossCutting;
using Doppler.Sap.Job.Service;
using Hangfire;
using Hangfire.Storage.SQLite;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Doppler.Service.Job.Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHangfire(x =>
            {
                x.UseSQLiteStorage();
            });

            ConfigureJob(services);
            ConfigureJobsScheduler(services);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHangfireDashboard();
            app.UseHangfireServer();

            var jobScheduler = (JobScheduler)app.ApplicationServices.GetService(typeof(JobScheduler));
            jobScheduler.ScheduleJobs();
        }

        private void ConfigureJob(IServiceCollection services)
        {
            var jobConfig = new RecurringJobConfiguration
            {
                Identifier = $"{Configuration["Jobs:WorkerServiceJob:Identifier"]}",
                IntervalCronExpression = $"{Configuration["Jobs:WorkerServiceJob:IntervalCronExpression"]}"
            };

            services.AddTransient(sp => new SapServiceJob(
                sp.GetService<ILogger<SapServiceJob>>(),
                jobConfig.IntervalCronExpression, 
                jobConfig.Identifier));
        }

        private void ConfigureJobsScheduler(IServiceCollection services)
        {
            var jobsConfig = new TimeZoneJobConfigurations
            {
                TimeZoneJobs = $"{Configuration["TimeZoneJobs"]}"
            };
            services.AddTransient(sp =>
                new JobScheduler(new List<IRecurringJob>
                    {
                        services.BuildServiceProvider().GetService<SapServiceJob>()
                    },
                    sp.GetRequiredService<ILogger<JobScheduler>>(),
                    jobsConfig)
            );
        }
    }
}
