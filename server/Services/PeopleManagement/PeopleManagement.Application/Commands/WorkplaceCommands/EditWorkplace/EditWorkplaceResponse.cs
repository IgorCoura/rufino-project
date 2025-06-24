namespace PeopleManagement.Application.Commands.WorkplaceCommands.EditWorkplace
{
    public record EditWorkplaceResponse(Guid Id) : BaseDTO(Id)
    {
        public static implicit operator EditWorkplaceResponse(Guid Id) => new(Id);
    }

}
