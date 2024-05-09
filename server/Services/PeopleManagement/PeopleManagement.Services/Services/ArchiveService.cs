using PeopleManagement.Domain.AggregatesModel.ArchiveAggregate;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using ArchiveFile = PeopleManagement.Domain.AggregatesModel.ArchiveAggregate.File;

namespace PeopleManagement.Services.Services
{
    public class ArchiveService : IArchiveService
    {
        private readonly IArchiveRepository _archiveRepository;

        public ArchiveService(IArchiveRepository archiveRepository)
        {
            _archiveRepository = archiveRepository;
        }
        

        public async Task RequiresFile(Guid ownerId, Guid companyId, Category category)
        {
            Archive? archive = await _archiveRepository.FirstOrDefaultAsync(x => x.OwnerId == ownerId && x.CompanyId == companyId && x.Category == category);

            archive ??= Archive.Create(Guid.NewGuid(), category, ownerId, companyId);

            archive.RequestFile();
        }

        public async Task RequiresFiles(Guid ownerId, Guid companyId, Category[] category)
        {
            var tasks = category.Select(item => RequiresFile(ownerId, companyId, item));
            await Task.WhenAll(tasks);
        }

        public async Task InsertFile(Guid ownerId, Guid companyId, Category category, ArchiveFile file, Stream stream)
        {
            Archive archive = await _archiveRepository.FirstOrDefaultAsync(x => x.OwnerId == ownerId && x.CompanyId == companyId && x.Category == category) ??
                throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Archive), ownerId.ToString()));
  
            archive.AddFile(file);
            var path = archive.GetFilePath(file);
            //TODO: Amarzenar arquivo no blob

        }

        public async Task<IEnumerable<Archive>> GetArchivesWithRequiresFiles(Guid ownerId, Guid companyId)
        {
            var archives = await _archiveRepository.GetDataAsync(x => x.OwnerId == ownerId && x.CompanyId == companyId && x.RequiresFile);
            return archives;
        }

        public async Task<bool> HasRequiresFiles(Guid ownerId, Guid companyId)
        {
            var has = await _archiveRepository.AnyAsync(x => x.OwnerId == ownerId && x.CompanyId == companyId && x.RequiresFile);
            return has;
        }
    }
}
