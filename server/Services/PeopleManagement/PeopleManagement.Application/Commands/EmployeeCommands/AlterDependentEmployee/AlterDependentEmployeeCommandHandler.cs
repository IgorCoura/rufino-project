using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Domain.ErrorTools;

namespace PeopleManagement.Application.Commands.EmployeeCommands.AlterDependentEmployee
{
    public sealed class AlterDependentEmployeeCommandHandler : IRequestHandler<AlterDependentEmployeeCommand, AlterDependentEmployeeResponse>
    {
        private readonly IEmployeeRepository _employeeRepository;

        public AlterDependentEmployeeCommandHandler(IEmployeeRepository employeeRepository)
        {
            _employeeRepository = employeeRepository;
        }

        public async Task<AlterDependentEmployeeResponse> Handle(AlterDependentEmployeeCommand request, CancellationToken cancellationToken)
        {
            var employee = await _employeeRepository.FirstOrDefaultAsync(request.Id, cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Employee), request.Id.ToString()));

            var idCard = IdCard.Create(
                request.CurrentDepentent.IdCard.Cpf,
                request.CurrentDepentent.IdCard.MotherName,
                request.CurrentDepentent.IdCard.FatherName,
                request.CurrentDepentent.IdCard.BirthCity,
                request.CurrentDepentent.IdCard.BirthState,
                request.CurrentDepentent.IdCard.Nacionality,
                request.CurrentDepentent.IdCard.DateOfBirth);

            var dependent = Dependent.Create(
                request.CurrentDepentent.Name,
                idCard,
                Gender.FromValue<Gender>(request.CurrentDepentent.Gender),
                DependencyType.FromValue<DependencyType>(request.CurrentDepentent.DependecyType));

            employee.AlterDependet(request.OldName, dependent);

            await _employeeRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            return employee.Id;
        }
    }
}
