namespace PeopleManagement.Application.Commands.DocumentCommands.CreateDocument
{
    /// <summary>
    /// Cria uma unidade no documento. [Date] é a data do documento e situa a unidade numa competência — é o RH
    /// preenchendo à mão um período que ficou sem unidade, e só documento por competência aceita.
    ///
    /// Opcional porque o app legado cria unidade sem data nenhuma (a unidade nasce esperando data).
    /// </summary>
    public record CreateDocumentCommand(Guid DocumentId, Guid EmployeeId, Guid CompanyId, DateOnly? Date = null) : IRequest<CreateDocumentResponse>
    {
    }

    public record CreateDocumentModel(Guid DocumentId, Guid EmployeeId, DateOnly? Date = null)
    {
        public CreateDocumentCommand ToCommand(Guid company) => new(DocumentId, EmployeeId, company, Date);
    }
}
