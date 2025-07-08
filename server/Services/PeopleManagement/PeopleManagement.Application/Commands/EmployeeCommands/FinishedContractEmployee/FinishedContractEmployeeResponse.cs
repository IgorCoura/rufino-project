namespace PeopleManagement.Application.Commands.EmployeeCommands.FinishedContractEmployee
{
    public record FinishedContractEmployeeResponse(Guid Id) : BaseDTO(Id)
    {
        public static implicit operator FinishedContractEmployeeResponse(Guid id) => new(id);
    }
}
