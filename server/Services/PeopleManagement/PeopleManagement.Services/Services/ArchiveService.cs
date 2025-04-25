using Microsoft.EntityFrameworkCore;
using PeopleManagement.Domain.AggregatesModel.ArchiveAggregate;
using PeopleManagement.Domain.AggregatesModel.ArchiveAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.ArchiveCategoryAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using System.Diagnostics;
using ArchiveFile = PeopleManagement.Domain.AggregatesModel.ArchiveAggregate.File;

namespace PeopleManagement.Services.Services
{
    public class ArchiveService(IArchiveRepository archiveRepository, IArchiveCategoryRepository archiveCategoryRepository, IBlobService blobService) : IArchiveService
    {
        private readonly IArchiveRepository _archiveRepository = archiveRepository;
        private readonly IArchiveCategoryRepository _archiveCategoryRepository = archiveCategoryRepository;
        private readonly IBlobService _blobService = blobService;

        public async Task RequiresFiles(Guid ownerId, Guid companyId, int eventId, CancellationToken cancellationToken = default)
        {

            var categories = await _archiveCategoryRepository.GetDataAsync(ac => ((string)(object)ac.ListenEventsIds).Contains(eventId.ToString()), cancellation: cancellationToken);

            foreach (var category in categories)
            {
                Archive? archive = await _archiveRepository.FirstOrDefaultAsync(x => x.OwnerId == ownerId && x.CompanyId == companyId && x.CategoryId == category.Id, cancellation: cancellationToken);
                if (archive is null)
                {
                    Debug.WriteLine($"Archive-> OwnerId: {ownerId}, EventId: {eventId}, CompanyId {companyId}");
                    var createArchive = Archive.Create(Guid.NewGuid(), category.Id, ownerId, companyId);
                    createArchive.RequestFile();
                    await _archiveRepository.InsertAsync(createArchive, cancellation: cancellationToken);
                    continue;
                }
                archive.RequestFile();
            }

        }   


        public async Task<Guid> InsertFile(Guid ownerId, Guid companyId, Guid categoryId, ArchiveFile file, Stream stream, CancellationToken cancellation = default)
        {
            Archive archive = await _archiveRepository.FirstOrDefaultAsync(x => x.OwnerId == ownerId && x.CompanyId == companyId && x.CategoryId == categoryId, cancellation: cancellation) ??
                throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Archive), ownerId.ToString()));
            
            archive.AddFile(file);
            await _blobService.UploadAsync(stream, file.GetNameWithExtension, archive.CompanyId.ToString(), cancellation);
            return archive.Id;
        }

        public async Task<bool> HasRequiresFiles(Guid ownerId, Guid companyId)
        {
            var has = await _archiveRepository.AnyAsync(x => x.OwnerId == ownerId && x.CompanyId == companyId && x.Status == ArchiveStatus.RequiresFile);
            return has;
        }
    }
}
