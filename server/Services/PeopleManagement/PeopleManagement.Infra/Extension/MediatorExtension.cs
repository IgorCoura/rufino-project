﻿using MediatR;
using PeopleManagement.Domain.SeedWord;
using PeopleManagement.Infra.Context;
using System.Diagnostics;

namespace PeopleManagement.Infra.Extension
{
    static class MediatorExtension
    {
        public static async Task DispatchDomainEventsAsync(this IMediator mediator, PeopleManagementContext ctx)
        {
            var domainEntities = ctx.ChangeTracker
                .Entries<Entity>()
                .Where(x => x.Entity.DomainEvents != null && x.Entity.DomainEvents.Count != 0);

            var domainEvents = domainEntities
                .SelectMany(x => x.Entity.DomainEvents)
                .ToList();

            domainEntities.ToList()
                .ForEach(entity => entity.Entity.ClearDomainEvents());



            foreach (var domainEvent in domainEvents)
            {
                await mediator.Publish(domainEvent);
            }
                
        }
    }
}
