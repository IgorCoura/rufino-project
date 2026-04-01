using PeopleManagement.Application.Commands.DocumentCommands.BatchGeneratePdf;

namespace PeopleManagement.Application.Commands.DocumentCommands.BatchGenerateAndSign
{
    public record BatchGenerateAndSignCommand(
        IEnumerable<BatchGeneratePdfItem> Items,
        DateTime DateLimitToSign,
        int ReminderEveryNDays,
        Guid CompanyId
    ) : IRequest<BatchGenerateAndSignResponse>;

    public record BatchGenerateAndSignModel(
        IEnumerable<BatchGeneratePdfItem> Items,
        DateTime DateLimitToSign,
        int ReminderEveryNDays)
    {
        public BatchGenerateAndSignCommand ToCommand(Guid company) =>
            new(Items, DateLimitToSign, ReminderEveryNDays, company);
    }
}
