using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate.Interfaces;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Infra.Idempotency;

namespace PeopleManagement.Application.Commands.RequireDocumentsCommands.GenerateDocumentUnits
{
    public sealed class GenerateDocumentUnitsCommandHandler(
        IRequireDocumentsRepository requireDocumentsRepository,
        IDocumentService documentService) : IRequestHandler<GenerateDocumentUnitsCommand, GenerateDocumentUnitsResponse>
    {
        private readonly IRequireDocumentsRepository _requireDocumentsRepository = requireDocumentsRepository;
        private readonly IDocumentService _documentService = documentService;

        public async Task<GenerateDocumentUnitsResponse> Handle(GenerateDocumentUnitsCommand request, CancellationToken cancellationToken)
        {
            var requireDocument = await _requireDocumentsRepository.FirstOrDefaultAsync(
                x => x.Id == request.RequireDocumentId && x.CompanyId == request.CompanyId, cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(RequireDocuments), request.RequireDocumentId.ToString()));

            await _documentService.GenerateDocumentUnitsForRequireDocument(request.RequireDocumentId, request.CompanyId, cancellationToken);

            return requireDocument.Id;
        }

        public class GenerateDocumentUnitsIdentifiedCommandHandler(
            IMediator mediator,
            ILogger<IdentifiedCommandHandler<GenerateDocumentUnitsCommand, GenerateDocumentUnitsResponse>> logger,
            IRequestManager requestManager)
            : IdentifiedCommandHandler<GenerateDocumentUnitsCommand, GenerateDocumentUnitsResponse>(mediator, logger, requestManager)
        {
            protected override GenerateDocumentUnitsResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
        }
    }
}
