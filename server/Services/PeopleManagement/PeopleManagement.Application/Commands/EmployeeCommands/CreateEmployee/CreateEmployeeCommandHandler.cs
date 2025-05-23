using PeopleManagement.Application.Commands.EmployeeCommands.CreateDependentEmployee;
using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Interfaces;
using PeopleManagement.Infra.Idempotency;

namespace PeopleManagement.Application.Commands.EmployeeCommands.CreateEmployee
{
    public sealed class CreateEmployeeCommandHandler : IRequestHandler<CreateEmployeeCommand, CreateEmployeeResponse>
    {
        private readonly IEmployeeRepository _employeeRepository;

        public CreateEmployeeCommandHandler(IEmployeeRepository employeeRepository)
        {
            _employeeRepository = employeeRepository;
        }

        public async Task<CreateEmployeeResponse> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
        {
            var id = Guid.NewGuid();
            var employee = request.ToEmployee(id);

            var result = await _employeeRepository.InsertAsync(employee, cancellationToken);
            await _employeeRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
            return result.Id;
        }
    }

    public class CreateEmployeeIdentifiedCommandHandler(IMediator mediator, 
        ILogger<IdentifiedCommandHandler<CreateEmployeeCommand, CreateEmployeeResponse>> logger, IRequestManager requestManager) 
        : IdentifiedCommandHandler<CreateEmployeeCommand, CreateEmployeeResponse>(mediator, logger, requestManager)
    {
        protected override CreateEmployeeResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
    }
}
