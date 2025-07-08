
using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Infra.Idempotency;
using PuppeteerSharp;

namespace PeopleManagement.Application.Commands.DocumentCommands.InsertDocument
{
    public class InsertDocumentCommandHandler(IDocumentService documentService, IDocumentRepository documentRepository) : IRequestHandler<InsertDocumentCommand, InsertDocumentResponse>
    {
        private readonly IDocumentService _documentService = documentService;
        private readonly IDocumentRepository _documentRepository = documentRepository;

        public async Task<InsertDocumentResponse> Handle(InsertDocumentCommand request, CancellationToken cancellationToken)
        {
            await _documentService.InsertFileWithoutRequireValidation(request.DocumentUnitId, request.DocumentId, 
                request.EmployeeId, request.CompanyId, request.Extension, request.Stream, cancellationToken);

            await _documentRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            return request.DocumentUnitId;
        }
    }

    public class InsertDocumentIdentifiedCommandHandler(IMediator mediator, 
        ILogger<IdentifiedCommandHandler<InsertDocumentCommand, InsertDocumentResponse>> logger, IRequestManager requestManager) 
        : IdentifiedCommandHandler<InsertDocumentCommand, InsertDocumentResponse>(mediator, logger, requestManager)
    {
        protected override InsertDocumentResponse CreateResultForDuplicateRequest() => new(Guid.Empty);

    }
}
