namespace PeopleManagement.Domain.Events
{
    public record CreateArchiveDomainEvent : INotification
    {

        public Guid ArchiveId { get; private set; }
        public Guid CompanyId { get; private set; }
        public string StoragePath { get; private set; }

        public CreateArchiveDomainEvent(Guid archiveId, Guid companyId,  string storagePath)
        {
            ArchiveId = archiveId;
            CompanyId = companyId;
            StoragePath = storagePath;
        }


    }
        


}

