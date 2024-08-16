namespace PeopleManagement.Application.Commands.DocumentCommands.InsertDocumentToSign
{
    public record InsertDocumentToSignModel(Guid DocumentUnitId, Guid DocumentId, Guid EmployeeId, Guid CompanyId, DateTime DateLimitToSign, int EminderEveryNDays)
    {
        public InsertDocumentToSignCommand ToCommand(Stream stream, string extension) => new(DocumentUnitId, DocumentId, EmployeeId, CompanyId, extension, stream, DateLimitToSign, EminderEveryNDays);
    }
}
