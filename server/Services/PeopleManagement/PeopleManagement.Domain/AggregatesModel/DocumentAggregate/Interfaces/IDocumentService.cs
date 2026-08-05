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
        Task<DocumentUnit> CreateDocumentUnit(Guid documentId, Guid employeeId, Guid companyId, CancellationToken cancellation = default);

        /// <summary>
        /// Deprecia a unidade (sai de vigência, continua valendo como prova) e deixa uma pendente no lugar.
        ///
        /// A pendente substituta não é opcional: a exigência continua de pé, e desde que o botão de criar
        /// unidade avulsa saiu da tela este é o único caminho para o RH voltar a ter o que preencher.
        /// </summary>
        Task<DocumentUnit> DeprecateDocumentUnit(Guid documentUnitId, Guid documentId, Guid employeeId, Guid companyId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Invalida a unidade (erro ou engano, sem valor legal) e deixa uma pendente no lugar. Mesmo caminho da
        /// recusa de validação.
        /// </summary>
        Task<DocumentUnit> InvalidateDocumentUnit(Guid documentUnitId, Guid documentId, Guid employeeId, Guid companyId, CancellationToken cancellationToken = default);
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
