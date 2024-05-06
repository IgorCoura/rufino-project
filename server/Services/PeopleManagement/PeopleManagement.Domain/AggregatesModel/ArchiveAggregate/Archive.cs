namespace PeopleManagement.Domain.AggregatesModel.ArchiveAggregate
{
    public class Archive : Entity, IAggregateRoot
    {
        public IEnumerable<File> Files { get; private set; } = [];
        public Category Category { get; private set; } = null!;
        public Guid OwnerId { get; private set; }
        public Guid CompanyId { get; private set; }
        public ArchiveStatus Status { get; private set; } = ArchiveStatus.OK;

        private Archive(Guid id, Category category, Guid ownerId, Guid companyId): base(id) 
        {
            Category = category;
            OwnerId = ownerId;
            CompanyId = companyId;
        }

        public static Archive Create(Guid id, Category category, Guid ownerId, Guid companyId) => new(id, category, ownerId, companyId);

        public void RequestFile()
        {
            Status = ArchiveStatus.RequiresFile;
        }

        public bool RequiresFile => Status == ArchiveStatus.RequiresFile;
        public bool RequiresVerification => Status == ArchiveStatus.RequiresVerification;

        public void AddFile(File file)
        {
            Files = Files.Append(file);
            Status = file.RequiresVerification ? ArchiveStatus.RequiresVerification : ArchiveStatus.OK;
        }

        public string GetArchivePath()
        {
            return Path.Combine(CompanyId.ToString(), OwnerId.ToString(), Category.Name);
        }

        public string GetFilePath(File file)
        {
            return Path.Combine(CompanyId.ToString(), OwnerId.ToString(), Category.Name, file.GetNameWithExtension);
        }
       
    }
}
