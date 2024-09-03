namespace PeopleManagement.Application.Commands.DocumentCommands.GenerateDocumentToSign
{
    public record GenerateDocumentToSignCommand(Guid DocumentUnitId, Guid DocumentId, Guid EmployeeId, Guid CompanyId, DateTime DateLimitToSign, 
        int EminderEveryNDays) : IRequest<GenerateDocumentToSignResponse>
    {
    }

    public record GenerateDocumentToSignModel(Guid DocumentUnitId, Guid DocumentId, Guid EmployeeId, DateTime DateLimitToSign,
        int EminderEveryNDays)
    {
        public GenerateDocumentToSignCommand ToCommand(Guid company) => new(DocumentUnitId, DocumentId, EmployeeId, company, 
            DateLimitToSign, EminderEveryNDays);
    }
}
