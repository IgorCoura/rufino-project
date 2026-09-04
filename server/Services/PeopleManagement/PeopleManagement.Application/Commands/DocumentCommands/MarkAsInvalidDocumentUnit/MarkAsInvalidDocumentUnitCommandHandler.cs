using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Infra.Idempotency;

namespace PeopleManagement.Application.Commands.DocumentCommands.MarkAsInvalidDocumentUnit
{
    public class MarkAsInvalidDocumentUnitCommandHandler(IDocumentService documentService, IDocumentRepository documentRepository)
        : IRequestHandler<MarkAsInvalidDocumentUnitCommand, MarkAsInvalidDocumentUnitResponse>
    {
        private readonly IDocumentService _documentService = documentService;
        private readonly IDocumentRepository _documentRepository = documentRepository;

        public async Task<MarkAsInvalidDocumentUnitResponse> Handle(MarkAsInvalidDocumentUnitCommand request, CancellationToken cancellationToken)
        {
            // Invalidar deixa a exigência descoberta, então o serviço já devolve a pendente substituta — vale
            // tanto para a recusa de validação quanto para a invalidação direta pelo RH.
            var replacement = await _documentService.InvalidateDocumentUnit(request.DocumentUnitId, request.DocumentId,
                request.EmployeeId, request.CompanyId, cancellationToken);

            await _documentRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            return replacement.Id;
        }
    }

    public class MarkAsInvalidDocumentUnitIdentifiedCommandHandler(IMediator mediator,
        ILogger<IdentifiedCommandHandler<MarkAsInvalidDocumentUnitCommand, MarkAsInvalidDocumentUnitResponse>> logger, IRequestManager requestManager)
        : IdentifiedCommandHandler<MarkAsInvalidDocumentUnitCommand, MarkAsInvalidDocumentUnitResponse>(mediator, logger, requestManager)
    {
        protected override MarkAsInvalidDocumentUnitResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
    }
}
