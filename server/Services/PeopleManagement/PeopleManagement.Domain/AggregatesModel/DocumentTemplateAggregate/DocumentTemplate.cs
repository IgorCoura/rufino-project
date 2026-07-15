using MediatR;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentGroupAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Policies;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;

namespace PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate
{
    public class DocumentTemplate: Entity, IAggregateRoot
    {
        private readonly List<DocumentPolicy> _policies = [];

        public Name Name { get; private set; } = null!;
        public Description Description { get; private set; } = null!;
        public Guid CompanyId { get; private set; }
        public TemplateFileInfo? TemplateFileInfo { get; private set; }
        public TimeSpan? DocumentValidityDuration { get; private set; }
        public TimeSpan? Workload { get; private set; }
        public bool AcceptsSignature { get; private set; }
        public List<PlaceSignature> PlaceSignatures { get; private set; } = [];
        public Guid DocumentGroupId { get; private set; }
        public bool UsePreviousPeriod { get; private set; }

        /// <summary>
        /// Conjunto de regras que o template compõe. Presença de uma policy = regra ativa (Composite).
        /// Durante a transição (Fase 2) é derivado dos campos escalares, que seguem sendo a fonte da verdade.
        /// </summary>
        public IReadOnlyCollection<DocumentPolicy> Policies => _policies.AsReadOnly();

        private DocumentTemplate() { }
        private DocumentTemplate(Guid id, Name name, Description description, Guid companyId, TimeSpan? documentValidityDuration, 
            TimeSpan? workload, TemplateFileInfo? templateFileInfo, bool acceptsSignature, List<PlaceSignature> placeSignatures, Guid documentGroupId,
            bool usePreviousPeriod) : base(id)
        {
            Name = name;
            Description = description;
            CompanyId = companyId;
            DocumentValidityDuration = documentValidityDuration;
            Workload = workload;
            TemplateFileInfo = templateFileInfo;
            AcceptsSignature = acceptsSignature;
            SetPlaceSignatures(placeSignatures);
            DocumentGroupId = documentGroupId;
            UsePreviousPeriod = usePreviousPeriod;
            SyncPoliciesFromFields();
        }

        public static DocumentTemplate Create(Guid id, Name name, Description description, Guid companyId, TimeSpan? documentValidityDuration,
            TimeSpan? workload, TemplateFileInfo? templateFileInfo, bool acceptsSignature, List<PlaceSignature> placeSignatures, Guid documentGroupId,
            bool usePreviousPeriod = false)
            => new(id, name, description, companyId, documentValidityDuration, workload, templateFileInfo, acceptsSignature, placeSignatures, documentGroupId, usePreviousPeriod);
        public static DocumentTemplate Create(Guid id, Name name, Description description, Guid companyId, double? documentValidityDurationInDays,
            double? workloadInHours, TemplateFileInfo? templateFileInfo, bool acceptsSignature, List<PlaceSignature> placeSignatures, Guid documentGroupId,
            bool usePreviousPeriod = false)
        {
            TimeSpan? documentValidityDuration = documentValidityDurationInDays.HasValue ? TimeSpan.FromDays((double)documentValidityDurationInDays!) : null;
            TimeSpan? workload = workloadInHours.HasValue ? TimeSpan.FromHours((double)workloadInHours!) : null;
            return new(id, name, description, companyId, documentValidityDuration, workload, templateFileInfo, acceptsSignature, placeSignatures, documentGroupId, usePreviousPeriod);
        }
        public void Edit(Name name, Description description, double? documentValidityDurationInDays,
            double? workloadInHours, TemplateFileInfo? templateFileInfo, bool acceptsSignature, List<PlaceSignature> placeSignatures, Guid documentGroupId,
            bool usePreviousPeriod = false)
        {
            Name = name;
            Description = description;
            DocumentValidityDuration = documentValidityDurationInDays.HasValue ? TimeSpan.FromDays((double)documentValidityDurationInDays!) : null;
            Workload = workloadInHours.HasValue ? TimeSpan.FromHours((double)workloadInHours!) : null;
            TemplateFileInfo = templateFileInfo;
            AcceptsSignature = acceptsSignature;
            SetPlaceSignatures(placeSignatures);
            DocumentGroupId = documentGroupId;
            UsePreviousPeriod = usePreviousPeriod;
            SyncPoliciesFromFields();
        }

        private void SetPlaceSignatures(List<PlaceSignature> placeSignatures)
        {
            if (placeSignatures.Count > 0 && !AcceptsSignature)
            {
                throw new DomainException(this, DomainErrors.DocumentTemplate.TemplateDoesNotAcceptSignature(Id));
            }
            PlaceSignatures = placeSignatures;
        }

        /// <summary>
        /// Compõe a policy no template. Uma policy por <see cref="PolicyType"/>: adicionar de novo substitui a anterior.
        /// </summary>
        public void AddPolicy(IDocumentPolicy policy)
        {
            var record = DocumentPolicyFactory.ToPersistence(policy);
            _policies.RemoveAll(x => x.Type.Equals(record.Type));
            _policies.Add(record);
        }

        public void RemovePolicy(PolicyType type) => _policies.RemoveAll(x => x.Type.Equals(type));

        /// <summary>
        /// Obtém a policy pela capacidade pedida (ex.: IExpirationPolicy). Null = regra não se aplica a este template.
        /// </summary>
        public T? GetPolicy<T>() where T : class, IDocumentPolicy
            => _policies.Select(DocumentPolicyFactory.ToPolicy).OfType<T>().FirstOrDefault();

        public bool HasPolicy<T>() where T : class, IDocumentPolicy => GetPolicy<T>() is not null;

        /// <summary>
        /// Deriva as policies a partir dos campos escalares legados, que continuam sendo a fonte da verdade
        /// enquanto a Fase 2 não migra os consumidores. Ausência do campo = ausência da policy.
        /// PeriodPolicy fica de fora: hoje quem decide se o documento é por competência é o fluxo do evento,
        /// não o template — passar isso para o template é mudança de comportamento, reservada à Fase 3.
        /// </summary>
        private void SyncPoliciesFromFields()
        {
            _policies.Clear();

            if (DocumentValidityDuration.HasValue)
                AddPolicy(new ExpirationPolicy(DocumentValidityDuration.Value));

            if (Workload.HasValue)
                AddPolicy(new WorkloadPolicy(Workload.Value));
        }

        public bool IsSignable => AcceptsSignature;
        public bool CanGenerateDocuments => TemplateFileInfo?.IsValid ?? false;
    }
}
