namespace PeopleManagement.Domain.AggregatesModel.ArchiveAggregate
{
    public class Archive : Entity, IAggregateRoot
    {
        private string? _storagePath;
        public Guid CompanyId { get; private set; }
        public Guid OwnerId { get; private set; }
        public string StoragePath 
        {
            get => _storagePath ?? throw new ArgumentNullException();
            private set 
            { 
                _storagePath = Path.Combine(CompanyId.ToString(), value);
            } 
        }
        public List<Document> Documents {  get; private set; }

        private Archive(Guid id, Guid OwnerId, Guid companyId, string storagePath) : base(id) 
        {
            CompanyId = companyId;
            StoragePath = storagePath;
            Documents = [];
        }

        public void AddDocument(Document document)
        {
            Documents.Add(document);
        }

        public static Archive Create(Guid id, Guid ownerId, Guid companyId, string storagePath) => new(id, ownerId, companyId, storagePath);
    }
}
