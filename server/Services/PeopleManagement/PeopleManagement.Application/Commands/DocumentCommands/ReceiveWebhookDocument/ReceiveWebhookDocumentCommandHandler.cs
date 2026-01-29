using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Infra.Idempotency;

namespace PeopleManagement.Application.Commands.DocumentCommands.ReceiveWebhookDocument
{
    public class ReceiveWebhookDocumentCommandHandler(ISignDocumentService signDocumentService, IDocumentRepository documentRepository) : IRequestHandler<ReceiveWebhookDocumentCommand, ReceiveWebhookDocumentResponse>
    {
        private readonly ISignDocumentService _signDocumentService = signDocumentService;
        private readonly IDocumentRepository _documentRepository = documentRepository;

        public async Task<ReceiveWebhookDocumentResponse> Handle(ReceiveWebhookDocumentCommand request, CancellationToken cancellationToken)
        {
            var result = await _signDocumentService.ReceiveWebhookDocument(request.ContentBody, cancellationToken);
            await _documentRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
            return result;
        }
    }
    public class ReceiveDocumentSignedIdentifiedCommandHandler(IMediator mediator, 
        ILogger<IdentifiedCommandHandler<ReceiveWebhookDocumentCommand, ReceiveWebhookDocumentResponse>> logger, IRequestManager requestManager) 
        : IdentifiedCommandHandler<ReceiveWebhookDocumentCommand, ReceiveWebhookDocumentResponse>(mediator, logger, requestManager)
    {
        protected override ReceiveWebhookDocumentResponse CreateResultForDuplicateRequest() => new(Guid.Empty);

    }
}
