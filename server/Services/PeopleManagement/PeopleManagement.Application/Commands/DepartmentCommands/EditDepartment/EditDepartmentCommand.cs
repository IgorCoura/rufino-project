namespace PeopleManagement.Application.Commands.DepartmentCommands.EditDepartment
{
    public record EditDepartmentCommand(Guid Id, Guid CompanyId, string Name, string Description) : IRequest<EditDepartmentResponse>
    {

    }

    public record EditDepartmentModel(Guid Id, string Name, string Description) 
    {
        public EditDepartmentCommand ToCommand( Guid companyId) =>
            new(Id, companyId, Name, Description);

    }
}
