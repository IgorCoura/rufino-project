using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Interfaces;

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
  
            var employee = await _employeeRepository.FirstOrDefaultAsync(x => x.Id == request.EmployeeId && x.CompanyId == request.CompanyId, cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Employee), request.EmployeeId.ToString()));

            var dependent = request.CurrentDependent.ToDependent();

            employee.AlterDependet(request.OldName, dependent);                

            await _employeeRepository.UnitOfWork.SaveChangesAsync(cancellationToken); 

            return employee.Id;
        }
    }

    public class AlterDependentEmployeeIdentifiedCommandHandler(IMediator mediator, ILogger<IdentifiedCommandHandler<AlterDependentEmployeeCommand, AlterDependentEmployeeResponse>> logger) : IdentifiedCommandHandler<AlterDependentEmployeeCommand, AlterDependentEmployeeResponse>(mediator, logger)
    {
        protected override AlterDependentEmployeeResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
    }
}



