namespace PeopleManagement.Application.Commands.DocumentCommands.GeneratePdf
{
    public record GeneratePdfCommand(Guid DocumentUnitId, Guid DocumentId, Guid EmployeeId, Guid CompanyId) : IRequest<GeneratePdfResponse> 
    {

    }

    public record GeneratePdfModel(Guid DocumentUnitId, Guid DocumentId, Guid EmployeeId)
    {
        public GeneratePdfCommand ToCommand(Guid company) => new(DocumentUnitId, DocumentId, EmployeeId, company);
    }
}
