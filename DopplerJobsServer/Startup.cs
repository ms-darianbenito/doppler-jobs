using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using CrossCutting;
using CrossCutting.DopplerSapService;
using CrossCutting.DopplerSapService.Settings;
using Doppler.Billing.Job;
using Doppler.Billing.Job.Database;
using Doppler.Billing.Job.Settings;
using Doppler.Currency.Job;
using Doppler.Currency.Job.DopplerCurrencyService;
using Doppler.Currency.Job.Settings;
using Hangfire;
using Hangfire.Storage.SQLite;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;
using Polly.Extensions.Http;

namespace Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHangfire(x =>
            {
                x.UseSQLiteStorage();
            });

            var httpClientPolicies = new HttpClientPoliciesSettings();
            Configuration.GetSection("HttpClient:Client").Bind(httpClientPolicies);
            services.AddSingleton(httpClientPolicies);

            var handlerHttpClient = new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Automatic,
                SslProtocols = SslProtocols.Tls12
            };

            services.AddHttpClient(httpClientPolicies.ClientName, c => { })
                .ConfigurePrimaryHttpMessageHandler(() => handlerHttpClient)
                .AddTransientHttpErrorPolicy(builder => GetRetryPolicy(httpClientPolicies.Policies.RetryAttemps));

            var dopplerCurrencySettings = new DopplerCurrencyServiceSettings
            {
                CurrencyCodeList = Configuration.GetSection("DopplerCurrencyServiceSettings:CurrencyCode")
                    .Get<List<string>>()
            };
            Configuration.GetSection(nameof(DopplerCurrencyServiceSettings)).Bind(dopplerCurrencySettings);
            services.AddSingleton(dopplerCurrencySettings);

            var jobsConfig = new TimeZoneJobConfigurations
            {
                TimeZoneJobs = Configuration["TimeZoneJobs"]
            };
            services.AddSingleton(jobsConfig);

            services.AddTransient<IDopplerCurrencyService, DopplerCurrencyService>();

            services.Configure<DopplerSapServiceSettings>(Configuration.GetSection(nameof(DopplerSapServiceSettings)));

            services.AddTransient<IDopplerSapService, DopplerSapService>();

            services.Configure<DopplerRepositorySettings>(Configuration.GetSection(nameof(DopplerRepositorySettings)));

            services.AddTransient<IDopplerRepository, DopplerRepository>();

            ConfigureJobsScheduler();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHangfireDashboard();
            app.UseHangfireServer(new BackgroundJobServerOptions
            {
                WorkerCount = 1
            });
        }

        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(int retry)
        {
            HttpStatusCode[] httpStatusCodesWorthRetrying =
            {
                HttpStatusCode.RequestTimeout,
                HttpStatusCode.InternalServerError,
                HttpStatusCode.BadGateway,
                HttpStatusCode.ServiceUnavailable,
                HttpStatusCode.GatewayTimeout
            };

            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(r => httpStatusCodesWorthRetrying.Contains(r.StatusCode))
                .WaitAndRetryAsync(retry, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
        }

        private void ConfigureJobsScheduler()
        {
            JobStorage.Current = new SQLiteStorage("Hangfire.db");

            RecurringJob.AddOrUpdate<DopplerBillingJob>(
                Configuration["Jobs:DopplerBillingJob:Identifier"],
                job => job.Run(),
                Configuration["Jobs:DopplerBillingJob:IntervalCronExpression"],
                TimeZoneInfo.FindSystemTimeZoneById(Configuration["TimeZoneJobs"]));

            RecurringJob.AddOrUpdate<DopplerCurrencyJob>(
                Configuration["Jobs:DopplerCurrencyJob:Identifier"],
                job => job.Run(),
                Configuration["Jobs:DopplerCurrencyJob:IntervalCronExpression"],
                TimeZoneInfo.FindSystemTimeZoneById(Configuration["TimeZoneJobs"]));
        }
    }
}
