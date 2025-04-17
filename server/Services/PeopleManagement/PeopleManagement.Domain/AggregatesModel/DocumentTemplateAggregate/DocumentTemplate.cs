namespace PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate
{
    public class DocumentTemplate: Entity, IAggregateRoot
    {

        public Name Name { get;  set; } = null!;
        public Description Description { get;  set; } = null!;
        public Guid CompanyId { get; private set; } 
        public DirectoryName Directory { get; private set; } = null!;
        public FileName BodyFileName { get;  set; } = null!;
        public FileName HeaderFileName { get;  set; } = null!;
        public FileName FooterFileName { get;  set; } = null!;
        public RecoverDataType RecoverDataType { get;  set; } = null!;
        public TimeSpan? DocumentValidityDuration { get;  set; }
        public TimeSpan? Workload { get;  set; }
        public List<PlaceSignature> PlaceSignatures { get;  set; } = [];

        private DocumentTemplate() { }

        private DocumentTemplate(Guid id, Name name, Description description, Guid companyId, DirectoryName directory, FileName bodyFileName, FileName headerFileName, FileName footerFileName, RecoverDataType recoverDataType, TimeSpan? documentValidityDuration, TimeSpan? workload, List<PlaceSignature> placeSignatures) : base(id)
        {
            Name = name;
            Description = description;
            CompanyId = companyId;
            Directory = directory;
            BodyFileName = bodyFileName;
            HeaderFileName = headerFileName;
            FooterFileName = footerFileName;
            RecoverDataType = recoverDataType;
            DocumentValidityDuration = documentValidityDuration;
            Workload = workload;
            PlaceSignatures = placeSignatures;
        }

        public static DocumentTemplate Create(Guid id, Name name, Description description, Guid companyId, DirectoryName directory, FileName bodyFileName, FileName headerFileName, FileName footerFileName, RecoverDataType recoverDataType, TimeSpan? documentValidityDuration, TimeSpan? workload, List<PlaceSignature> placeSignatures)
            => new(id, name, description, companyId, directory, bodyFileName, headerFileName, footerFileName, recoverDataType, documentValidityDuration, workload, placeSignatures);
    }
}
