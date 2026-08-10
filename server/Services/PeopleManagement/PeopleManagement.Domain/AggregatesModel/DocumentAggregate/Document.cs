using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Events;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using System.Reflection.Metadata.Ecma335;

namespace PeopleManagement.Domain.AggregatesModel.DocumentAggregate
{
    public class Document: Entity, IAggregateRoot
    {
        private DocumentStatus _documentStatus = DocumentStatus.OK;
        public Name Name { get; private set; }
        public Description Description { get; private set; }
        public Guid EmployeeId { get; private set; }
        public Guid CompanyId { get; private set; }
        public Guid RequiredDocumentId { get; private set; }
        public List<DocumentUnit> DocumentsUnits { get; private set; } = [];
        public DocumentStatus Status {
            get => _documentStatus;
            private set 
            { 
                if(_documentStatus != value)
                {
                    var oldValue = _documentStatus;
                    _documentStatus = value;
                    AddDomainEvent(new DocumentStatusChangedDomainEvent(this.Id, this.EmployeeId, this.CompanyId, oldValue, value));
                }
            } 
        }
        public Guid DocumentTemplateId { get; private set; }

        /// <summary>
        /// Quantas vezes este documento já venceu. É o contador de renovações consumido pela
        /// <c>IExpirationPolicy</c> do template: fato bruto do histórico, incrementado só por
        /// <see cref="ExpireDocumentUnit"/>.
        ///
        /// Existe como campo, e não como contagem de unidades por status, porque nenhum status serve de contador:
        /// a vencida vira Deprecated quando o substituto chega, e substituição por reenvio corrigido também
        /// deprecia — contar status faria correção de documento consumir renovação.
        /// </summary>
        public int ExpirationCount { get; private set; }

        private Document(Guid id, Guid employeeId, Guid companyId, Guid requiredDocumentId, Guid documentTemplateId, Name name, Description description) : base(id)
        {
            EmployeeId = employeeId;
            CompanyId = companyId;
            RequiredDocumentId = requiredDocumentId;
            DocumentTemplateId = documentTemplateId;
            Description = description;
            Name = name;
        }

        public static Document Create(Guid id, Guid employeeId, Guid companyId, Guid requiredDocumentId, Guid documentTemplateId, Name name, Description description)
            => new(id, employeeId, companyId, requiredDocumentId, documentTemplateId, name, description);

        /// <summary>
        /// Cria a próxima unidade do documento, reaproveitando a pendente da mesma competência quando já existir.
        ///
        /// A configuração de competência ([periodType]/[usePreviousPeriod]) vem do template do documento, lida
        /// pelo caller no momento da operação — o documento não guarda cópia: template é a configuração, a unit
        /// é a história. [referenceDate] é só a data usada para calcular em qual competência a unidade cai.
        /// </summary>
        public DocumentUnit NewDocumentUnit(Guid documentUnitId, PeriodType? periodType = null, bool usePreviousPeriod = false, DateTime? referenceDate = null)
        {
            if (periodType is not null)
            {
                var candidatePeriod = CandidatePeriodFor(periodType, usePreviousPeriod, referenceDate);

                var existingPending = DocumentsUnits.FirstOrDefault(x => x.Status == DocumentUnitStatus.Pending && x.Period != null && x.Period.Equals(candidatePeriod));

                // Candidata na competência mínima: qualquer pendente na mínima é uma unidade "esperando data",
                // reaproveitável mesmo que a granularidade do template tenha mudado desde que ela nasceu — sem
                // isso, trocar a granularidade deixaria a pendente antiga órfã e criaria uma segunda. Ela é
                // re-situada na mínima do tipo novo para não carregar a granularidade velha adiante.
                if (existingPending is null && candidatePeriod.Year == Period.MIN_YEAR)
                {
                    existingPending = DocumentsUnits.FirstOrDefault(x => x.Status == DocumentUnitStatus.Pending && x.Period != null && x.Period.Year == Period.MIN_YEAR);
                    existingPending?.ResetPeriodToMinimum(periodType);
                }

                if (existingPending is not null)
                    return existingPending;
            }
            else
            {
                var existingPending = DocumentsUnits.FirstOrDefault(x => x.Status == DocumentUnitStatus.Pending && x.Period == null);
                if (existingPending is not null)
                    return existingPending;
            }

            var documentUnit = DocumentUnit.Create(documentUnitId, this, periodType, usePreviousPeriod, referenceDate);
            DocumentsUnits.Add(documentUnit);
            RefreshDocumentStatus();
            return documentUnit;
        }

