using PeopleManagement.Application.Commands.DocumentCommands.CreateDocument;

namespace PeopleManagement.Application.Commands.DocumentCommands.GeneratePdf
{
    public record GeneratePdfResponse(
        Guid Id,
        byte[] Pdf,
        string EmployeeName,
        string DocumentName,
        DateOnly DocumentUnitDate) : BaseDTO(Id)
    {
        public static implicit operator GeneratePdfResponse(Guid id) => new(id, [], string.Empty, string.Empty, default);
    }
}
