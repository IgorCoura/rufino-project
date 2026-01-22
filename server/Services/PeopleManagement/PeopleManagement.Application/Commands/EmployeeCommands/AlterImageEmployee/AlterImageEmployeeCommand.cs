using PeopleManagement.Application.Commands.EmployeeCommands.AlterMedicalAdmissionExamEmployee;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;

namespace PeopleManagement.Application.Commands.EmployeeCommands.AlterImageEmployee
{
    public record AlterImageEmployeeCommand(Guid EmployeeId, Guid CompanyId, string Extension, Stream Stream) : IRequest<AlterImageEmployeeResponse>
    {
    }

    public record AlterImageEmployeeModel(Guid EmployeeId)
    {
        public AlterImageEmployeeCommand ToCommand(Guid company, string extension, Stream stream) => new(EmployeeId, company, extension, stream);
    }
}
