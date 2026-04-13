namespace PeopleManagement.Application.Commands.RequireDocumentsCommands.GenerateDocumentUnits
{
    public record GenerateDocumentUnitsCommand(Guid RequireDocumentId, Guid CompanyId) : IRequest<GenerateDocumentUnitsResponse>;

    public record GenerateDocumentUnitsModel(Guid RequireDocumentId)
    {
        public GenerateDocumentUnitsCommand ToCommand(Guid company) => new(RequireDocumentId, company);
    }
}
