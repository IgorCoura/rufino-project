using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Domain.ErrorTools;

namespace PeopleManagement.Domain.AggregatesModel.ArchiveAggregate
{
    public sealed class Archive : Entity, IAggregateRoot
    {
        public List<File> Files { get; private set; } = [];
        public Guid CategoryId { get; private set; }
        public Guid OwnerId { get; private set; }
        public Guid CompanyId { get; private set; }
        public ArchiveStatus Status { get; private set; } = ArchiveStatus.OK;

        private Archive() { }
        private Archive(Guid id, Guid categoryId, Guid ownerId, Guid companyId): base(id) 
        {
            CategoryId = categoryId;
            OwnerId = ownerId;
            CompanyId = companyId;
        }

        public static Archive Create(Guid id, Guid categoryId, Guid ownerId, Guid companyId) => new(id, categoryId, ownerId, companyId);

        public void RequestFile()
        {
            Status = ArchiveStatus.RequiresFile;
        }

        public bool RequiresFile => Status == ArchiveStatus.RequiresFile;
        public bool RequiresVerification => Status == ArchiveStatus.RequiresVerification;

        public void AddFile(File file)
        {
            Files.Add(file);
            Status = file.RequiresVerification ? ArchiveStatus.RequiresVerification : ArchiveStatus.OK;
        }

        public void DocumentNotApplicable(Name fileName)
        {
            var document = Files.FirstOrDefault(x => x.Name == fileName)
               ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentUnit), fileName.ToString()));

            document.NotApplicable();
            Status = ArchiveStatus.OK;
        }


    }
}
