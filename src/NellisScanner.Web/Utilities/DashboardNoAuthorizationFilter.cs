using Hangfire.Annotations;
using Hangfire.Dashboard;

//https://discuss.hangfire.io/t/hangfire-path-on-staging-machine-throws-a-401-unauthorized/100/6

namespace NellisScanner.Web.Utilities;
public class DashboardNoAuthorizationFilter : IDashboardAuthorizationFilter, IDashboardAsyncAuthorizationFilter
{
    public bool Authorize(DashboardContext dashboardContext)
    {
        return true;
    }

    public Task<bool> AuthorizeAsync([NotNull] DashboardContext context)
    {
        return Task.FromResult(true);
    }
}