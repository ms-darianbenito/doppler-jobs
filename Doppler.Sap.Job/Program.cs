using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Doppler.Sap.Job.Service
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    Configure(services);
                });

        private static void Configure(IServiceCollection services)
        {
        }
    }
}
