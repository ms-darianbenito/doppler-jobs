using Hangfire.Annotations;
using Hangfire.Dashboard;

namespace Server
{
    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize([NotNull] DashboardContext context)
        {
            return true;
        }
    }
}