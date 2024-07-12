
using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.SecurityDocumentAggregate.Interfaces;
using PuppeteerSharp;

namespace PeopleManagement.Application.Commands.SecurityDocumentCommands.InsertDocument
{
    public class InsertDocumentCommandHandler(ISecurityDocumentService securityDocumentService, ISecurityDocumentRepository securityDocumentRepository) : IRequestHandler<InsertDocumentCommand, InsertDocumentResponse>
    {
        private readonly ISecurityDocumentService _securityDocumentService = securityDocumentService;
        private readonly ISecurityDocumentRepository _securityDocumentRepository = securityDocumentRepository;

        public async Task<InsertDocumentResponse> Handle(InsertDocumentCommand request, CancellationToken cancellationToken)
        {
            await _securityDocumentService.InsertFileWithoutRequireValidation(request.DocumentId, request.SecurityDocumentId, 
                request.EmployeeId, request.CompanyId, request.Extension, request.Stream, cancellationToken);

            await _securityDocumentRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            return request.DocumentId;
        }
    }

    public class InsertDocumentIdentifiedCommandHandler(IMediator mediator, ILogger<IdentifiedCommandHandler<InsertDocumentCommand, InsertDocumentResponse>> logger) : IdentifiedCommandHandler<InsertDocumentCommand, InsertDocumentResponse>(mediator, logger)
    {
        protected override InsertDocumentResponse CreateResultForDuplicateRequest() => new(Guid.Empty);

    }
}
