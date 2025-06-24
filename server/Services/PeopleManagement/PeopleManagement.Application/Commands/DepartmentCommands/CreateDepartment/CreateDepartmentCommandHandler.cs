
using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.DepartmentAggregate;
using PeopleManagement.Domain.AggregatesModel.DepartmentAggregate.Interfaces;
using PeopleManagement.Infra.Idempotency;

namespace PeopleManagement.Application.Commands.DepartmentCommands.CreateDepartment
{
    public class CreateDepartmentCommandHandler(IDepartmentRepository departmentRepository) : IRequestHandler<CreateDepartmentCommand, CreateDepartmentResponse>
    {
        private readonly IDepartmentRepository _departmentRepository = departmentRepository;
        public async Task<CreateDepartmentResponse> Handle(CreateDepartmentCommand request, CancellationToken cancellationToken)
        {
            var department = Department.Create(Guid.NewGuid(), request.Name, request.Description, request.CompanyId);
            await _departmentRepository.InsertAsync(department, cancellationToken);
            await _departmentRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            return department.Id;
        }
    }

    public class CreateDepartmentIdentifiedCommandHandler(
        IMediator mediator, 
        ILogger<IdentifiedCommandHandler<CreateDepartmentCommand, CreateDepartmentResponse>> logger, 
        IRequestManager requestManager) 
        : IdentifiedCommandHandler<CreateDepartmentCommand, CreateDepartmentResponse>(mediator, logger, requestManager)
    {
        protected override CreateDepartmentResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
    }
}
