namespace PeopleManagement.Application.Commands.RequireDocumentsCommands.EditRequireSecurityDocuments
{
    public record EditRequireDocumentsResponse(Guid Id) : BaseDTO(Id)
    {
        public static implicit operator EditRequireDocumentsResponse(Guid id) => new(id);
    }
}
