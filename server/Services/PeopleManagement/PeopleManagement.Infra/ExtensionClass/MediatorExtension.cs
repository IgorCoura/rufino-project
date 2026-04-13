using MediatR;
using PeopleManagement.Domain.SeedWord;
using PeopleManagement.Infra.Context;
using System.Diagnostics;

namespace PeopleManagement.Infra.ExtensionClass
{
    static class MediatorExtension
    {
        public static async Task DispatchDomainEventsAsync(this IMediator mediator, PeopleManagementContext ctx)
        {
            while (true)
            {
                var domainEntities = ctx.ChangeTracker
                    .Entries<Entity>()
                    .Where(x => x.Entity.DomainEvents != null && x.Entity.DomainEvents.Count != 0)
                    .ToList();

                if (domainEntities.Count == 0)
                    break;

                var domainEvents = domainEntities
                    .SelectMany(x => x.Entity.DomainEvents)
                    .ToList();

                domainEntities.ForEach(entity => entity.Entity.ClearDomainEvents());

                foreach (var domainEvent in domainEvents)
                {
                    await mediator.Publish(domainEvent);
                }
            }
        }
    }
}
