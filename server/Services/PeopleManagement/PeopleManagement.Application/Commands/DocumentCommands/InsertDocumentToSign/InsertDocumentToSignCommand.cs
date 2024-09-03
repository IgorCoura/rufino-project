namespace PeopleManagement.Application.Commands.DocumentCommands.InsertDocumentToSign
{
    public record InsertDocumentToSignCommand(Guid DocumentUnitId, Guid DocumentId, Guid EmployeeId, Guid CompanyId, string Extension, Stream Stream, DateTime DateLimitToSign, int EminderEveryNDays) : IRequest<InsertDocumentToSignResponse>
    {
    }

    public record InsertDocumentToSignModel(Guid DocumentUnitId, Guid DocumentId, Guid EmployeeId, DateTime DateLimitToSign, int EminderEveryNDays)
    {
        public InsertDocumentToSignCommand ToCommand(Stream stream, string extension, Guid company) => new(DocumentUnitId, DocumentId, EmployeeId, company, extension, stream, DateLimitToSign, EminderEveryNDays);
    }
}
