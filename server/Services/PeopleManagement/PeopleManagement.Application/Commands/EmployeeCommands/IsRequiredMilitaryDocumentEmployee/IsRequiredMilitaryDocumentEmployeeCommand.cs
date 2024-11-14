namespace PeopleManagement.Application.Commands.EmployeeCommands.IsRequiredMilitaryDocumentEmployee
{
    public record IsRequiredMilitaryDocumentEmployeeCommand(Guid EmployeeId, Guid CompanyId) : IRequest<IsRequiredMilitaryDocumentEmployeeResponse>
    {
    }

    public record IsRequiredMilitaryDocumentEmployeeModel(Guid EmployeeId) 
    {
        public IsRequiredMilitaryDocumentEmployeeCommand ToCommand(Guid companyId) => new (EmployeeId, companyId);
    }
}
