namespace PeopleManagement.Application.Commands.WorkplaceCommands.CreateWorkplace
{
    public record CreateWorkplaceResponse(Guid Id) : BaseDTO(Id)
    {
        public static implicit operator CreateWorkplaceResponse(Guid Id) => new(Id);
    }
}