        // A competência que uma nova unidade teria, usada só para achar uma pendente equivalente antes de criar
        // outra. Precisa espelhar exatamente o que DocumentUnit.Create faz: sem data -> mínima; com data ->
        // corrente ou anterior conforme usePreviousPeriod.
        private static Period CandidatePeriodFor(PeriodType periodType, bool usePreviousPeriod, DateTime? referenceDate)
        {
            if (!referenceDate.HasValue)
                return Period.CreateMinimum(periodType);

            return usePreviousPeriod
                ? Period.CreatePreviousPeriod(periodType, referenceDate.Value)
                : Period.Create(periodType, referenceDate.Value);
        }

        public void InsertUnitWithRequireValidation(Guid documentUnitId, Name name, Extension extension)
        {
            var documentUnit = DocumentsUnits.FirstOrDefault(x => x.Id == documentUnitId)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentUnit), documentUnitId.ToString()));

            documentUnit.InsertWithRequireValidation(name, extension);

            RefreshDocumentStatus();
        }

        public string InsertUnitWithoutRequireValidation(Guid documentUnitId, Name name, Extension extension)
        {
            var documentUnit = DocumentsUnits.FirstOrDefault(x => x.Id == documentUnitId)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentUnit), documentUnitId.ToString()));

            documentUnit.InsertWithoutRequireValidation(name, extension);

            SupersedeUnitsCoveredBy(documentUnitId);
            return documentUnit.GetNameWithExtension;
        }

        public DocumentUnit UpdateDocumentUnitDetails(Guid documentUnitId, DateOnly date, DateOnly? validity, string content,
            PeriodType? periodType = null, bool usePreviousPeriod = false)
        {
            var documentUnit = DocumentsUnits.FirstOrDefault(x => x.Id == documentUnitId)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentUnit), documentUnitId.ToString()));

            documentUnit.UpdateDetails(date, validity, content, periodType, usePreviousPeriod);

            if (documentUnit.IsPeriod)
                VerifyDuplicatedPendings(documentUnit);

            RefreshDocumentStatus();
            return documentUnit;
        }

        public DocumentUnit UpdateDocumentUnitDetails(Guid documentUnitId, DateOnly date, TimeSpan? validity, string content,
            PeriodType? periodType = null, bool usePreviousPeriod = false)
        {
            var documentUnit = DocumentsUnits.FirstOrDefault(x => x.Id == documentUnitId)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentUnit), documentUnitId.ToString()));

            documentUnit.UpdateDetails(date, validity, content, periodType, usePreviousPeriod);

            if (documentUnit.IsPeriod)
                VerifyDuplicatedPendings(documentUnit);

            RefreshDocumentStatus();
            return documentUnit;
        }

        private void VerifyDuplicatedPendings(DocumentUnit documentUnit)
        {
            var duplicatePending = DocumentsUnits
                .Where(x => x.Id != documentUnit.Id && x.Status == DocumentUnitStatus.Pending && x.Period != null && x.Period.Equals(documentUnit.Period))
                .ToList();

            duplicatePending.ForEach(x => x.MarkAsInvalid());
        }



        public DocumentUnit GetDocumentUnit(Guid documentUnitId)
        {
            var documentUnit = DocumentsUnits.FirstOrDefault(x => x.Id == documentUnitId)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentUnit), documentUnitId.ToString()));

            return documentUnit;
        }
        public void MarkAsAwaitingDocumentUnitSignature(Guid documentUnitId)
        {
            
            var documentUnit = DocumentsUnits.FirstOrDefault(x => x.Id == documentUnitId)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentUnit), documentUnitId.ToString()));

            documentUnit.MarkAsAwaitingSignature();
            RefreshDocumentStatus();
        }

        /// <summary>
        /// Agenda o envio da unidade para assinatura numa data futura, em vez de mandá-la agora.
        ///
        /// As mesmas guardas do envio imediato valem aqui, e valem de novo no disparo: entre o agendamento e a
        /// data escolhida o documento pode ter sido entregue, invalidado ou enviado por outro caminho.
        /// </summary>
        public void ScheduleSignatureSend(Guid documentUnitId, DateOnly sendOn, DateOnly dateLimitToSign, int reminderEveryNDays)
        {
            var documentUnit = DocumentsUnits.FirstOrDefault(x => x.Id == documentUnitId)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentUnit), documentUnitId.ToString()));

            if (documentUnit.IsAwaitingSignature)
                throw new DomainException(this, DomainErrors.Document.DocumentAlreadySentToSignature(documentUnitId));

            if (documentUnit.IsPending == false)
                throw new DomainException(this, DomainErrors.Document.IsNotPending());

            documentUnit.ScheduleSignatureSend(ScheduledSignature.Create(sendOn, dateLimitToSign, reminderEveryNDays));
        }

        /// <summary>
        /// Cancela o envio agendado da unidade. Sem agendamento, não faz nada.
        /// </summary>
        public void CancelScheduledSignatureSend(Guid documentUnitId)
        {
            var documentUnit = DocumentsUnits.FirstOrDefault(x => x.Id == documentUnitId)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentUnit), documentUnitId.ToString()));

            documentUnit.CancelScheduledSignatureSend();
        }

        /// <summary>
        /// A unidade tem erro, foi entregue por engano, ou a dispensa que ela registrava deixou de valer. Diferente
        /// de depreciar: aqui o documento não vale nada, então não fica valendo como comprovação de período nenhum.
        ///
        /// É o mesmo caminho da recusa de validação — daí aceitar também <c>RequiresValidation</c>, que a regra
        /// de tela da invalidação direta (pendente, OK ou não aplicável) não contempla.
        ///
        /// Invalidar uma unidade <c>NotApplicable</c> é como o documento volta a ser exigido: a competência perde a
        /// cobertura aqui, e quem chama pelo <c>DocumentService</c> ganha a pendente substituta no lugar.
        /// </summary>
        public void MarkAsInvalidDocumentUnit(Guid documentUnitId)
        {
            var documentUnit = DocumentsUnits.FirstOrDefault(x => x.Id == documentUnitId)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentUnit), documentUnitId.ToString()));

            if (documentUnit.MarkAsInvalid() == false)
                throw new DomainException(this,
                    DomainErrors.Document.CannotInvalidateDocumentUnit(documentUnitId, documentUnit.Status.Name));

            RefreshDocumentStatus();
        }

        /// <summary>
        /// Depreciação manual: o documento continua válido como prova, mas não é mais o vigente.
        ///
        /// Só a partir de vigência — depreciar uma pendente não faz sentido (não há o que comprovar) e depreciar
        /// uma inválida apagaria o registro de que houve erro.
        /// </summary>
        public void DeprecateDocumentUnit(Guid documentUnitId)
        {
            var documentUnit = DocumentsUnits.FirstOrDefault(x => x.Id == documentUnitId)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentUnit), documentUnitId.ToString()));

            if (documentUnit.MarkAsDeprecated() == false)
                throw new DomainException(this,
                    DomainErrors.Document.CannotDeprecateDocumentUnit(documentUnitId, documentUnit.Status.Name));

            RefreshDocumentStatus();
        }

        public void MarkAsValidDocumentUnit(Guid documentUnitId)
        {
            var documentUnit = DocumentsUnits.FirstOrDefault(x => x.Id == documentUnitId)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentUnit), documentUnitId.ToString()));

            documentUnit.MarkAsValid();

            SupersedeUnitsCoveredBy(documentUnitId);
        }

        public bool MarkAsNotApplicableDocumentUnit(Guid documentUnitId)
        {
            var documentUnit = DocumentsUnits.FirstOrDefault(x => x.Id == documentUnitId)
               ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentUnit), documentUnitId.ToString()));

            var isNotApplicable = documentUnit.MarkAsNotApplicable();
            if (isNotApplicable)
            {
                SupersedeUnitsCoveredBy(documentUnitId);
                return true;
            }
            return false;
        }

        /// <summary>
        /// A unidade venceu: a exigência fica descoberta até chegar substituto. Incrementa
        /// <see cref="ExpirationCount"/>, que é o contador de renovações lido pela política de vencimento do
        /// template.
        /// </summary>
        public bool ExpireDocumentUnit(Guid documentUnitId)
        {
            var documentUnit = DocumentsUnits.FirstOrDefault(x => x.Id == documentUnitId)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentUnit), documentUnitId.ToString()));

            var expired = documentUnit.MarkAsExpired();

            if (expired)
                ExpirationCount++;

            RefreshDocumentStatus();
            return expired;
        }

        public bool MakeAsWarning(Guid documentUnitIdExpire)
        {
            var documentUnit = DocumentsUnits.FirstOrDefault(x => x.Id == documentUnitIdExpire)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentUnit), documentUnitIdExpire.ToString()));

            var isMarkAsWarning = documentUnit.MarkAsWarning();

            // Deriva como todo o resto em vez de atribuir Warning direto: com várias competências, uma delas
            // entrando em aviso não pode apagar outra descoberta.
            RefreshDocumentStatus();
            return isMarkAsWarning;
        }


        /// <summary>
        /// O documento inteiro saiu de cena — deixou de ser exigido deste funcionário. Toda unidade é superada,
        /// inclusive as que estavam em curso, sem exceção por competência.
        /// </summary>
        public void MakeAsDeprecated()
        {
            DocumentsUnits.ForEach(x => x.Supersede());
            RefreshDocumentStatus();
        }

        /// <summary>
        /// Deprecia as unidades já entregues do documento e devolve quantas foram. Usado quando um novo contrato
        /// de trabalho começa e o template manda depreciar o que veio do contrato anterior.
        ///
        /// Entregue = <see cref="DocumentUnitStatus.OK"/>, <see cref="DocumentUnitStatus.Warning"/> ou
        /// <see cref="DocumentUnitStatus.Expired"/>. Warning entra porque é uma unidade OK que está vencendo:
        /// deixá-la viva atravessaria um documento do contrato anterior para o novo, e ao vencer ela seria
        /// renovada já sob o vínculo errado. Expired entra pelo mesmo motivo — é entrega do contrato anterior, e
        /// o novo contrato gera as suas próprias pendências, então ela deixa de ser uma cobrança em aberto.
        ///
        /// O que está em curso fica: pendente / aguardando assinatura / requer validação não são entrega de
        /// contrato nenhum. Depreciado / inválido / não aplicável já saíram de cena. Diferente de
        /// <see cref="MakeAsDeprecated"/>, que supera tudo indiscriminadamente.
        ///
        /// Exige a coleção de unidades carregada por inteiro: o RefreshDocumentStatus varre DocumentsUnits para
        /// recalcular o status do documento, e com uma coleção parcial ele mentiria.
        /// </summary>
        public int DeprecateDeliveredUnits()
        {
            var deliveredUnits = DocumentsUnits
                .Where(x => x.Status == DocumentUnitStatus.OK ||
                    x.Status == DocumentUnitStatus.Warning ||
                    x.Status == DocumentUnitStatus.Expired)
                .ToList();

            if (deliveredUnits.Count == 0)
                return 0;

            deliveredUnits.ForEach(x => x.MarkAsDeprecated());
            RefreshDocumentStatus();

            return deliveredUnits.Count;
        }

        /// <summary>
        /// Uma unidade passou a cobrir a exigência: as outras que cobriam a mesma coisa saem de cena.
        ///
        /// O alcance é a competência da unidade que resolveu — competências são obrigações independentes, e
        /// entregar a de março não resolve a de abril. Unidades sem competência num documento periodizado são
        /// legado e não pertencem a competência nenhuma, então qualquer entrega as supera.
        ///
        /// Fora desse alcance há uma exceção deliberada: TODA unidade vencida é depreciada, de qualquer
        /// competência. Uma vencida está esperando substituto, e a renovação de um documento por competência cai
        /// na competência SEGUINTE — exigir competência igual a deixaria esperando um substituto que, por
        /// construção, nunca chega na dela.
        /// </summary>
        private void SupersedeUnitsCoveredBy(Guid resolvingUnitId)
        {
            var resolving = DocumentsUnits.FirstOrDefault(x => x.Id == resolvingUnitId);

            if (resolving is null)
                return;

            var superseded = DocumentsUnits
                .Where(x => x.Id != resolvingUnitId)
                .Where(x => x.IsExpired
                    || (resolving.Period is null
                        ? x.Period is null
                        : x.Period is null || x.Period.Equals(resolving.Period)))
                .ToList();

            superseded.ForEach(x => x.Supersede());

            RefreshDocumentStatus();
        }

        

        public bool IsAwaitingSignatureDocumentUnit(Guid documentUnitId)
        {
            var document = DocumentsUnits.FirstOrDefault(x => x.Id == documentUnitId)
               ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentUnit), documentUnitId.ToString()));

            return document.IsAwaitingSignature;
        }

        public bool CanEditDocumentUnit(Guid documentUnitId)
        {
            var document = DocumentsUnits.FirstOrDefault(x => x.Id == documentUnitId)
               ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentUnit), documentUnitId.ToString()));

            return document.CanEdit;
        }

        public bool IsPendingDocumentUnit(Guid documentUnitId)
        {
            var document = DocumentsUnits.FirstOrDefault(x => x.Id == documentUnitId)
               ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentUnit), documentUnitId.ToString()));

            return document.IsPending;
        }

        public bool CanBeDeleted()
        {
            if(DocumentsUnits.Count == 0)
                return true;    
            return DocumentsUnits.Any(x => x.IsPending == false) == false;
        }

        
        /// <summary>
        /// Recalcula o status do documento a partir das unidades. É o ÚNICO lugar que escreve
        /// <see cref="Status"/>: cada competência vira um status pelo <see cref="GetStatusFromGroup"/>, e o
        /// documento assume a pior delas por <see cref="DocumentStatus.Severity"/>.
        ///
        /// Documento sem competência é tratado como uma competência só — mesmo caminho, mesma ordem. A versão
        /// anterior tinha dois caminhos com precedências diferentes, e o ramo por competência descartava
        /// Deprecated e AwaitingSignature: um documento periodizado com tudo vencido reportava OK.
        /// </summary>
        private void RefreshDocumentStatus()
        {
            if (DocumentsUnits.Count == 0)
            {
                Status = DocumentStatus.OK;
                return;
            }

            var statusPerPeriod = DocumentsUnits
                .GroupBy(x => x.Period)
                .Select(GetStatusFromGroup);

            Status = statusPerPeriod.MinBy(DocumentStatus.Severity) ?? DocumentStatus.OK;
        }

        /// <summary>
        /// O status de UMA competência. A melhor unidade define: basta uma cobrindo para a competência estar
        /// resolvida, e só quando nenhuma cobre é que a pior notícia aparece.
        /// </summary>
        private static DocumentStatus GetStatusFromGroup(IEnumerable<DocumentUnit> units)
        {
            if (!units.Any())
                return DocumentStatus.OK;

            // NotApplicable cobre tanto quanto OK — é exceção deliberada à regra, não falta.
            if (units.Any(x => x.IsOK || x.IsNotApplicable))
                return DocumentStatus.OK;

            if (units.Any(x => x.Status == DocumentUnitStatus.Warning))
                return DocumentStatus.Warning;

            if (units.Any(x => x.Status == DocumentUnitStatus.RequiresValidation))
                return DocumentStatus.RequiresValidation;

            if (units.Any(x => x.Status == DocumentUnitStatus.AwaitingSignature))
                return DocumentStatus.AwaitingSignature;

            // Vencida sem nada melhor na competência: houve cobertura e ela caducou.
            if (units.Any(x => x.IsExpired))
                return DocumentStatus.Expired;

            if (units.Any(x => x.IsPending))
                return DocumentStatus.RequiresDocument;

            // Só sobraram unidades depreciadas e/ou inválidas. Depreciada sem substituto na competência é o
            // documento inteiro fora de cena (MakeAsDeprecated); inválida não cobre nada.
            if (units.All(x => x.Status == DocumentUnitStatus.Deprecated))
                return DocumentStatus.Deprecated;

            return DocumentStatus.RequiresDocument;
        }



    }
}
