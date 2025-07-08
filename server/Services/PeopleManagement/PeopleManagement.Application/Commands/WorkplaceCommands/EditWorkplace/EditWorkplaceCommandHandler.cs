using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.WorkplaceAggregate;
using PeopleManagement.Domain.AggregatesModel.WorkplaceAggregate.Interfaces;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Infra.Idempotency;
using Address = PeopleManagement.Domain.AggregatesModel.WorkplaceAggregate.Address;

namespace PeopleManagement.Application.Commands.WorkplaceCommands.EditWorkplace
{
    public class EditWorkplaceCommandHandler(IWorkplaceRepository workplaceRepository) : IRequestHandler<EditWorkplaceCommand, EditWorkplaceResponse>
    {
        private readonly IWorkplaceRepository _workplaceRepository = workplaceRepository;
        public async Task<EditWorkplaceResponse> Handle(EditWorkplaceCommand request, CancellationToken cancellationToken)
        {
            var workplace = await _workplaceRepository.FirstOrDefaultAsync(x => x.Id == request.Id && x.CompanyId == request.CompanyId, cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Workplace), request.Id.ToString()));
            workplace.Edit(request.Name, 
                Address.Create(
                    request.Address.ZipCode,
                    request.Address.Street, 
                    request.Address.Number,
                    request.Address.Complement,
                    request.Address.Neighborhood,
                    request.Address.City, 
                    request.Address.State, 
                    request.Address.Country
                ));
            await _workplaceRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
            return workplace.Id;
        }
    }

    public class EditWorkplaceIdentifiedCommandHandler(IMediator mediator, 
        ILogger<IdentifiedCommandHandler<EditWorkplaceCommand, EditWorkplaceResponse>> logger, IRequestManager requestManager) 
        : IdentifiedCommandHandler<EditWorkplaceCommand, EditWorkplaceResponse>(mediator, logger, requestManager)
    {
        protected override EditWorkplaceResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
    }

}
