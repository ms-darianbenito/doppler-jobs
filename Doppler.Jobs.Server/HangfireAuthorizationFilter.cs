using Hangfire.Annotations;
using Hangfire.Dashboard;

namespace Doppler.Service.Job.Server
{
    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize([NotNull] DashboardContext context)
        {
            return true;
        }
    }
}