#pragma warning disable CS0618 // O arquivo INTEIRO e a feature Archive, descontinuada.
// Os tipos seguem marcados com [Obsolete] para quem estiver de fora; aqui dentro o aviso
// so produziria ruido no build de uma feature que ninguem deve mexer.
using Microsoft.EntityFrameworkCore;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Events;
using PeopleManagement.Infra.Context;
using static PeopleManagement.Application.Queries.ArchiveCategory.ArchiveCategoryDtos;
using static PeopleManagement.Application.Queries.Base.BaseDtos;

namespace PeopleManagement.Application.Queries.ArchiveCategory
{
    [Obsolete("Feature Archive descontinuada em 2026-09-04: o desenvolvimento parou no meio e os endpoints foram removidos. Nao estenda nem use em codigo novo; ver o plano de refatoracao de autorizacao no CLAUDE.md.")]
    public class ArchiveCategoryQueries(PeopleManagementContext peopleManagementContext) : IArchiveCategoryQueries
    {
        private PeopleManagementContext _context = peopleManagementContext;

        public async Task<IEnumerable<ArchiveCategoryDTO>> GetAll(Guid companyId)
        {
            var query = _context.ArchiveCategories.AsNoTracking().Where(x => x.CompanyId == companyId).OrderBy(x => x.Name);


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
