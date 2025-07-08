namespace PeopleManagement.Application.Commands.EmployeeCommands.FinishedContractEmployee
{
    public record FinishedContractEmployeeCommand(Guid EmployeeId, Guid CompanyId, DateOnly FinishDateContract) : IRequest<FinishedContractEmployeeResponse>
    {
    }

    public record FinishedContractEmployeeModel(Guid EmployeeId, DateOnly FinishDateContract)
    {
        public FinishedContractEmployeeCommand ToCommand(Guid company) => new(EmployeeId, company, FinishDateContract); 
    }
}
