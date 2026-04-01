using PeopleManagement.Application.Commands.DocumentCommands.InsertDocumentRange;

namespace PeopleManagement.Application.Commands.DocumentCommands.InsertDocumentRangeToSign
{
    public record InsertDocumentRangeToSignCommand(
        IEnumerable<InsertDocumentRangeItem> Items,
        DateTime DateLimitToSign,
        int ReminderEveryNDays,
        Guid CompanyId
    ) : IRequest<InsertDocumentRangeResponse>;
}
