namespace PeopleManagement.Application.Commands.RequireDocumentsCommands.CreateRequireSecurityDocuments
{
    public record CreateRequireDocumentsResponse(Guid Id) : BaseDTO(Id)
    {
        public static implicit operator CreateRequireDocumentsResponse(Guid id) => new(id);
    }
}
