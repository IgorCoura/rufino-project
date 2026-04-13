namespace PeopleManagement.Application.Commands.RequireDocumentsCommands.GenerateDocumentUnits
{
    public record GenerateDocumentUnitsResponse(Guid Id) : BaseDTO(Id)
    {
        public static implicit operator GenerateDocumentUnitsResponse(Guid id) => new(id);
    }
}
