using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.SecurityDocumentAggregate.Interfaces;

namespace PeopleManagement.Application.Commands.SecurityDocumentCommands.CreateDocument
{
    public class CreateDocumentCommandHandler : IRequestHandler<CreateDocumentCommand, CreateDocumentResponse>
    {
        private readonly ISecurityDocumentService _securityDocumentService;
        private readonly ISecurityDocumentRepository _securityDocumentRepository;

        public CreateDocumentCommandHandler(ISecurityDocumentService securityDocumentService, ISecurityDocumentRepository securityDocumentRepository)
        {
            _securityDocumentService = securityDocumentService;
            _securityDocumentRepository = securityDocumentRepository;
        }

        public async Task<CreateDocumentResponse> Handle(CreateDocumentCommand request, CancellationToken cancellationToken)
        {
            var document = await _securityDocumentService.CreateDocument(request.SecurityDocumentId, request.EmployeeId, request.CompanyId, request.DocumentDate, cancellationToken);
            await _securityDocumentRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
            
            return document.Id;
        }
    }

    public class CreateDocumentIdentifiedCommandHandler(IMediator mediator, ILogger<IdentifiedCommandHandler<CreateDocumentCommand, CreateDocumentResponse>> logger) : IdentifiedCommandHandler<CreateDocumentCommand, CreateDocumentResponse>(mediator, logger)
    {
        protected override CreateDocumentResponse CreateResultForDuplicateRequest() => new(Guid.Empty);

    }
}
