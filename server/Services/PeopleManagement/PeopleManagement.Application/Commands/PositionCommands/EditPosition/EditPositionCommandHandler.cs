
using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.PositionAggregate;
using PeopleManagement.Domain.AggregatesModel.PositionAggregate.Interfaces;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Infra.Idempotency;

namespace PeopleManagement.Application.Commands.PositionCommands.EditPosition
{
    public class EditPositionCommandHandler(IPositionRepository positionRepository) : IRequestHandler<EditPositionCommand, EditPositionResponse>
    {
        private readonly IPositionRepository _positionRepository = positionRepository;
        public async Task<EditPositionResponse> Handle(EditPositionCommand request, CancellationToken cancellationToken)
        {
            var position = await _positionRepository.FirstOrDefaultAsync(x => x.Id == request.Id && x.CompanyId == request.CompanyId, cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Position), request.Id.ToString()));
            position.Edit(request.Name, request.Description, request.CBO);
            await _positionRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
            return position.Id;
        }
    }

    public class EditPositionIdentifiedCommandHandler(IMediator mediator,
        ILogger<IdentifiedCommandHandler<EditPositionCommand, EditPositionResponse>> logger, IRequestManager requestManager)
        : IdentifiedCommandHandler<EditPositionCommand, EditPositionResponse>(mediator, logger, requestManager)
    {
        protected override EditPositionResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
    }
}