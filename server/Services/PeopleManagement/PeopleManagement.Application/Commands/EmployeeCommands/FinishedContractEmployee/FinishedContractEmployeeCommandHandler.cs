using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Application.Commands.Identified;

namespace PeopleManagement.Application.Commands.EmployeeCommands.FinishedContractEmployee
{
    public sealed class FinishedContractEmployeeCommandHandler : IRequestHandler<FinishedContractEmployeeCommand, FinishedContractEmployeeResponse>
    {
        private readonly IEmployeeRepository _employeeRepository;

        public FinishedContractEmployeeCommandHandler(IEmployeeRepository employeeRepository)
        {
            _employeeRepository = employeeRepository;
        }

        public async Task<FinishedContractEmployeeResponse> Handle(FinishedContractEmployeeCommand request, CancellationToken cancellationToken)
        {
            var employee = await _employeeRepository.FirstOrDefaultAsync(x => x.Id == request.EmployeeId && x.CompanyId == request.CompanyId, cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Employee), request.EmployeeId.ToString()));

            employee.FinishedContract(request.FinishDateContract);

            await _employeeRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            return employee.Id;
        }
    }

    public class FinishedContractEmployeeIdentifiedCommandHandler(IMediator mediator, ILogger<IdentifiedCommandHandler<FinishedContractEmployeeCommand, FinishedContractEmployeeResponse>> logger) : IdentifiedCommandHandler<FinishedContractEmployeeCommand, FinishedContractEmployeeResponse>(mediator, logger)
    {
        protected override FinishedContractEmployeeResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
    }
}
