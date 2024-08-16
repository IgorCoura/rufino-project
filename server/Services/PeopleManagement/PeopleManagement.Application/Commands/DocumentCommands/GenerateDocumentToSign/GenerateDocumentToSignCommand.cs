namespace PeopleManagement.Application.Commands.DocumentCommands.GenerateDocumentToSign
{
    public record GenerateDocumentToSignCommand(Guid DocumentUnitId, Guid DocumentId, Guid EmployeeId, Guid CompanyId, DateTime DateLimitToSign, 
        int EminderEveryNDays) : IRequest<GenerateDocumentToSignResponse>
    {
    }
}
