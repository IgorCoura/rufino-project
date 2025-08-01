using PeopleManagement.Domain.AggregatesModel.WebHookAggregate;
using PeopleManagement.Infra.Context;
namespace PeopleManagement.Infra.Repository
{
    public class WebHookRepository(PeopleManagementContext context) : Repository<WebHook>(context), IWebHookRepository
    {

    }
}
