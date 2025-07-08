namespace PeopleManagement.Application.Commands.DocumentCommands.UpdateDocumentUnitDetails
{
    public record UpdateDocumentUnitDetailsCommand(Guid DocumentUnitId, Guid DocumentId, Guid EmployeeId, Guid CompanyId,
        DateOnly DocumentUnitDate) : IRequest<UpdateDocumentUnitDetailsResponse>
    {
    }

    public record UpdateDocumentUnitDetailsModel(Guid DocumentUnitId, Guid DocumentId, Guid EmployeeId,
        DateOnly DocumentUnitDate)
    {
        public UpdateDocumentUnitDetailsCommand ToCommand(Guid company) => new(DocumentUnitId, DocumentId, EmployeeId, company, DocumentUnitDate);
    }
}
