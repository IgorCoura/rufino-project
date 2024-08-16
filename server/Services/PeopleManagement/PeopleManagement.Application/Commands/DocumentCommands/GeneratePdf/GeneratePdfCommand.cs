namespace PeopleManagement.Application.Commands.DocumentCommands.GeneratePdf
{
    public record GeneratePdfCommand(Guid DocumentUnitId, Guid DocumentId, Guid EmployeeId, Guid CompanyId) : IRequest<GeneratePdfResponse> 
    {

    }
}
