using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;

namespace PeopleManagement.Application.Commands.DocumentCommands.CheckOutdatedDocumentContent
{
    /// <summary>
    /// Verificação pura: não muta nada, logo não salva e não passa por idempotência.
    /// </summary>
    public class CheckOutdatedDocumentContentCommandHandler(IDocumentService documentService)
        : IRequestHandler<CheckOutdatedDocumentContentCommand, CheckOutdatedDocumentContentResponse>
    {
        private readonly IDocumentService _documentService = documentService;

        public async Task<CheckOutdatedDocumentContentResponse> Handle(CheckOutdatedDocumentContentCommand request, CancellationToken cancellationToken)
        {
            var statuses = await _documentService.CheckOutdatedContent(
                request.Items.Select(x => (x.DocumentUnitId, x.DocumentId, x.EmployeeId)),
                request.CompanyId, cancellationToken);

            var items = statuses
                .Select(x => new OutdatedDocumentContentItem(x.DocumentUnitId, x.IsOutdated, x.CheckFailed))
                .ToList();

            return new CheckOutdatedDocumentContentResponse(items);
        }
    }
}
