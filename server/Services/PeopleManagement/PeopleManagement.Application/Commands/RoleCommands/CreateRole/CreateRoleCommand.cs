namespace PeopleManagement.Application.Commands.RoleCommands.CreateRole
{
    public record CreateRoleCommand(Guid CompanyId, string Name, string Description, string CBO,  CreateRemunerationModel Remuneration, Guid PositionId) : IRequest<CreateRoleResponse>
    {


    }

    public record CreateRoleModel(string Name, string Description, string CBO, CreateRemunerationModel Remuneration, Guid PositionId) 
    {
        public CreateRoleCommand ToCommand(Guid companyId) =>
            new(companyId, Name, Description, CBO, Remuneration, PositionId);
    }

    public record CreateRemunerationModel(int PaymentUnit, CreateCurrencyModel BaseSalary, string Description)
    {
    }

    public record CreateCurrencyModel(int Type, string Value)
    {

    }
}
