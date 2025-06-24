using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.RoleAggregate;
using PeopleManagement.Domain.AggregatesModel.RoleAggregate.Interfaces;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Infra.Idempotency;

namespace PeopleManagement.Application.Commands.RoleCommands.EditRole
{
    public class EditRoleCommandHandler(IRoleRepository roleRepository) : IRequestHandler<EditRoleCommand, EditRoleResponse>
    {
        private readonly IRoleRepository _roleRepository = roleRepository;
        public async Task<EditRoleResponse> Handle(EditRoleCommand request, CancellationToken cancellationToken)
        {
            var role = await _roleRepository.FirstOrDefaultAsync(x => x.Id == request.Id && x.CompanyId == request.CompanyId, cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Role), request.Id.ToString()));
            role.Edit(
                request.Name,
                request.Description, 
                request.CBO, 
                Remuneration.Create(
                    request.Remuneration.PaymentUnit,
                    Currency.Create(
                        request.Remuneration.BaseSalary.Type,
                        request.Remuneration.BaseSalary.Value),
                    request.Remuneration.Description));
            await _roleRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
            return role.Id;
        }
    }

    public class EditRoleIdentifiedCommandHandler(IMediator mediator, 
        ILogger<IdentifiedCommandHandler<EditRoleCommand, EditRoleResponse>> logger, IRequestManager requestManager) 
        : IdentifiedCommandHandler<EditRoleCommand, EditRoleResponse>(mediator, logger, requestManager)
    {
        protected override EditRoleResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
    }

}
