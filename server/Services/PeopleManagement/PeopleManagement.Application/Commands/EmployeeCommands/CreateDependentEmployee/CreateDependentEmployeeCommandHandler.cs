
using PeopleManagement.Application.Commands.EmployeeCommands.AlterWorkPlaceEmployee;
using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;

namespace PeopleManagement.Application.Commands.EmployeeCommands.CreateDependentEmployee
{
    public class CreateDependentEmployeeCommandHandler : IRequestHandler<CreateDependentEmployeeCommand, CreateDependentEmployeeResponse>
    {
        private readonly IEmployeeRepository _employeeRepository;

        public CreateDependentEmployeeCommandHandler(IEmployeeRepository employeeRepository)
        {
            _employeeRepository = employeeRepository;
        }

        public async Task<CreateDependentEmployeeResponse> Handle(CreateDependentEmployeeCommand request, CancellationToken cancellationToken)
        {
            var employee = await _employeeRepository.FirstOrDefaultAsync(x => x.Id == request.EmployeeId && x.CompanyId == request.CompanyId, cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Employee), request.EmployeeId.ToString()));

            var idCard = IdCard.Create(
                request.IdCard.Cpf, 
                request.IdCard.MotherName, 
                request.IdCard.FatherName, 
                request.IdCard.BirthCity, 
                request.IdCard.BirthState, 
                request.IdCard.Nacionality, 
                request.IdCard.DateOfBirth);

            var dependent = Dependent.Create(
                request.Name,
                idCard,
                request.Gender,
                request.DependecyType);

            employee.AddDependent(dependent);

            await _employeeRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            return employee.Id;
        }
    }

    public class CreateDependentEmployeeIdentifiedCommandHandler(IMediator mediator, ILogger<IdentifiedCommandHandler<CreateDependentEmployeeCommand, CreateDependentEmployeeResponse>> logger) : IdentifiedCommandHandler<CreateDependentEmployeeCommand, CreateDependentEmployeeResponse>(mediator, logger)
    {
        protected override CreateDependentEmployeeResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
    }
}
