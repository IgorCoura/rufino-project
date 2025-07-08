namespace PeopleManagement.Application.Commands.PositionCommands.EditPosition
{
    public record EditPositionCommand(Guid Id, Guid CompanyId, string Name, string Description,
       string CBO) : IRequest<EditPositionResponse>
    {
    }

    public record EditPositionModel(Guid Id, string Name, string Description, string CBO)
    {
        public EditPositionCommand ToCommand(Guid companyId) =>
            new(Id, companyId, Name, Description, CBO);
    }

}
