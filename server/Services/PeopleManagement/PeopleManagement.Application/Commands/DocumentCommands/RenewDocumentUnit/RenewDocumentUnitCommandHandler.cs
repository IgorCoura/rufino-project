using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Infra.Idempotency;

namespace PeopleManagement.Application.Commands.DocumentCommands.RenewDocumentUnit
{
    public class RenewDocumentUnitCommandHandler(IDocumentService documentService, IDocumentRepository documentRepository)
        : IRequestHandler<RenewDocumentUnitCommand, RenewDocumentUnitResponse>
    {
        private readonly IDocumentService _documentService = documentService;
        private readonly IDocumentRepository _documentRepository = documentRepository;

        public async Task<RenewDocumentUnitResponse> Handle(RenewDocumentUnitCommand request, CancellationToken cancellationToken)
        {
            // Passa pelo serviço, e não direto pelo agregado, porque a cota de renovações e a regra de
            // competência são do template — outro aggregate.
            var replacement = await _documentService.RenewDocumentUnit(request.DocumentUnitId, request.DocumentId,
                request.EmployeeId, request.CompanyId, cancellationToken);

            await _documentRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            return replacement.Id;
        }
    }

    public class RenewDocumentUnitIdentifiedCommandHandler(IMediator mediator,
        ILogger<IdentifiedCommandHandler<RenewDocumentUnitCommand, RenewDocumentUnitResponse>> logger, IRequestManager requestManager)
        : IdentifiedCommandHandler<RenewDocumentUnitCommand, RenewDocumentUnitResponse>(mediator, logger, requestManager)
    {
        protected override RenewDocumentUnitResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
    }
}
