namespace PeopleManagement.Domain.AggregatesModel.DocumentAggregate
{
    /// <summary>
    /// O estado do documento como um todo, DERIVADO dos status das suas unidades — nunca escrito diretamente
    /// (ver <see cref="Document.RefreshDocumentStatus"/>).
    /// </summary>
    public class DocumentStatus : Enumeration
    {
        const int REQUIRES_DOCUMENT = 1;
        const int REQUIRES_VALIDATION = 2;
        const int OKAY = 3;
        const int DEPRECATED = 4;
        const int AWAITING_SIGNATURE = 5;
        const int WARNING = 6;
        const int EXPIRED = 7;

        /// <summary>Falta entregar: há competência sem nenhuma unidade que a cubra.</summary>
        public static readonly DocumentStatus RequiresDocument = new(REQUIRES_DOCUMENT, nameof(RequiresDocument));

        /// <summary>Entregue, aguardando conferência.</summary>
        public static readonly DocumentStatus RequiresValidation = new(REQUIRES_VALIDATION, nameof(RequiresValidation));

        /// <summary>Todas as competências cobertas.</summary>
        public static readonly DocumentStatus OK = new(OKAY, nameof(OK));

        /// <summary>
        /// O documento inteiro saiu de cena — deixou de ser exigido deste funcionário. Não é problema de
        /// conformidade; um documento cuja última unidade foi apenas substituída fica <see cref="OK"/>.
        /// </summary>
        public static readonly DocumentStatus Deprecated = new(DEPRECATED, nameof(Deprecated));

        /// <summary>Enviado ao funcionário, aguardando assinatura.</summary>
        public static readonly DocumentStatus AwaitingSignature = new(AWAITING_SIGNATURE, nameof(AwaitingSignature));

        /// <summary>Coberto, mas com unidade perto de vencer.</summary>
        public static readonly DocumentStatus Warning = new(WARNING, nameof(Warning));

        /// <summary>Havia cobertura, venceu, e ainda não chegou substituto.</summary>
        public static readonly DocumentStatus Expired = new(EXPIRED, nameof(Expired));

        private DocumentStatus(int id, string name) : base(id, name)
        {
        }

        public static implicit operator DocumentStatus(int id) => Enumeration.FromValue<DocumentStatus>(id);
        public static implicit operator DocumentStatus(string name) => Enumeration.FromDisplayName<DocumentStatus>(name);

        /// <summary>
        /// Gravidade do status, do pior (0) para o melhor. É a ÚNICA ordem de severidade do sistema: o status do
        /// documento é o pior entre as suas competências, e o rollup para grupo e funcionário lê a mesma escala.
        ///
        /// <see cref="Expired"/> é o pior porque significa cobertura que caducou — risco ativo, diferente de
        /// <see cref="RequiresDocument"/>, que é uma exigência que ainda não foi cumprida nenhuma vez.
        ///
        /// <see cref="Deprecated"/> é o mais benigno de todos, ATRÁS de <see cref="OK"/>: uma competência
        /// encerrada por substituição não tem nada a dizer sobre o documento, então ela só aparece quando é tudo
        /// o que existe — que é exatamente o caso de o documento inteiro ter saído de cena.
        /// </summary>
        public static int Severity(DocumentStatus status) => status.Id switch
        {
            EXPIRED => 0,
            REQUIRES_DOCUMENT => 1,
            REQUIRES_VALIDATION => 2,
            AWAITING_SIGNATURE => 3,
            WARNING => 4,
            OKAY => 5,
            DEPRECATED => 6,
            _ => 5
        };
    }
}
