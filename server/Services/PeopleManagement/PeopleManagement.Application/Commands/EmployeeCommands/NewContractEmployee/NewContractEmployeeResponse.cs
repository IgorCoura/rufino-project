using PeopleManagement.Application.Commands.EmployeeCommands.FinishedContractEmployee;

namespace PeopleManagement.Application.Commands.EmployeeCommands.NewContractEmployee
{
    public record NewContractEmployeeResponse(Guid Id) : BaseDTO(Id)
    {
        public static implicit operator NewContractEmployeeResponse(Guid id) => new(id);
    }
}
