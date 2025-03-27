namespace PeopleManagement.Application.Commands.EmployeeCommands.NewContractEmployee
{
    public record NewContractEmployeeCommand(Guid EmployeeId, Guid CompanyId, DateOnly InitDateContract, int ContractType) : IRequest<NewContractEmployeeResponse>
    {
    }

    public record NewContractEmployeeModel(Guid EmployeeId, DateOnly InitDateContract, int ContractType)
    {
        public NewContractEmployeeCommand ToCommand(Guid companyId) => new(EmployeeId, companyId, InitDateContract, ContractType);
    }
}
