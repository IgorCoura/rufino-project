using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Infra.Idempotency;

namespace PeopleManagement.Application.Commands.DocumentCommands.BatchUpdateDocumentUnitDate
{
    public class BatchUpdateDocumentUnitDateCommandHandler(
        IDocumentService documentService,
        IDocumentRepository documentRepository
    ) : IRequestHandler<BatchUpdateDocumentUnitDateCommand, BatchUpdateDocumentUnitDateResponse>
    {
        private readonly IDocumentService _documentService = documentService;
        private readonly IDocumentRepository _documentRepository = documentRepository;

        public async Task<BatchUpdateDocumentUnitDateResponse> Handle(BatchUpdateDocumentUnitDateCommand request, CancellationToken cancellationToken)
        {
            var updatedCount = 0;

            foreach (var item in request.Items)
            {
                await _documentService.UpdateDocumentUnitDetails(
                    item.DocumentUnitId, item.DocumentId, item.EmployeeId,
                    request.CompanyId, request.Date, cancellationToken);

                updatedCount++;
            }

            await _documentRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            return new BatchUpdateDocumentUnitDateResponse(updatedCount);
        }
    }

    public class BatchUpdateDocumentUnitDateIdentifiedCommandHandler(
        IMediator mediator,
        ILogger<IdentifiedCommandHandler<BatchUpdateDocumentUnitDateCommand, BatchUpdateDocumentUnitDateResponse>> logger,
        IRequestManager requestManager
    ) : IdentifiedCommandHandler<BatchUpdateDocumentUnitDateCommand, BatchUpdateDocumentUnitDateResponse>(mediator, logger, requestManager)
    {
        protected override BatchUpdateDocumentUnitDateResponse CreateResultForDuplicateRequest()
            => new(0);
    }
}
