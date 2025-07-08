
using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.PositionAggregate.Interfaces;
using PeopleManagement.Infra.Idempotency;

namespace PeopleManagement.Application.Commands.PositionCommands.CreatePosition
{
    public class CreatePositionCommandHandler(IPositionRepository positionRepository) : IRequestHandler<CreatePositionCommand, CreatePositionResponse>
    {
        private readonly IPositionRepository _positionRepository = positionRepository;
        public async Task<CreatePositionResponse> Handle(CreatePositionCommand request, CancellationToken cancellationToken)
        {
            var position = Domain.AggregatesModel.PositionAggregate.Position.Create(
                Guid.NewGuid(), 
                request.Name, 
                request.Description,
                request.CBO,
                request.DepartmentId,
                request.CompanyId);

            await _positionRepository.InsertAsync(position, cancellationToken);

            await _positionRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            return position.Id;

        }
    }
    public class CreatePositionIdentifiedCommandHandler(IMediator mediator, 
        ILogger<IdentifiedCommandHandler<CreatePositionCommand, CreatePositionResponse>> logger, 
        IRequestManager requestManager) 
        : IdentifiedCommandHandler<CreatePositionCommand, CreatePositionResponse>(mediator, logger, requestManager)
    {
        protected override CreatePositionResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
    }
}
