using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;

namespace PeopleManagement.Application.Commands.DocumentCommands.CreateDocument
{
    public class CreateDocumentCommandHandler(IDocumentService documentService, IDocumentRepository documentRepository) : IRequestHandler<CreateDocumentCommand, CreateDocumentResponse>
    {
        private readonly IDocumentService _documentService = documentService;
        private readonly IDocumentRepository _documentRepository = documentRepository;

        public async Task<CreateDocumentResponse> Handle(CreateDocumentCommand request, CancellationToken cancellationToken)
        {
            var document = await _documentService.CreateDocument(request.DocumentId, request.EmployeeId, request.CompanyId, request.DocumentDate, cancellationToken);
            await _documentRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
            
            return document.Id;
        }
    }

    public class CreateDocumentIdentifiedCommandHandler(IMediator mediator, ILogger<IdentifiedCommandHandler<CreateDocumentCommand, CreateDocumentResponse>> logger) : IdentifiedCommandHandler<CreateDocumentCommand, CreateDocumentResponse>(mediator, logger)
    {
        protected override CreateDocumentResponse CreateResultForDuplicateRequest() => new(Guid.Empty);

    }
}
