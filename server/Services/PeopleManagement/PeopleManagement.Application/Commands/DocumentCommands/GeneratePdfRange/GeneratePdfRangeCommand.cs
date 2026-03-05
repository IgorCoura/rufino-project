namespace PeopleManagement.Application.Commands.DocumentCommands.GeneratePdfRange
{
    public record GeneratePdfRangeItem(Guid DocumentId, IEnumerable<Guid> DocumentUnitIds);

    public record GeneratePdfRangeCommand(
        IEnumerable<GeneratePdfRangeItem> Items,
        Guid EmployeeId,
        Guid CompanyId) : IRequest<GeneratePdfRangeResponse>;
}
