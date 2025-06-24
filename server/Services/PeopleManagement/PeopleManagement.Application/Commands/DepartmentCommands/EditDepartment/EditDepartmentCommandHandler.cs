using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.DepartmentAggregate;
using PeopleManagement.Domain.AggregatesModel.DepartmentAggregate.Interfaces;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Infra.Idempotency;

namespace PeopleManagement.Application.Commands.DepartmentCommands.EditDepartment
{
    public class EditDepartmentCommandHandler(IDepartmentRepository departmentRepository) : IRequestHandler<EditDepartmentCommand, EditDepartmentResponse>
    {
        private readonly IDepartmentRepository _departmentRepository = departmentRepository;
        public async Task<EditDepartmentResponse> Handle(EditDepartmentCommand request, CancellationToken cancellationToken)
        {
            var department = await _departmentRepository.FirstOrDefaultAsync(x => x.Id == request.Id && x.CompanyId == request.CompanyId, cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Department), request.Id.ToString()));

            department.Edit(request.Name, request.Description);

            await _departmentRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            return department.Id;
        }

    }

    public class EditDepartmentIdentifiedCommandHandler(IMediator mediator, 
        ILogger<IdentifiedCommandHandler<EditDepartmentCommand, EditDepartmentResponse>> logger, IRequestManager requestManager) 
        : IdentifiedCommandHandler<EditDepartmentCommand, EditDepartmentResponse>(mediator, logger, requestManager)
    {
        protected override EditDepartmentResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
    }

}
