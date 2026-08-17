using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Events;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;

namespace PeopleManagement.Domain.AggregatesModel.DocumentAggregate
{
    public class DocumentUnit : Entity
    {
        private DateOnly? _validity;
        public string Content { get; private set; } = string.Empty;
        public DateOnly? Validity 
        { 
            get => _validity;
            private set
            {
                
                if (value != null)
                {
                    DateOnly cValue = (DateOnly)value;
                    if(cValue.ToDateTime(TimeOnly.MinValue) < DateTime.UtcNow)
                        throw new DomainException(this, DomainErrors.DataIsGreaterThanMax(nameof(Validity), 
                            (DateOnly)value, DateOnly.FromDateTime(DateTime.UtcNow)));
                }
                _validity = value;               
            }
        }

        public Name? Name { get; private set; } = null!;
        public Extension? Extension { get; private set; } = null!;
        public Period? Period { get; private set; }
        public DocumentUnitStatus Status { get; private set; } = DocumentUnitStatus.Pending;
        public DateOnly Date { get; private set; }
        public Guid DocumentId { get; private set; }
        public Document Document { get; private set; } = null!;
        public string? SignatureDocumentToken { get; private set; }
        public string? SignatureUrl { get; private set; }
        public string? AttachmentToken { get; private set; }
        public DateTime? SentToSignatureAt { get; private set; }
        public DateOnly? WorkloadEndDate { get; private set; }

        /// <summary>
        /// Envio para assinatura agendado, quando houver. Null = nada agendado; o envio imediato não passa por
        /// aqui. Fica ao lado de <see cref="SentToSignatureAt"/> de propósito: um é a intenção, o outro é o fato.
        /// </summary>
        public ScheduledSignature? ScheduledSignature { get; private set; }

        /// <summary>
        /// A unidade que esta nasceu para substituir, quando ela veio de uma renovação. Null = unidade comum.
        ///
        /// A relação sempre existiu de fato — a renovada substitui a que está vencendo —, mas era implícita e
        /// irrecuperável, e por isso três perguntas não tinham resposta: quem depreciar quando a substituta é
        /// entregue (ela e a substituída podem estar em competências diferentes), se uma renovação já foi pedida
        /// (para não pedir duas), e se uma pendente é cobrança de verdade ou apenas a renovação em voo de uma
        /// cobertura que ainda vale.
        /// </summary>
        public Guid? ReplacesDocumentUnitId { get; private set; }

        private DocumentUnit() { }
        private DocumentUnit(Guid id, Document document) : base(id)
        {
            Document = document;
            DocumentId = document.Id;
        }

        /// <summary>
        /// Cria a unidade situando-a na competência configurada no template do documento, quando houver.
        ///
        /// A configuração ([periodType]/[usePreviousPeriod]) vem do template, lida pelo caller no momento da
        /// operação — nem o documento nem a unidade guardam cópia da regra; a unidade guarda só a competência em
        /// que caiu (a história). Com [referenceDate], a unidade cai na competência daquela data (e a data vira o
        /// Date da unidade). Sem [referenceDate], a unidade ainda não tem data que a situe, então recebe a
        /// competência mínima possível — substituída assim que uma data real chega por UpdateDetails.
        /// </summary>
        public static DocumentUnit Create(Guid id, Document document, PeriodType? periodType = null, bool usePreviousPeriod = false, DateTime? referenceDate = null)
        {
            var documentUnit = new DocumentUnit(id, document);

            if (periodType is not null)
            {
                if (referenceDate.HasValue)
                {
                    documentUnit.UpdateDetails(DateOnly.FromDateTime(referenceDate.Value), (DateOnly?)null, "", periodType, usePreviousPeriod);
                }
                else
                {
                    documentUnit.Period = Period.CreateMinimum(periodType);
                }
            }

            return documentUnit;
        }

        /// <summary>
        /// Recalcula a competência da unidade a partir de [referenceDate], com a configuração atual do template
        /// (lida pelo caller no momento da operação).
        /// </summary>
        public void SetPeriod(DateTime referenceDate, PeriodType periodType, bool usePreviousPeriod)
        {
            Period = usePreviousPeriod
                ? Period.CreatePreviousPeriod(periodType, referenceDate)
                : Period.Create(periodType, referenceDate);
        }

        /// <summary>
        /// Re-situa a unidade na competência mínima de [periodType]. Usado quando uma pendente que espera data é
        /// reaproveitada após o template trocar de granularidade — a mínima antiga não pode sobreviver com o tipo
        /// velho, senão a próxima busca por pendente equivalente não a encontraria.
        /// </summary>
        public void ResetPeriodToMinimum(PeriodType periodType)
        {
            Period = Period.CreateMinimum(periodType);
        }

