namespace PeopleManagement.Application.Commands.EmployeeCommands.DocumentSigningOptions
{
    public record DocumentSigningOptionsCommand(Guid EmployeeId, Guid CompanyId, int DocumentSigningOptions) : IRequest<DocumentSigningOptionsResponse>
    {
    }
    public record DocumentSigningOptionsModel(Guid EmployeeId,  int DocumentSigningOptions) : IRequest<DocumentSigningOptionsResponse>
    {
        public DocumentSigningOptionsCommand ToCommand(Guid companyId) => new(EmployeeId, companyId, DocumentSigningOptions);
    }
}
