namespace PeopleManagement.Application.Commands.PositionCommands.CreatePosition
{
    public record CreatePositionResponse(Guid Id) : BaseDTO(Id)
    {
        public static implicit operator CreatePositionResponse(Guid id) =>
            new(id);
    }

}
