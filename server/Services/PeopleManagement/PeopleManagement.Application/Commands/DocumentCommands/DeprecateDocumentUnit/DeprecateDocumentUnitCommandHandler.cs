using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Infra.Idempotency;

namespace PeopleManagement.Application.Commands.DocumentCommands.DeprecateDocumentUnit
{
    public class DeprecateDocumentUnitCommandHandler(IDocumentService documentService, IDocumentRepository documentRepository)
        : IRequestHandler<DeprecateDocumentUnitCommand, DeprecateDocumentUnitResponse>
    {
        private readonly IDocumentService _documentService = documentService;
        private readonly IDocumentRepository _documentRepository = documentRepository;

        public async Task<DeprecateDocumentUnitResponse> Handle(DeprecateDocumentUnitCommand request, CancellationToken cancellationToken)
        {
            // Passa pelo serviço, e não direto pelo agregado, porque a pendente substituta precisa da regra de
            // competência do template.
            var replacement = await _documentService.DeprecateDocumentUnit(request.DocumentUnitId, request.DocumentId,
                request.EmployeeId, request.CompanyId, cancellationToken);

            await _documentRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            return replacement.Id;
        }
    }

    public class DeprecateDocumentUnitIdentifiedCommandHandler(IMediator mediator,
        ILogger<IdentifiedCommandHandler<DeprecateDocumentUnitCommand, DeprecateDocumentUnitResponse>> logger, IRequestManager requestManager)
        : IdentifiedCommandHandler<DeprecateDocumentUnitCommand, DeprecateDocumentUnitResponse>(mediator, logger, requestManager)
    {
        protected override DeprecateDocumentUnitResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
    }
}
