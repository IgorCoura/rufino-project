using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Interfaces;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Application.Commands.Identified;

namespace PeopleManagement.Application.Commands.EmployeeCommands.NewContractEmployee
{
    public sealed class NewContractEmployeeCommandHandler : IRequestHandler<NewContractEmployeeCommand, NewContractEmployeeResponse>
    {
        private readonly IEmployeeRepository _employeeRepository;

        public NewContractEmployeeCommandHandler(IEmployeeRepository employeeRepository)
        {
            _employeeRepository = employeeRepository;
        }
        public async Task<NewContractEmployeeResponse> Handle(NewContractEmployeeCommand request, CancellationToken cancellationToken)
        {
            var employee = await _employeeRepository.FirstOrDefaultAsync(x => x.Id == request.EmployeeId && x.CompanyId == request.CompanyId, cancellation: cancellationToken)
               ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Employee), request.EmployeeId.ToString()));

            employee.CreateContract(request.InitDateContract, request.ContractType);
            await _employeeRepository.UnitOfWork.SaveChangesAsync();
            return new NewContractEmployeeResponse(employee.Id);
        }
    }

    public class NewContractEmployeeIdentifiedCommandHandler(IMediator mediator, ILogger<IdentifiedCommandHandler<NewContractEmployeeCommand, NewContractEmployeeResponse>> logger) : IdentifiedCommandHandler<NewContractEmployeeCommand, NewContractEmployeeResponse>(mediator, logger)
    {
        protected override NewContractEmployeeResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
    }

}
