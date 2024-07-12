namespace PeopleManagement.Application.Commands.RequireSecurityDocumentsCommands.CreateRequireSecurityDocuments
{
    public record CreateRequireSecurityDocumentsResponse(Guid Id) : BaseDTO(Id)
    {
        public static implicit operator CreateRequireSecurityDocumentsResponse(Guid id) => new(id);
    }
}
