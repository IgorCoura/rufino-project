namespace PeopleManagement.Application.Commands.ArchiveCommands.NotApplicable
{
    public record FileNotApplicableResponse(Guid Id) : BaseDTO(Id)
    {
        public static implicit operator FileNotApplicableResponse(Guid id) => new(id);
    }
}
