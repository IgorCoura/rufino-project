using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Interfaces;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;

namespace PeopleManagement.Application.Commands.EmployeeCommands.RemoveDependentEmployee
{
    public sealed class RemoveDependentEmployeeCommandHandler : IRequestHandler<RemoveDependentEmployeeCommand, RemoveDependentEmployeeResponse>
    {
        private readonly IEmployeeRepository _employeeRepository;

        public RemoveDependentEmployeeCommandHandler(IEmployeeRepository employeeRepository)
        {
            _employeeRepository = employeeRepository;
        }

        public async Task<RemoveDependentEmployeeResponse> Handle(RemoveDependentEmployeeCommand request, CancellationToken cancellationToken)
        {

            var employee = await _employeeRepository.FirstOrDefaultAsync(x => x.Id == request.EmployeeId && x.CompanyId == request.CompanyId, cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Employee), request.EmployeeId.ToString()));


            employee.RemoveDependent(request.NameDepedent);

            await _employeeRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            return employee.Id;
        }
    }

    public class RemoveDependentEmployeeIdentifiedCommandHandler(IMediator mediator, ILogger<IdentifiedCommandHandler<RemoveDependentEmployeeCommand, RemoveDependentEmployeeResponse>> logger) : IdentifiedCommandHandler<RemoveDependentEmployeeCommand, RemoveDependentEmployeeResponse>(mediator, logger)
    {
        protected override RemoveDependentEmployeeResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
    }
}
