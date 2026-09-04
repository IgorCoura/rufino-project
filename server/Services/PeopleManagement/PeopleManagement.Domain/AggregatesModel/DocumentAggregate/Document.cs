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
        /// Quantos ciclos de validade este documento já gastou. É o contador lido pela <c>IExpirationPolicy</c>
        /// do template (<c>HasValidityCycleLeft</c>) para decidir se a próxima unidade ainda nasce com validade,
        /// incrementado só por <see cref="RegisterRenewal"/>.
        ///
        /// Conta renovações EMITIDAS que abriram um ciclo novo, não vencimentos. Já contou vencimentos (chamava-se
        /// <c>ExpirationCount</c>), e funcionava enquanto renovar era consequência automática de vencer. Com a
        /// renovação virando decisão explícita do RH, contar vencimento errava dos dois lados: renovar antes do
        /// prazo — que é o caminho bom — nunca vencia a unidade e portanto nunca consumia cota, deixando um teto de
        /// N ciclos valer para sempre; e um documento vencido e abandonado queimava a cota inteira sem nenhuma
        /// renovação ter existido.
        ///
        /// Renovar com a cota esgotada NÃO incrementa: a unidade criada ali nasce sem validade, então não abre
        /// ciclo nenhum. É o que mantém o contador significando "ciclos gastos" — e o que faz aumentar o teto no
        /// template devolver ciclos de verdade, em vez de esbarrar num número inflado por renovações que não
        /// venceram nada.
        ///
        /// Existe como campo, e não como contagem de unidades por status, porque nenhum status serve de contador:
        /// a vencida vira Deprecated quando o substituto chega, e substituição por reenvio corrigido também
        /// deprecia — contar status faria correção de documento consumir renovação.
        /// </summary>
        public int RenewalCount { get; private set; }

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

        /// <summary>
        /// Cria a unidade de UMA competência específica, pedida à mão pelo RH — o caso do documento por
        /// competência cuja unidade de um período nunca chegou a ser criada.
        ///
        /// Diferente de <see cref="NewDocumentUnit"/>, que reaproveita a pendente equivalente em silêncio: aqui
        /// competência ocupada é recusa, não devolução da unidade existente. Quem pede a criação manual quer uma
        /// unidade A MAIS; se já há quem responda por aquele período — cobrindo, em curso ou vencida — a saída é
        /// depreciar ou invalidar a existente antes. Só inválida e depreciada desocupam
        /// (<see cref="DocumentUnit.OccupiesPeriod"/>).
        ///
        /// [referenceDate] é a data do documento: situa a unidade na competência e vira o <c>Date</c> dela.
        /// </summary>
        public DocumentUnit NewDocumentUnitForPeriod(Guid documentUnitId, PeriodType periodType, bool usePreviousPeriod,
            DateTime referenceDate)
        {
            var candidatePeriod = CandidatePeriodFor(periodType, usePreviousPeriod, referenceDate);

            var occupyingUnit = DocumentsUnits.FirstOrDefault(x => x.OccupiesPeriod && x.Period != null
                && x.Period.Equals(candidatePeriod));

            if (occupyingUnit is not null)
                throw new DomainException(this, DomainErrors.Document.PeriodAlreadyHasDocumentUnit(
                    candidatePeriod.Describe(), occupyingUnit.Status.Name));

            return NewDocumentUnit(documentUnitId, periodType, usePreviousPeriod, referenceDate);
        }

        /// <summary>
        /// Cria a substituta de [replacedUnitId] — a unidade que renova a que está saindo de vigência.
        ///
        /// É uma <see cref="NewDocumentUnit"/> com o vínculo carimbado, e por isso herda o reaproveitamento da
        /// pendente equivalente: pedir renovação duas vezes devolve a mesma unidade, com o mesmo vínculo.
        ///
        /// Quem decide SE pode renovar é o serviço, que lê a política de vencimento do template — regra de dois
        /// aggregates. Aqui só se registra o fato.
        /// </summary>
        public DocumentUnit NewReplacementUnit(Guid documentUnitId, Guid replacedUnitId, PeriodType? periodType = null,
            bool usePreviousPeriod = false)
        {
            var replacedUnit = DocumentsUnits.FirstOrDefault(x => x.Id == replacedUnitId)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentUnit), replacedUnitId.ToString()));

            if (replacedUnit.CanBeRenewed == false)
                throw new DomainException(this,
                    DomainErrors.Document.CannotRenewDocumentUnit(replacedUnitId, replacedUnit.Status.Name));

            var replacement = NewDocumentUnit(documentUnitId, periodType, usePreviousPeriod);
            replacement.SetReplacementOf(replacedUnitId);

            // De novo depois do vínculo: uma substituta em voo não conta como falta enquanto a substituída
            // ainda cobre, e é o vínculo que informa isso ao RefreshDocumentStatus.
            RefreshDocumentStatus();
            return replacement;
        }

        /// <summary>
        /// A substituta de [documentUnitId] que ainda está de pé — em curso ou já cobrindo —, ou null quando a
        /// renovação nunca foi pedida.
        ///
        /// Uma substituta descartada (inválida) não conta: a renovação pode ser pedida de novo. É o que torna
        /// pedir renovação idempotente sem travar o RH depois de um engano.
        /// </summary>
        public DocumentUnit? LiveReplacementFor(Guid documentUnitId)
            => DocumentsUnits.FirstOrDefault(x => x.ReplacesDocumentUnitId == documentUnitId &&
                (x.IsInFlight || x.CoversRequirement));

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
        /// A unidade venceu: a exigência fica descoberta até chegar substituto.
        ///
        /// Não mexe no <see cref="RenewalCount"/> — vencer não é renovar. Quem consome cota é
        /// <see cref="RegisterRenewal"/>, chamado quando a substituta é efetivamente emitida.
        /// </summary>
        public bool ExpireDocumentUnit(Guid documentUnitId)
        {
            var documentUnit = DocumentsUnits.FirstOrDefault(x => x.Id == documentUnitId)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentUnit), documentUnitId.ToString()));

            var expired = documentUnit.MarkAsExpired();

            RefreshDocumentStatus();
            return expired;
        }

        /// <summary>
        /// Registra que um ciclo de validade foi aberto. Chamado uma única vez por substituta efetivamente
        /// emitida — pedir a renovação de novo devolve a mesma unidade e não conta de novo.
        ///
        /// Quem decide SE este ciclo existe é o serviço, que lê o teto do template (regra de dois aggregates):
        /// com a cota esgotada a substituta nasce sem validade e não passa por aqui. Depreciar e invalidar também
        /// deixam uma pendente no lugar e também não passam: são correção de um documento errado, não a troca de
        /// um ciclo de validade por outro.
        /// </summary>
        public void RegisterRenewal()
        {
            RenewalCount++;
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
        /// Fora desse alcance há duas exceções deliberadas:
        ///
        /// TODA unidade vencida é depreciada, de qualquer competência. Uma vencida está esperando substituto, e a
        /// renovação de um documento por competência cai na competência SEGUINTE — exigir competência igual a
        /// deixaria esperando um substituto que, por construção, nunca chega na dela.
        ///
        /// E a unidade que esta veio substituir (<see cref="DocumentUnit.ReplacesDocumentUnitId"/>) sai de cena
        /// também, de qualquer competência. É o caso da renovação entregue ANTES do vencimento: a substituída
        /// ainda está OK ou A Vencer, então nem a competência nem a regra da vencida a alcançariam, e ela ficaria
        /// viva até vencer sozinha — deixando a substituta pendente e o documento cobrando duas vezes a mesma
        /// coisa. Ela vira histórico, não engano: no dia da entrega ela ainda tinha valor.
        /// </summary>
        private void SupersedeUnitsCoveredBy(Guid resolvingUnitId)
        {
            var resolving = DocumentsUnits.FirstOrDefault(x => x.Id == resolvingUnitId);

            if (resolving is null)
                return;

            var superseded = DocumentsUnits
                .Where(x => x.Id != resolvingUnitId)
                .Where(x => x.Id == resolving.ReplacesDocumentUnitId
                    || x.IsExpired
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
        ///
        /// A renovação em voo não entra na conta (ver <see cref="IsRenewalInFlight"/>).
        /// </summary>
        private void RefreshDocumentStatus()
        {
            if (DocumentsUnits.Count == 0)
            {
                Status = DocumentStatus.OK;
                return;
            }

            var statusPerPeriod = DocumentsUnits
                .Where(x => IsRenewalInFlight(x) == false)
                .GroupBy(x => x.Period)
                .Select(GetStatusFromGroup);

            Status = statusPerPeriod.MinBy(DocumentStatus.Severity) ?? DocumentStatus.OK;
        }

        /// <summary>
        /// A unidade é a renovação de uma cobertura que ainda vale — pedida, ainda não entregue, e a substituída
        /// continua cobrindo a exigência.
        ///
        /// Ela é ignorada no status do documento porque não é falta: o que ela representa já está coberto agora, e
        /// o que ela promete é a cobertura do próximo ciclo. Sem isso, pedir a renovação de um documento A Vencer
        /// piorava o status dele — a substituta nasce em outra competência (a mínima, esperando data), então virava
        /// uma competência descoberta e o documento passava de "A Vencer" para "Falta Entregar". Pedir a renovação
        /// no prazo não pode deixar o documento pior do que ignorá-la.
        ///
        /// Quando a substituída vence, ela deixa de cobrir e a substituta volta a contar — daí o documento fica
        /// "Vencido" até a nova ser entregue, e não antes.
        /// </summary>
        private bool IsRenewalInFlight(DocumentUnit unit)
            => unit.IsReplacement && unit.IsInFlight &&
                DocumentsUnits.Any(x => x.Id == unit.ReplacesDocumentUnitId && x.CoversRequirement);

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
