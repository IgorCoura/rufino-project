using Contact = PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Contact;

namespace PeopleManagement.Application.Commands.EmployeeCommands.AlterContactEmployee
{
    public record AlterContactEmployeeCommand(Guid EmployeeId, Guid CompanyId, string CellPhone, string Email) : IRequest<AlterContactEmployeeResponse>
    {
        public Contact ToContact() => Contact.Create(Email, CellPhone);
    }
}
