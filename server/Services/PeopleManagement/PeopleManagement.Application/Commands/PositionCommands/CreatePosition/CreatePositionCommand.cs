namespace PeopleManagement.Application.Commands.PositionCommands.CreatePosition
{
    public record CreatePositionCommand(Guid CompanyId, string Name, string Description,
        string CBO, Guid DepartmentId) : IRequest<CreatePositionResponse>  
    {
    }

    public record CreatePositionModel(string Name, string Description, string CBO, Guid DepartmentId)
    {
        public CreatePositionCommand ToCommand(Guid companyId) =>
            new(companyId, Name, Description, CBO, DepartmentId);
    }   
}
