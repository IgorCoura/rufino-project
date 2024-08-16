namespace PeopleManagement.Application.Commands.DocumentCommands.InsertDocumentToSign
{
    public record InsertDocumentToSignCommand(Guid DocumentUnitId, Guid DocumentId, Guid EmployeeId, Guid CompanyId, string Extension, Stream Stream, DateTime DateLimitToSign, int EminderEveryNDays) : IRequest<InsertDocumentToSignResponse>
    {
    }
}
