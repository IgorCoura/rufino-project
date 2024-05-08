using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;

namespace PeopleManagement.Application.Commands.EmployeeCommands.CreateEmployee
{
    public sealed class CreateEmployeeCommandHandler : IRequestHandler<CreateEmployeeCommand, CreateEmployeeCommandResponse>
    {
        private readonly IEmployeeRepository _employeeRepository;

        public CreateEmployeeCommandHandler(IEmployeeRepository employeeRepository)
        {
            _employeeRepository = employeeRepository;
        }

        public async Task<CreateEmployeeCommandResponse> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
        {
            var id = Guid.NewGuid();
            var employee = Employee.Create(id, request.CompanyId, request.Name);

            var result = await _employeeRepository.InsertAsync(employee);
            await _employeeRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
            return result.Id;
        }
    }
}
