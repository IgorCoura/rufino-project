
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
            var employee = await _employeeRepository.FirstOrDefaultAsync(request.Id, cancellation: cancellationToken) ??
                throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Employee), request.Id.ToString()));

            var deficiency = Deficiency.Create(request.Deficiency.Observation, request.Deficiency.Disability.Select(x => Disability.FromValue<Disability>(x)).ToArray());

            employee.PersonalInfo = PersonalInfo.Create(
                    deficiency,
                    MaritalStatus.FromValue<MaritalStatus>(request.MaritalStatus),
                    Gender.FromValue<Gender>(request.Gender),
                    Ethinicity.FromValue<Ethinicity>(request.Ethinicity),
                    EducationLevel.FromValue<EducationLevel>(request.EducationLevel)
                );

            await _employeeRepository.UnitOfWork.SaveChangesAsync();

            return employee.Id;
        }
    }
}
