using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Infra.Idempotency;

namespace PeopleManagement.Application.Commands.DocumentCommands.RefreshDocumentContent
{
    public class RefreshDocumentContentCommandHandler(
        IDocumentService documentService,
        IDocumentRepository documentRepository
    ) : IRequestHandler<RefreshDocumentContentCommand, RefreshDocumentContentResponse>
    {
        private readonly IDocumentService _documentService = documentService;
        private readonly IDocumentRepository _documentRepository = documentRepository;

        public async Task<RefreshDocumentContentResponse> Handle(RefreshDocumentContentCommand request, CancellationToken cancellationToken)
        {
            var items = request.Items.ToList();

            await _documentService.RefreshDocumentUnitContent(
                items.Select(x => (x.DocumentUnitId, x.DocumentId, x.EmployeeId)),
                request.CompanyId, cancellationToken);

            await _documentRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            return new RefreshDocumentContentResponse(items.Count);
        }
    }

    public class RefreshDocumentContentIdentifiedCommandHandler(
        IMediator mediator,
        ILogger<IdentifiedCommandHandler<RefreshDocumentContentCommand, RefreshDocumentContentResponse>> logger,
        IRequestManager requestManager
    ) : IdentifiedCommandHandler<RefreshDocumentContentCommand, RefreshDocumentContentResponse>(mediator, logger, requestManager)
    {
        protected override RefreshDocumentContentResponse CreateResultForDuplicateRequest() => new(0);
    }
}
