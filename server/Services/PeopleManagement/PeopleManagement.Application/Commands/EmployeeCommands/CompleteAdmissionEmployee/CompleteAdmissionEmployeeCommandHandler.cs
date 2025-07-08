using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Interfaces;
using PeopleManagement.Infra.Idempotency;

namespace PeopleManagement.Application.Commands.EmployeeCommands.CompleteAdmissionEmployee
{
    public sealed class CompleteAdmissionEmployeeCommandHandler : IRequestHandler<CompleteAdmissionEmployeeCommand, CompleteAdmissionEmployeeResponse>
    {
        private readonly ICompleteAdmissionService _completeAdmissionService;
        private readonly IEmployeeRepository _employeeRepository;

        public CompleteAdmissionEmployeeCommandHandler(ICompleteAdmissionService completeAdmissionService, IEmployeeRepository employeeRepository)
        {
            _completeAdmissionService = completeAdmissionService;
            _employeeRepository = employeeRepository;
        }

        public async Task<CompleteAdmissionEmployeeResponse> Handle(CompleteAdmissionEmployeeCommand request, CancellationToken cancellationToken)
        {
            var employee = await _completeAdmissionService.CompleteAdmission(request.EmployeeId, request.CompanyId, 
                request.Registration, request.dateInit, request.ContractType, cancellationToken);

            await _employeeRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            return employee.Id;
        }
    }

    public class CompleteAdmissionEmployeeIdentifiedCommandHandler(IMediator mediator, 
        ILogger<IdentifiedCommandHandler<CompleteAdmissionEmployeeCommand, CompleteAdmissionEmployeeResponse>> logger, IRequestManager requestManager) 
        : IdentifiedCommandHandler<CompleteAdmissionEmployeeCommand, CompleteAdmissionEmployeeResponse>(mediator, logger, requestManager)
    {
        protected override CompleteAdmissionEmployeeResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
    }
}
