using PuppeteerSharp;

namespace PeopleManagement.Application.Commands.DepartmentCommands.CreateDepartment
{
    public record CreateDepartmentCommand(Guid CompanyId, string Name, string Description) : IRequest<CreateDepartmentResponse>
    {

    }

    public record CreateDepartmentModel(string Name, string Description)
    {
        public CreateDepartmentCommand ToCommand(Guid companyId) =>
            new(companyId, Name, Description);
    }

}
