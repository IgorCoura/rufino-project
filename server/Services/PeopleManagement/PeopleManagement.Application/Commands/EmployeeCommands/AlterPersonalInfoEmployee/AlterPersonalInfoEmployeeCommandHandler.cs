
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Domain.ErrorTools;

namespace PeopleManagement.Application.Commands.EmployeeCommands.AlterPersonalInfoEmployee
{
    public sealed class AlterPersonalInfoEmployeeCommandHandler : IRequestHandler<AlterPersonalInfoEmployeeCommand, AlterPersonalInfoEmployeeResponse>
    {
        private readonly IEmployeeRepository _employeeRepository;

        public AlterPersonalInfoEmployeeCommandHandler(IEmployeeRepository employeeRepository)
        {
            _employeeRepository = employeeRepository;
        }

        public async Task<AlterPersonalInfoEmployeeResponse> Handle(AlterPersonalInfoEmployeeCommand request, CancellationToken cancellationToken)
        {
            var employee = await _employeeRepository.FirstOrDefaultAsync(x => x.Id == request.EmployeeId && x.CompanyId == request.CompanyId, cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Employee), request.EmployeeId.ToString()));

            var deficiency = Deficiency.Create(request.Deficiency.Observation, request.Deficiency.Disability.Select(x => Disability.FromValue<Disability>(x)).ToArray());

            employee.PersonalInfo = PersonalInfo.Create(
                    deficiency,
                    request.MaritalStatus,
                    request.Gender,
                    request.Ethinicity,
                    request.EducationLevel
                );

            await _employeeRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            return employee.Id;
        }
    }
}
