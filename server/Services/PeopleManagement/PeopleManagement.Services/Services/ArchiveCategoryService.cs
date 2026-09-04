#pragma warning disable CS0618 // O arquivo INTEIRO e a feature Archive, descontinuada.
// Os tipos seguem marcados com [Obsolete] para quem estiver de fora; aqui dentro o aviso
// so produziria ruido no build de uma feature que ninguem deve mexer.
using PeopleManagement.Domain.AggregatesModel.ArchiveCategoryAggregate;
using PeopleManagement.Domain.AggregatesModel.ArchiveCategoryAggregate.Interfaces;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Events;
using Microsoft.Extensions.Logging;

namespace PeopleManagement.Services.Services
{
    [Obsolete("Feature Archive descontinuada em 2026-09-04: o desenvolvimento parou no meio e os endpoints foram removidos. Nao estenda nem use em codigo novo; ver o plano de refatoracao de autorizacao no CLAUDE.md.")]
    public class ArchiveCategoryService(IArchiveCategoryRepository archiveCategoryRepository) : IArchiveCategoryService
    {
        private readonly IArchiveCategoryRepository _archiveCategoryRepository = archiveCategoryRepository;

        public async Task<Guid> CrateArchiveCategory(string Name, string Description, Guid CompanyId, int[] ListenEventsIds, CancellationToken cancellationToken)
        {
            foreach (var eventId in ListenEventsIds)
            {
                if ( EmployeeEvent.EventExist(eventId) == false)
                    throw new DomainException(this, DomainErrors.ArchiveCategory.EventNotExist(eventId));
            }

            var archiveCategory = ArchiveCategory.Create(Guid.NewGuid(), Name, Description, ListenEventsIds.ToList(), CompanyId);
            await _archiveCategoryRepository.InsertAsync(archiveCategory, cancellationToken);

            return archiveCategory.Id;
        }
        public async Task AddListenEvent(Guid archiveCategoryId, Guid companyId, int[] eventIds, CancellationToken cancellationToken)
        {
            var category = await _archiveCategoryRepository.FirstOrDefaultAsync(x => x.Id == archiveCategoryId && x.CompanyId == companyId, cancellation: cancellationToken)
            ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(ArchiveCategory), archiveCategoryId.ToString()));

            foreach(var eventId in eventIds)
            {
                if (EmployeeEvent.EventExist(eventId) == false)
                    throw new DomainException(this, DomainErrors.ArchiveCategory.EventNotExist(eventId));
                category.AddListenEvent(eventId);
            }
        }

      
    }
}
