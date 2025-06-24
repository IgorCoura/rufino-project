using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.RoleAggregate;
using PeopleManagement.Domain.AggregatesModel.RoleAggregate.Interfaces;
using PeopleManagement.Infra.Idempotency;

namespace PeopleManagement.Application.Commands.RoleCommands.CreateRole
{
    public class CreateRoleCommandHandler(IRoleRepository roleRepository) : IRequestHandler<CreateRoleCommand, CreateRoleResponse>
    {
        private readonly IRoleRepository _roleRepository = roleRepository;
        public async Task<CreateRoleResponse> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
        {
            var role = Role.Create(
                Guid.NewGuid(),
                request.Name,
                request.Description,
                request.CBO,
                Remuneration.Create(
                    request.Remuneration.PaymentUnit,
                    Currency.Create(
                        request.Remuneration.BaseSalary.Type,
                        request.Remuneration.BaseSalary.Value),
                    request.Remuneration.Description),
                request.PositionId,
                request.CompanyId);

            await _roleRepository.InsertAsync(role, cancellationToken);

            await _roleRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            return role.Id;

        }
    }

    public class CreateRoleIdentifiedCommandHandler(IMediator mediator, 
        ILogger<IdentifiedCommandHandler<CreateRoleCommand, CreateRoleResponse>> logger, 
        IRequestManager requestManager) 
        : IdentifiedCommandHandler<CreateRoleCommand, CreateRoleResponse>(mediator, logger, requestManager)
    {
        protected override CreateRoleResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
}
}
