namespace PeopleManagement.Domain.AggregatesModel.DocumentAggregate
{
    /// <summary>
    /// O estado de uma entrega concreta de documento. É o único status do agregado que é realmente escrito —
    /// o do <see cref="Document"/> e o do funcionário são derivados destes.
    ///
    /// Cobertura da exigência: <see cref="OK"/>, <see cref="Warning"/> e <see cref="NotApplicable"/> cobrem;
    /// o resto deixa a exigência descoberta, com exceção de <see cref="Deprecated"/>, que é histórico e por
    /// definição já tem substituto.
    /// </summary>
    public sealed class DocumentUnitStatus : Enumeration
    {
        const int PENDING = 1;
        const int OKEY = 2;
        const int DEPRECATED = 3;
        const int INVALID = 4;
        const int REQUIRES_VALIDATION = 5;
        const int NOT_APPLICABLE = 6;
        const int AWAITING_SIGNATURE = 7;
        const int WARNING = 8;
        const int EXPIRED = 9;


        /// <summary>Falta entregar. Não há arquivo ainda.</summary>
        public static readonly DocumentUnitStatus Pending = new(PENDING, nameof(Pending));

        /// <summary>Documento válido e vigente.</summary>
        public static readonly DocumentUnitStatus OK = new(OKEY, nameof(OK));

        /// <summary>
        /// Foi válido, saiu de vigência, e já existe substituto. Mantido por valor probatório: comprova que o
        /// funcionário tinha documento válido naquele período. Diferente de <see cref="Expired"/>, que é a
        /// mesma saída de vigência SEM substituto — só ali há problema de conformidade.
        /// </summary>
        public static readonly DocumentUnitStatus Deprecated = new(DEPRECATED, nameof(Deprecated));

        /// <summary>Erro ou engano — não tem valor legal nenhum.</summary>
        public static readonly DocumentUnitStatus Invalid = new(INVALID, nameof(Invalid));

        /// <summary>Arquivo entregue, alguém precisa conferir se vale.</summary>
        public static readonly DocumentUnitStatus RequiresValidation = new(REQUIRES_VALIDATION, nameof(RequiresValidation));

        /// <summary>
        /// Este documento não se aplica a este funcionário. Exceção deliberada à regra, e por isso vale tanto
        /// quanto <see cref="OK"/> para efeito de cobertura.
        /// </summary>
        public static readonly DocumentUnitStatus NotApplicable = new(NOT_APPLICABLE, nameof(NotApplicable));

        /// <summary>Enviado ao funcionário, aguardando assinatura.</summary>
        public static readonly DocumentUnitStatus AwaitingSignature = new(AWAITING_SIGNATURE, nameof(AwaitingSignature));

        /// <summary>Ainda válido, mas perto de vencer — o RH precisa providenciar o substituto.</summary>
        public static readonly DocumentUnitStatus Warning = new(WARNING, nameof(Warning));

        /// <summary>
        /// Venceu e ainda NÃO há substituto entregue. É o estado que representa risco de conformidade; assim
        /// que uma entrega o supera, vira <see cref="Deprecated"/>.
        /// </summary>
        public static readonly DocumentUnitStatus Expired = new(EXPIRED, nameof(Expired));

        private DocumentUnitStatus(int id, string name) : base(id, name)
        {
        }

        public static implicit operator DocumentUnitStatus(int id) => Enumeration.FromValue<DocumentUnitStatus>(id);
        public static implicit operator DocumentUnitStatus(string name) => Enumeration.FromDisplayName<DocumentUnitStatus>(name);
    }
}

