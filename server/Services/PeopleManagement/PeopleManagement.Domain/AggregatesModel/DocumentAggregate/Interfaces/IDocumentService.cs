namespace PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces
{
    /// <summary>
    /// Situação do snapshot gravado numa unidade frente aos dados atuais do funcionário.
    ///
    /// [CheckFailed] indica que a comparação não pôde ser concluída (algum bloco de dado não foi recuperado) —
    /// nesse caso [IsOutdated] é sempre falso, porque um bloco ausente por falha é indistinguível de um dado que
    /// mudou e avisar levaria o usuário a sobrescrever conteúdo bom.
    /// </summary>
    public sealed record DocumentUnitContentStatus(Guid DocumentUnitId, bool IsOutdated, bool CheckFailed);

    public interface IDocumentService
    {
        /// <summary>
        /// Cria uma unidade no documento (<c>POST /document</c>).
        ///
        /// Sem [date], é a criação avulsa que serve o app legado: a unidade nasce esperando data — na competência
        /// mínima, quando o template é por competência.
        ///
        /// Com [date], é a criação manual de UMA competência: só vale para documento por competência (os demais
        /// não podem ter duas unidades cobrindo ao mesmo tempo, e a próxima nasce de depreciar/invalidar a
        /// vigente ou de renovar), a competência precisa estar livre, e a unidade já nasce com data, validade e
        /// snapshot — o mesmo estado que "editar data" deixaria.
        /// </summary>
        Task<DocumentUnit> CreateDocumentUnit(Guid documentId, Guid employeeId, Guid companyId, DateOnly? date = null, CancellationToken cancellation = default);

        /// <summary>
        /// Deprecia a unidade (sai de vigência, continua valendo como prova) e deixa uma pendente no lugar.
        ///
        /// A pendente substituta não é opcional: a exigência continua de pé, e num documento que não é por
        /// competência este (com o renovar) é o caminho para o RH voltar a ter o que preencher — lá a criação
        /// manual não existe, justamente porque duas unidades não podem cobrir ao mesmo tempo.
        /// </summary>
        Task<DocumentUnit> DeprecateDocumentUnit(Guid documentUnitId, Guid documentId, Guid employeeId, Guid companyId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Invalida a unidade (erro ou engano, sem valor legal) e deixa uma pendente no lugar. Mesmo caminho da
        /// recusa de validação.
        /// </summary>
        Task<DocumentUnit> InvalidateDocumentUnit(Guid documentUnitId, Guid documentId, Guid employeeId, Guid companyId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Renova a unidade: cria a substituta pendente vinculada a ela e consome uma renovação da cota do
        /// template. Vale antes de vencer (OK, A Vencer) e depois (Vencido).
        ///
        /// A unidade renovada NÃO sai de vigência aqui — é a entrega da substituta que a deprecia. Enquanto isso
        /// o documento continua contando com ela, então renovar no prazo não deixa o documento pior.
        ///
        /// Idempotente: pedir de novo devolve a mesma substituta, sem consumir cota outra vez.
        /// </summary>
        Task<DocumentUnit> RenewDocumentUnit(Guid documentUnitId, Guid documentId, Guid employeeId, Guid companyId, CancellationToken cancellationToken = default);
        Task<DocumentUnit> UpdateDocumentUnitDetails(Guid documentUnitId, Guid documentId, Guid employeeId, Guid companyId, DateOnly documentUnitDate, CancellationToken cancellationToken = default);
        Task CreateDocumentUnitsForEvent(Guid employeeId, Guid companyId, int eventId, CancellationToken cancellationToken = default);
        Task<byte[]> GeneratePdf(Guid documentUnitId, Guid documentId, Guid employeeId, Guid companyId, CancellationToken cancellation = default);
        Task<IReadOnlyList<(Guid DocumentUnitId, Guid DocumentId, string DocumentName, DateOnly DocumentUnitDate, byte[] Pdf)>> GeneratePdfRange(
            IEnumerable<(Guid DocumentId, IEnumerable<Guid> DocumentUnitIds)> items,
            Guid employeeId, Guid companyId, CancellationToken cancellationToken = default);
        Task InsertFileWithoutRequireValidation(Guid documentUnitId, Guid documentId, Guid employeeId, Guid companyId, Extension extension, Stream stream, CancellationToken cancellationToken = default);
        Task GenerateDocumentUnitsForRequireDocument(Guid requireDocumentId, Guid companyId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Compara o snapshot gravado em cada unidade com os dados atuais do funcionário, remontando o conteúdo
        /// pelo mesmo caminho da gravação e confrontando as strings cruas.
        ///
        /// Não altera nada. Usa as datas já gravadas na unidade como entrada, então o que a comparação enxerga é
        /// divergência de dado recuperado — não mudança de regra do template.
        /// </summary>
        Task<IReadOnlyList<DocumentUnitContentStatus>> CheckOutdatedContent(
            IEnumerable<(Guid DocumentUnitId, Guid DocumentId, Guid EmployeeId)> items,
            Guid companyId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Regrava o snapshot das unidades com os dados atuais, mantendo a data que cada uma já tem.
        /// </summary>
        Task RefreshDocumentUnitContent(
            IEnumerable<(Guid DocumentUnitId, Guid DocumentId, Guid EmployeeId)> items,
            Guid companyId, CancellationToken cancellationToken = default);
    }
}
