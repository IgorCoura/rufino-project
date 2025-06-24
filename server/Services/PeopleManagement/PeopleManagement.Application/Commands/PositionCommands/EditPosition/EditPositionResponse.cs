namespace PeopleManagement.Application.Commands.PositionCommands.EditPosition
{
    public record EditPositionResponse(Guid Id) : BaseDTO(Id)
    {
        public static implicit operator EditPositionResponse(Guid id) =>
            new(id);
    }
}
