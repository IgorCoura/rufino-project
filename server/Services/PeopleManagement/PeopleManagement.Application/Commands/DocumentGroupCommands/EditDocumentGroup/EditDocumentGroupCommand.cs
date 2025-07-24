namespace PeopleManagement.Application.Commands.DocumentGroupCommands.EditDocumentGroup
{
    public record EditDocumentGroupCommand(Guid Id, Guid CompanyId, string Name, string Description) : IRequest<EditDocumentGroupResponse>
    {

    }

    public record EditDocumentGroupModel(Guid Id, string Name, string Description) 
    {
        public EditDocumentGroupCommand ToCommand( Guid companyId) =>
            new(Id, companyId, Name, Description);

    }
}
