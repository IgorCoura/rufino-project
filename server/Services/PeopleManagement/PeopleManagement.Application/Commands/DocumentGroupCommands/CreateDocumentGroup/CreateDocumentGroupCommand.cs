using PuppeteerSharp;

namespace PeopleManagement.Application.Commands.DocumentGroupCommands.CreateDocumentGroup
{
    public record CreateDocumentGroupCommand(Guid CompanyId, string Name, string Description) : IRequest<CreateDocumentGroupResponse>
    {

    }

    public record CreateDocumentGroupModel(string Name, string Description)
    {
        public CreateDocumentGroupCommand ToCommand(Guid companyId) =>
            new(companyId, Name, Description);
    }

}
