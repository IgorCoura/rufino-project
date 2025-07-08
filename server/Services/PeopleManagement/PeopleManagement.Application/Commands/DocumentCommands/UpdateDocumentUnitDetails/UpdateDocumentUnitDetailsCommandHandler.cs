using PeopleManagement.Application.Commands.DocumentCommands.InsertDocumentToSign;
using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Infra.Idempotency;

namespace PeopleManagement.Application.Commands.DocumentCommands.UpdateDocumentUnitDetails
{
    public class UpdateDocumentUnitDetailsCommandHandler(IDocumentService documentService, IDocumentRepository documentRepository) : 
        IRequestHandler<UpdateDocumentUnitDetailsCommand, UpdateDocumentUnitDetailsResponse>
    {
        private readonly IDocumentService _documentService = documentService;
        private readonly IDocumentRepository _documentRepository = documentRepository;
        public async Task<UpdateDocumentUnitDetailsResponse> Handle(UpdateDocumentUnitDetailsCommand request, CancellationToken cancellationToken)
        {
            
            var result = await _documentService.UpdateDocumentUnitDetails(request.DocumentUnitId, request.DocumentId, request.EmployeeId,
            request.CompanyId, request.DocumentUnitDate, cancellationToken);
            await _documentRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
            return result.Id;
           
        }
    }

    public class UpdateDocumentUnitDetailsIdentifiedCommandHandler(IMediator mediator, 
        ILogger<IdentifiedCommandHandler<UpdateDocumentUnitDetailsCommand, UpdateDocumentUnitDetailsResponse>> logger, IRequestManager requestManager) 
        : IdentifiedCommandHandler<UpdateDocumentUnitDetailsCommand, UpdateDocumentUnitDetailsResponse>(mediator, logger, requestManager)
    {
        protected override UpdateDocumentUnitDetailsResponse CreateResultForDuplicateRequest() => new(Guid.Empty);

    }
}
