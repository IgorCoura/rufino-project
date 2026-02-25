using MediatR;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentGroupAggregate;

namespace PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate
{
    public class DocumentTemplate: Entity, IAggregateRoot
    {

        public Name Name { get; private set; } = null!;
        public Description Description { get; private set; } = null!;
        public Guid CompanyId { get; private set; }
        public TemplateFileInfo? TemplateFileInfo { get; private set; }
        public TimeSpan? DocumentValidityDuration { get; private set; }
        public TimeSpan? Workload { get; private set; }
        public List<PlaceSignature> PlaceSignatures { get; private set; } = [];
        public Guid DocumentGroupId { get; private set; }
        public bool UsePreviousPeriod { get; private set; }

        private DocumentTemplate() { }
        private DocumentTemplate(Guid id, Name name, Description description, Guid companyId, TimeSpan? documentValidityDuration, 
            TimeSpan? workload, TemplateFileInfo? templateFileInfo, List<PlaceSignature> placeSignatures, Guid documentGroupId,
            bool usePreviousPeriod) : base(id)
        {
            Name = name;
            Description = description;
            CompanyId = companyId;
            DocumentValidityDuration = documentValidityDuration;
            Workload = workload;
            TemplateFileInfo = templateFileInfo;
            PlaceSignatures = placeSignatures;  
            DocumentGroupId = documentGroupId;
            UsePreviousPeriod = usePreviousPeriod;
        }

        public static DocumentTemplate Create(Guid id, Name name, Description description, Guid companyId, TimeSpan? documentValidityDuration, 
            TimeSpan? workload, TemplateFileInfo? templateFileInfo, List<PlaceSignature> placeSignatures, Guid documentGroupId,
            bool usePreviousPeriod = false)
            => new(id, name, description, companyId, documentValidityDuration, workload, templateFileInfo, placeSignatures, documentGroupId, usePreviousPeriod);
        public static DocumentTemplate Create(Guid id, Name name, Description description, Guid companyId, double? documentValidityDurationInDays,
            double? workloadInHours, TemplateFileInfo? templateFileInfo, List<PlaceSignature> placeSignatures, Guid documentGroupId,
            bool usePreviousPeriod = false)
        {
            TimeSpan? documentValidityDuration = documentValidityDurationInDays.HasValue ? TimeSpan.FromDays((double)documentValidityDurationInDays!) : null;
            TimeSpan? workload = workloadInHours.HasValue ? TimeSpan.FromHours((double)workloadInHours!) : null;
            return new(id, name, description, companyId, documentValidityDuration, workload, templateFileInfo, placeSignatures, documentGroupId, usePreviousPeriod);
        }
        public void Edit(Name name, Description description, double? documentValidityDurationInDays,
            double? workloadInHours, TemplateFileInfo? templateFileInfo, List<PlaceSignature> placeSignatures, Guid documentGroupId,
            bool usePreviousPeriod = false)
        {
            Name = name;
            Description = description;
            DocumentValidityDuration = documentValidityDurationInDays.HasValue ? TimeSpan.FromDays((double)documentValidityDurationInDays!) : null;
            Workload = workloadInHours.HasValue ? TimeSpan.FromHours((double)workloadInHours!) : null;
            TemplateFileInfo = templateFileInfo;
            PlaceSignatures = placeSignatures;
            DocumentGroupId = documentGroupId;
            UsePreviousPeriod = usePreviousPeriod;
        }

        public bool IsSignable => PlaceSignatures.Count > 0;
    }
}
