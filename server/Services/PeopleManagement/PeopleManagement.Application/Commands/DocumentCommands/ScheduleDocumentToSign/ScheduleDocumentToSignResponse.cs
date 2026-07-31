namespace PeopleManagement.Application.Commands.DocumentCommands.ScheduleDocumentToSign
{
    public record ScheduleDocumentToSignResponse(Guid Id) : BaseDTO(Id)
    {
        public static implicit operator ScheduleDocumentToSignResponse(Guid id) => new(id);
    }
}
