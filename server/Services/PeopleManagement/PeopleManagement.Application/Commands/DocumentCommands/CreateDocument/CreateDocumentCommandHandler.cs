using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Infra.Idempotency;

namespace PeopleManagement.Application.Commands.DocumentCommands.CreateDocument
{
    public class CreateDocumentCommandHandler(IDocumentService documentService, IDocumentRepository documentRepository) : IRequestHandler<CreateDocumentCommand, CreateDocumentResponse>
    {
        private readonly IDocumentService _documentService = documentService;
        private readonly IDocumentRepository _documentRepository = documentRepository;

        public async Task<CreateDocumentResponse> Handle(CreateDocumentCommand request, CancellationToken cancellationToken)
        {
            var document = await _documentService.CreateDocumentUnit(request.DocumentId, request.EmployeeId, request.CompanyId, cancellationToken);
            await _documentRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
            
            return document.Id;
        }
    }

    public class CreateDocumentIdentifiedCommandHandler(IMediator mediator, 
        ILogger<IdentifiedCommandHandler<CreateDocumentCommand, CreateDocumentResponse>> logger, IRequestManager requestManager) 
        : IdentifiedCommandHandler<CreateDocumentCommand, CreateDocumentResponse>(mediator, logger, requestManager)
    {
        protected override CreateDocumentResponse CreateResultForDuplicateRequest() => new(Guid.Empty);

    }
}