        public void InsertWithRequireValidation(Name name, Extension extension)
        {
            if (HasInvalidDateOrValidity)
                throw new DomainException(this, DomainErrors.DataInvalid(nameof(Date), Date));
            Name = name;
            Extension = extension;
            Status = DocumentUnitStatus.RequiresValidation;
        }
        public void InsertWithoutRequireValidation(Name name, Extension extension)
        {
            if (HasInvalidDateOrValidity)
                throw new DomainException(this, DomainErrors.DataInvalid(nameof(Date), Date));
            Name = name;
            Extension = extension;
            Status = DocumentUnitStatus.OK;
            if(Validity is not null)
                AddDomainEvent(ScheduleDocumentExpirationEvent.Create(Document.Id, Id, Document.CompanyId, (DateOnly)Validity, Date));
        }

        // Com [periodType], a data recebida re-situa a unidade na competência correspondente — inclusive quando a
        // unidade ainda não tinha competência (documento nascido antes de o template ganhar a PeriodPolicy passa
        // a ser situado aqui). Sem [periodType] (template sem a regra), a competência existente fica intocada:
        // ela é história, não configuração.
        public void UpdateDetails(DateOnly date, DateOnly? validity, string content, PeriodType? periodType = null, bool usePreviousPeriod = false)
        {
            Date = date;
            Validity = validity;
            Content = content;
            if (periodType is not null)
                SetPeriod(date.ToDateTime(TimeOnly.MinValue), periodType, usePreviousPeriod);
        }

        public void UpdateDetails(DateOnly date, TimeSpan? validity, string content, PeriodType? periodType = null, bool usePreviousPeriod = false)
        {
            Date = date;
            DateOnly? dateValidity = null;
            if (validity is not null && validity != TimeSpan.Zero)
            {
                var dateTimeValidity = date.ToDateTime(TimeOnly.MinValue).Add(validity.Value);
                dateValidity = DateOnly.FromDateTime(dateTimeValidity);
            }
            Validity = dateValidity;
            Content = content;
            if (periodType is not null)
                SetPeriod(date.ToDateTime(TimeOnly.MinValue), periodType, usePreviousPeriod);
        }



        /// <summary>
        /// Agenda o envio para assinatura. Agendar de novo substitui o agendamento anterior — o disparo antigo
        /// compara a data que carrega com a gravada aqui e desiste sozinho quando elas divergem, então não é
        /// preciso rastrear nem cancelar o job já criado.
        /// </summary>
        public void ScheduleSignatureSend(ScheduledSignature schedule)
        {
            ScheduledSignature = schedule;
            AddDomainEvent(ScheduleDocumentSignatureSendEvent.Create(Document.Id, Id, Document.CompanyId, schedule.SendOn));
        }

        /// <summary>
        /// Cancela o agendamento. No-op quando não há nenhum: cancelar duas vezes (ou cancelar o que o disparo
        /// já consumiu) é a mesma intenção realizada, não um erro.
        /// </summary>
        public void CancelScheduledSignatureSend()
        {
            ScheduledSignature = null;
        }

        public bool IsSignatureScheduled => ScheduledSignature is not null;

        /// <summary>
        /// Marca esta unidade como substituta de [replacedUnitId]. Chamado só pelo agregado, na renovação.
        /// </summary>
        internal void SetReplacementOf(Guid replacedUnitId)
        {
            ReplacesDocumentUnitId = replacedUnitId;
        }

        public bool IsReplacement => ReplacesDocumentUnitId is not null;

        /// <summary>
        /// Se faz sentido pedir a substituta desta unidade. Renovar é trocar uma entrega que teve valor por outra,
        /// então só uma unidade que está (ou esteve) em vigência pode ser renovada — antes de vencer
        /// (<see cref="DocumentUnitStatus.OK"/>, <see cref="DocumentUnitStatus.Warning"/>) ou depois
        /// (<see cref="DocumentUnitStatus.Expired"/>).
        ///
        /// <see cref="DocumentUnitStatus.Deprecated"/> já foi substituída. Pendente, aguardando assinatura e
        /// requer validação ainda são a entrega em curso — o que falta ali é entregar, não renovar.
        /// <see cref="DocumentUnitStatus.NotApplicable"/> tem saída própria (invalidar volta a exigir o documento)
        /// e <see cref="DocumentUnitStatus.Invalid"/> nunca teve valor a renovar.
        /// </summary>
        public bool CanBeRenewed => Status == DocumentUnitStatus.OK ||
            Status == DocumentUnitStatus.Warning ||
            Status == DocumentUnitStatus.Expired;

