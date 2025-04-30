namespace PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate
{
    public class DocumentTemplate: Entity, IAggregateRoot
    {

        public Name Name { get;  set; } = null!;
        public Description Description { get;  set; } = null!;
        public Guid CompanyId { get; private set; }
        public TemplateFileInfo? TemplateFileInfo { get; private set; }
        public TimeSpan? DocumentValidityDuration { get;  set; }
        public TimeSpan? Workload { get;  set; } 

        private DocumentTemplate() { }
        private DocumentTemplate(Guid id, Name name, Description description, Guid companyId, TimeSpan? documentValidityDuration, 
            TimeSpan? workload, TemplateFileInfo? templateFileInfo) : base(id)
        {
            Name = name;
            Description = description;
            CompanyId = companyId;
            DocumentValidityDuration = documentValidityDuration;
            Workload = workload;
            TemplateFileInfo = templateFileInfo;
        }

        public static DocumentTemplate Create(Guid id, Name name, Description description, Guid companyId, TimeSpan? documentValidityDuration, 
            TimeSpan? workload, TemplateFileInfo? templateFileInfo)
            => new(id, name, description, companyId, documentValidityDuration, workload, templateFileInfo);
    }
}
