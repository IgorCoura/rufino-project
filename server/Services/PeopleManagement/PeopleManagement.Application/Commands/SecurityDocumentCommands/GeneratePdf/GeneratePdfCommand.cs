namespace PeopleManagement.Application.Commands.SecurityDocumentCommands.GeneratePdf
{
    public record GeneratePdfCommand(Guid DocumentId, Guid SecurityDocumentId, Guid EmployeeId, Guid CompanyId) : IRequest<GeneratePdfResponse> 
    {

    }
}