        /// <summary>
        /// A unidade tem erro, foi entregue por engano, ou a decisão que ela registrava deixou de valer: perde
        /// qualquer valor legal.
        ///
        /// Aceita o que ainda não virou história — pendente, entregue mas ainda em vigência, aguardando conferência
        /// ou assinatura — e também <see cref="DocumentUnitStatus.NotApplicable"/>: dispensar o documento é uma
        /// decisão administrativa, não prova de cobertura, então voltar atrás quando o documento passa a ser exigido
        /// de novo não apaga período nenhum. <see cref="DocumentUnitStatus.Deprecated"/> e
        /// <see cref="DocumentUnitStatus.Expired"/> são recusados de propósito — são a prova de que o funcionário
        /// teve documento válido naquele período, e apagá-la é justamente o que a definição de Deprecated existe
        /// para impedir.
        /// </summary>
        public bool MarkAsInvalid()
        {
            if (Status == DocumentUnitStatus.OK ||
                Status == DocumentUnitStatus.NotApplicable ||
                IsInFlight)
            {
                Status = DocumentUnitStatus.Invalid;
                return true;
            }
            return false;
        }

        public void MarkAsValid()
        {
            if (Name != null && Extension != null)
            {
                Status = DocumentUnitStatus.OK;
                if (Validity is not null)
                    AddDomainEvent(ScheduleDocumentExpirationEvent.Create(Document.Id, Id, Document.CompanyId, (DateOnly)Validity!, Date));
            }
            else
            {
                throw new DomainException(this, DomainErrors.Document.DocumentUnitMissingNameOrExtension(Id));
            }
        }

        /// <summary>
        /// A unidade venceu e ainda não há substituta entregue — a exigência está descoberta AGORA.
        ///
        /// Só a partir de vigência (OK ou Warning): vencer é o fim de uma vigência, não um estado que uma pendente
        /// ou uma unidade já superada possa alcançar.
        /// </summary>
        public bool MarkAsExpired()
        {
            if (Status == DocumentUnitStatus.OK || Status == DocumentUnitStatus.Warning)
            {
                Status = DocumentUnitStatus.Expired;
                return true;
            }
            return false;
        }

