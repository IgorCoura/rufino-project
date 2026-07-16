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
        public Guid DocumentGroupId { get; private set; }
        public bool UsePreviousPeriod { get; private set; }

        /// <summary>
        /// Conjunto de regras que o template compõe. Presença de uma policy = regra ativa (Composite).
        /// Quem manda são as policies quando o caller as informa; se não informar, elas são derivadas dos
        /// campos escalares legados (caminho retrocompatível).
        /// </summary>
        public IReadOnlyCollection<DocumentPolicy> Policies => _policies.AsReadOnly();

        /// <summary>
        /// Se documentos gerados por este template podem ser assinados. Derivado da presença da SignaturePolicy.
        /// </summary>
        public bool AcceptsSignature => HasPolicy<ISignaturePolicy>();

        /// <summary>
        /// Onde as assinaturas entram na página. Vazio quando o template não aceita assinatura — a lista mora
        /// dentro da SignaturePolicy, então local sem aceite é inexprimível no estado persistido.
        /// </summary>
        public IReadOnlyList<PlaceSignature> PlaceSignatures =>
            GetPolicy<ISignaturePolicy>()?.PlaceSignatures ?? [];

        private DocumentTemplate() { }
        private DocumentTemplate(Guid id, Name name, Description description, Guid companyId, TimeSpan? documentValidityDuration,
            TimeSpan? workload, TemplateFileInfo? templateFileInfo, bool acceptsSignature, List<PlaceSignature> placeSignatures, Guid documentGroupId,
            bool usePreviousPeriod, IEnumerable<IDocumentPolicy>? policies) : base(id)
        {
            Name = name;
            Description = description;
            CompanyId = companyId;
            DocumentValidityDuration = documentValidityDuration;
            Workload = workload;
            TemplateFileInfo = templateFileInfo;
            DocumentGroupId = documentGroupId;
            UsePreviousPeriod = usePreviousPeriod;
            ApplyPolicies(policies, acceptsSignature, placeSignatures);
        }

        public static DocumentTemplate Create(Guid id, Name name, Description description, Guid companyId, TimeSpan? documentValidityDuration,
            TimeSpan? workload, TemplateFileInfo? templateFileInfo, bool acceptsSignature, List<PlaceSignature> placeSignatures, Guid documentGroupId,
            bool usePreviousPeriod = false, IEnumerable<IDocumentPolicy>? policies = null)
            => new(id, name, description, companyId, documentValidityDuration, workload, templateFileInfo, acceptsSignature, placeSignatures, documentGroupId, usePreviousPeriod, policies);
        public static DocumentTemplate Create(Guid id, Name name, Description description, Guid companyId, double? documentValidityDurationInDays,
            double? workloadInHours, TemplateFileInfo? templateFileInfo, bool acceptsSignature, List<PlaceSignature> placeSignatures, Guid documentGroupId,
            bool usePreviousPeriod = false, IEnumerable<IDocumentPolicy>? policies = null)
        {
            TimeSpan? documentValidityDuration = documentValidityDurationInDays.HasValue ? TimeSpan.FromDays((double)documentValidityDurationInDays!) : null;
            TimeSpan? workload = workloadInHours.HasValue ? TimeSpan.FromHours((double)workloadInHours!) : null;
            return new(id, name, description, companyId, documentValidityDuration, workload, templateFileInfo, acceptsSignature, placeSignatures, documentGroupId, usePreviousPeriod, policies);
        }
        public void Edit(Name name, Description description, double? documentValidityDurationInDays,
            double? workloadInHours, TemplateFileInfo? templateFileInfo, bool acceptsSignature, List<PlaceSignature> placeSignatures, Guid documentGroupId,
            bool usePreviousPeriod = false, IEnumerable<IDocumentPolicy>? policies = null)
        {
            Name = name;
            Description = description;
            DocumentValidityDuration = documentValidityDurationInDays.HasValue ? TimeSpan.FromDays((double)documentValidityDurationInDays!) : null;
            Workload = workloadInHours.HasValue ? TimeSpan.FromHours((double)workloadInHours!) : null;
            TemplateFileInfo = templateFileInfo;
            DocumentGroupId = documentGroupId;
            UsePreviousPeriod = usePreviousPeriod;
            ApplyPolicies(policies, acceptsSignature, placeSignatures);
        }

        /// <summary>
        /// Compõe (ou remove) a SignaturePolicy a partir do par aceite + locais.
        ///
        /// A contradição "tem local mas não aceita" não existe no modelo persistido — os locais moram dentro da
        /// policy. Mas o caller ainda a informa como dois parâmetros soltos, então a checagem sobrevive aqui,
        /// na fronteira: é o último ponto onde os dois podem discordar.
        /// </summary>
        private void SetSignature(bool acceptsSignature, List<PlaceSignature> placeSignatures)
        {
            if (placeSignatures.Count > 0 && !acceptsSignature)
            {
                throw new DomainException(this, DomainErrors.DocumentTemplate.TemplateDoesNotAcceptSignature(Id));
            }

            RemovePolicy(PolicyType.Signature);

            if (acceptsSignature)
                AddPolicy(new SignaturePolicy(placeSignatures));
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
        /// Define o conjunto de regras do template. Caller que não informa policies mantém o caminho legado
        /// (derivar dos campos escalares); caller que informa passa a mandar, e os escalares viram projeção.
        /// Conjunto vazio informado explicitamente = template sem regra alguma.
        ///
        /// A assinatura é a exceção: vem sempre dos parâmetros, nos dois caminhos. O contrato da API a informa
        /// separadamente (acceptsSignature + placeSignatures), e não dentro do bloco de policies — deixá-la
        /// depender do conjunto informado apagaria a assinatura em todo Edit que mandasse só as outras regras.
        /// </summary>
        private void ApplyPolicies(IEnumerable<IDocumentPolicy>? policies, bool acceptsSignature, List<PlaceSignature> placeSignatures)
        {
            if (policies is null)
            {
                SyncPoliciesFromFields();
            }
            else
            {
                _policies.Clear();

                foreach (var policy in policies)
                    AddPolicy(policy);

                SyncFieldsFromPolicies();
            }

            SetSignature(acceptsSignature, placeSignatures);
        }

        /// <summary>
        /// Deriva as policies a partir dos campos escalares legados. Ausência do campo = ausência da policy.
        /// PeriodPolicy fica de fora: hoje quem decide se o documento é por competência é o fluxo do evento,
        /// não o template — passar isso para o template é mudança de comportamento, reservada à Fase 3.
        /// </summary>
        private void SyncPoliciesFromFields()
        {
            _policies.Clear();

            if (DocumentValidityDuration.HasValue && DocumentValidityDuration > TimeSpan.Zero)
                AddPolicy(new ExpirationPolicy(DocumentValidityDuration.Value));

            if (Workload.HasValue && Workload > TimeSpan.Zero)
                AddPolicy(new WorkloadPolicy(Workload.Value));
        }

        /// <summary>
        /// Espelha as policies de volta nos campos escalares legados. As colunas foram mantidas (e depreciadas),
        /// e o read model ainda lê delas — sem este espelho, configurar por policy deixaria as queries mentindo.
        /// </summary>
        private void SyncFieldsFromPolicies()
        {
            DocumentValidityDuration = GetPolicy<IExpirationPolicy>()?.Duration;
            Workload = GetPolicy<IWorkloadPolicy>()?.Workload;

            // UsePreviousPeriod é a competência: quando há PeriodPolicy, ela manda; sem policy, o escalar fica
            // como veio (caminho legado). O escalar segue depreciado, mas o read model ainda o lê.
            var period = GetPolicy<IPeriodPolicy>();
            if (period is not null)
                UsePreviousPeriod = period.UsePreviousPeriod;
        }

        public bool IsSignable => AcceptsSignature;
        public bool CanGenerateDocuments => TemplateFileInfo?.IsValid ?? false;
    }
}
