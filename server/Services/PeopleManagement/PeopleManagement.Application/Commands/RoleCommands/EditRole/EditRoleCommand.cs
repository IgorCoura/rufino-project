namespace PeopleManagement.Application.Commands.RoleCommands.EditRole
{
    public record EditRoleCommand(Guid Id, Guid CompanyId, string Name, string Description, string CBO, EditRemunerationModel Remuneration) : IRequest<EditRoleResponse>
    {


    }

    public record EditRoleModel(Guid Id, string Name, string Description, string CBO, EditRemunerationModel Remuneration)
    {
        public EditRoleCommand ToCommand(Guid companyId) =>
            new(Id, companyId, Name, Description, CBO, Remuneration);
    }

    public record EditRemunerationModel(int PaymentUnit, EditCurrencyModel BaseSalary, string Description)
    {
    }

    public record EditCurrencyModel(int Type, string Value)
    {

    }
}