        /// <summary>
        /// A unidade sai de vigência mas continua valendo como prova de que o funcionário esteve coberto naquele
        /// período. Aceita também a vencida: é exatamente a transição de <see cref="DocumentUnitStatus.Expired"/>
        /// para <see cref="DocumentUnitStatus.Deprecated"/> quando o substituto finalmente chega.
        /// </summary>
        public bool MarkAsDeprecated()
        {
            if (Status == DocumentUnitStatus.OK ||
                Status == DocumentUnitStatus.Warning ||
                Status == DocumentUnitStatus.Expired)
            {
                Status = DocumentUnitStatus.Deprecated;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Outra unidade passou a cobrir a mesma exigência. O que já teve valor vira histórico
        /// (<see cref="MarkAsDeprecated"/>); o que ainda estava em curso nunca chegou a valer nada e é descartado
        /// (<see cref="DiscardUndelivered"/>). A ordem importa: uma unidade OK é histórico, não engano.
        ///
        /// <see cref="DocumentUnitStatus.NotApplicable"/> não entra em nenhum dos dois e sobrevive à supersessão:
        /// dispensar o documento é uma decisão deliberada do RH, e uma entrega nova não a revoga sozinha. Desfazê-la
        /// é o <see cref="MarkAsInvalid"/> chamado de propósito.
        /// </summary>
        public bool Supersede() => MarkAsDeprecated() || DiscardUndelivered();

        /// <summary>
        /// Descarta o que ainda estava em curso. Existe separado de <see cref="MarkAsInvalid"/> porque aquele aceita
        /// também OK e NotApplicable, que na supersessão têm destino próprio — virar histórico e sobreviver,
        /// respectivamente.
        /// </summary>
        private bool DiscardUndelivered()
        {
            if (IsInFlight)
            {
                Status = DocumentUnitStatus.Invalid;
                return true;
            }
            return false;
        }

        public bool MarkAsNotApplicable()
        {
            if (Status == DocumentUnitStatus.Pending)
            {
                Status = DocumentUnitStatus.NotApplicable;
                return true;
            }
                
            return false;
        }

        public bool MarkAsWarning()
        {
            if (Status == DocumentUnitStatus.OK)
            {
                Status = DocumentUnitStatus.Warning;
                return true;
            }
            return false;
        }


        public bool MarkAsAwaitingSignature()
        {
            if (HasInvalidDateOrValidity)
                throw new DomainException(this, DomainErrors.DataInvalid(nameof(Date), Date));

            if (Date > DateOnly.FromDateTime(DateTime.UtcNow))
                throw new DomainException(this, DomainErrors.Document.DocumentUnitCantBeSentBeforeOfficialDate(Id, Date));

            if (Status == DocumentUnitStatus.Pending)
            {
                Status = DocumentUnitStatus.AwaitingSignature;
                SentToSignatureAt = DateTime.UtcNow;
                return true;
            }
            return false;
        }

        public bool HasContent => string.IsNullOrEmpty(Content) == false;

        public bool IsAwaitingSignature => Status == DocumentUnitStatus.AwaitingSignature;
        public bool RequiresVerification => Status == DocumentUnitStatus.RequiresValidation;
        public bool IsOK => Status == DocumentUnitStatus.OK;
        public bool IsPending => Status == DocumentUnitStatus.Pending;
        public bool IsExpired => Status == DocumentUnitStatus.Expired;
        public bool IsNotApplicable => Status == DocumentUnitStatus.NotApplicable;

        /// <summary>
        /// A unidade ainda está em curso: nada foi entregue e aceito, então ela não comprova nada nem dispensa nada.
        /// </summary>
        public bool IsInFlight => Status == DocumentUnitStatus.Pending ||
            Status == DocumentUnitStatus.RequiresValidation ||
            Status == DocumentUnitStatus.AwaitingSignature;

        /// <summary>
        /// Se esta unidade cobre a exigência. NotApplicable cobre tanto quanto OK — é a exceção deliberada à
        /// regra, não uma falta. Warning ainda está em vigência, só perto de vencer.
        /// </summary>
        public bool CoversRequirement => Status == DocumentUnitStatus.OK ||
            Status == DocumentUnitStatus.Warning ||
            Status == DocumentUnitStatus.NotApplicable;

        /// <summary>
        /// Se esta unidade ainda ocupa a competência em que está — ou seja, se ela responde por aquele período,
        /// seja cobrindo (OK, A Vencer, Não Aplicável), em curso (pendente, requer validação, aguardando
        /// assinatura) ou já caducada (vencida, esperando substituto).
        ///
        /// Mais amplo que <see cref="CoversRequirement"/> de propósito: quem pergunta isto quer saber se há
        /// espaço para criar OUTRA unidade na competência, e uma pendente já é o espaço ocupado. Só
        /// <see cref="DocumentUnitStatus.Invalid"/> (nunca teve valor) e <see cref="DocumentUnitStatus.Deprecated"/>
        /// (já foi substituída) desocupam.
        /// </summary>
        public bool OccupiesPeriod => Status != DocumentUnitStatus.Invalid &&
            Status != DocumentUnitStatus.Deprecated;
        public string GetNameWithExtension => $"{Name}.{Extension}";
        public bool CanEdit => (Name == null || Name.IsNullOrEmpty) && Extension == null;

        public bool IsPeriodDaily => Period?.IsDaily ?? false;
        public bool IsPeriodWeekly => Period?.IsWeekly ?? false;
        public bool IsPeriodMonthly => Period?.IsMonthly ?? false;
        public bool IsPeriodYearly => Period?.IsYearly ?? false;
        public bool IsPeriod => Period != null; 

        private bool HasInvalidDateOrValidity => Date == DateOnly.MinValue || Date == DateOnly.MaxValue || (Validity != null && (Validity == DateOnly.MinValue || Validity == DateOnly.MaxValue));

        public void SetSignatureInfo(string documentToken, string signatureUrl)
        {
            SignatureDocumentToken = documentToken;
            SignatureUrl = signatureUrl;
        }

        public void SetAttachmentSignatureInfo(string sessionDocToken, string attachmentToken, string signatureUrl)
        {
            SignatureDocumentToken = sessionDocToken;
            AttachmentToken = attachmentToken;
            SignatureUrl = signatureUrl;
        }

        public bool IsSessionPrimary => IsAwaitingSignature && SignatureDocumentToken != null && AttachmentToken == null;

        public void SetWorkloadEndDate(DateOnly endDate)
        {
            WorkloadEndDate = endDate;
        }

    }
}
