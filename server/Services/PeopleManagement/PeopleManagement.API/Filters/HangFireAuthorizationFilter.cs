using Hangfire.Dashboard;

namespace PeopleManagement.API.Filters
{
    public class HangFireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        
        public bool Authorize(DashboardContext context)
        {

            return true;
        }
        
    }
}
