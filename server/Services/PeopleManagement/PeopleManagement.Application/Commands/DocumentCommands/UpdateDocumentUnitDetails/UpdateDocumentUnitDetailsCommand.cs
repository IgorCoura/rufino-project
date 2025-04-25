namespace PeopleManagement.Application.Commands.DocumentCommands.UpdateDocumentUnitDetails
{
    public record UpdateDocumentUnitDetailsCommand(Guid DocumentUnitId, Guid DocumentId, Guid EmployeeId, Guid CompanyId, 
        DateTime DocumentUnitDate) : IRequest<UpdateDocumentUnitDetailsResponse>
    {
    }

    public record UpdateDocumentUnitDetailsModel(Guid DocumentUnitId, Guid DocumentId, Guid EmployeeId,
        DateTime DocumentUnitDate)
    {
        public UpdateDocumentUnitDetailsCommand ToCommand(Guid company) => new(DocumentUnitId, DocumentId, EmployeeId, company, DocumentUnitDate);
    }
}
