using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Interfaces;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;

namespace PeopleManagement.Application.Commands.EmployeeCommands.IsRequiredMilitaryDocumentEmployee
{
    public class IsRequiredMilitaryDocumentEmployeeCommandHandler(IEmployeeRepository employeeRepository) : IRequestHandler<IsRequiredMilitaryDocumentEmployeeCommand, IsRequiredMilitaryDocumentEmployeeResponse>
    {
        private readonly IEmployeeRepository _employeeRepository = employeeRepository;

        public async Task<IsRequiredMilitaryDocumentEmployeeResponse> Handle(IsRequiredMilitaryDocumentEmployeeCommand request, CancellationToken cancellationToken)
        {
            var employee = await _employeeRepository.FirstOrDefaultAsync(x => x.Id == request.EmployeeId && x.CompanyId == request.CompanyId, cancellation: cancellationToken)
               ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Employee), request.EmployeeId.ToString()));

            var result = employee.IsRequiredMilitarDocument();

            return new(employee.Id, result);
        }
    }

    public class IsRequiredMilitaryDocumentEmployeeIdentifiedCommandHandler(IMediator mediator, ILogger<IdentifiedCommandHandler<IsRequiredMilitaryDocumentEmployeeCommand, IsRequiredMilitaryDocumentEmployeeResponse>> logger) : IdentifiedCommandHandler<IsRequiredMilitaryDocumentEmployeeCommand, IsRequiredMilitaryDocumentEmployeeResponse>(mediator, logger)
    {
        protected override IsRequiredMilitaryDocumentEmployeeResponse CreateResultForDuplicateRequest() => new(Guid.Empty, false);
    }
}
