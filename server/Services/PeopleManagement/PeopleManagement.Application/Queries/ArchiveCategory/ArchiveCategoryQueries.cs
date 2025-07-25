﻿using Microsoft.EntityFrameworkCore;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Events;
using PeopleManagement.Infra.Context;
using static PeopleManagement.Application.Queries.ArchiveCategory.ArchiveCategoryDtos;
using static PeopleManagement.Application.Queries.Base.BaseDtos;

namespace PeopleManagement.Application.Queries.ArchiveCategory
{
    public class ArchiveCategoryQueries(PeopleManagementContext peopleManagementContext) : IArchiveCategoryQueries
    {
        private PeopleManagementContext _context = peopleManagementContext;

        public async Task<IEnumerable<ArchiveCategoryDTO>> GetAll(Guid companyId)
        {
            var query = _context.ArchiveCategories.AsNoTracking().Where(x => x.CompanyId == companyId);


            var result = await query.ToArrayAsync();

            var list = result.Select(c => new ArchiveCategoryDTO
            {
                Id = c.Id,
                Name = c.Name.Value,
                Description = c.Description.Value,
                ListenEvents = c.ListenEventsIds.Select(x => new EnumerationDto
                {
                    Id = x,
                    Name = EmployeeEvent.FromValue(x)?.Name ?? "",
                }).ToArray(),
                CompanyId = c.CompanyId,
            }).ToArray();

            return list;
        }

    }
}
