
using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Infra.Idempotency;

namespace PeopleManagement.Application.Commands.DocumentCommands.GeneratePdf
{
    public class GeneratePdfCommandHandler(IDocumentService documentService) : IRequestHandler<GeneratePdfCommand, GeneratePdfResponse>
    {
        private readonly IDocumentService _documentService = documentService;

        public async Task<GeneratePdfResponse> Handle(GeneratePdfCommand request, CancellationToken cancellationToken)
        {
            var pdf = await _documentService.GeneratePdf(request.DocumentUnitId, request.DocumentId, request.EmployeeId, request.CompanyId, cancellationToken);
            return new GeneratePdfResponse(request.DocumentUnitId, pdf);
        }
    }
    public class CreateDocumentIdentifiedCommandHandler(IMediator mediator, 
        ILogger<IdentifiedCommandHandler<GeneratePdfCommand, GeneratePdfResponse>> logger, IRequestManager requestManager) 
        : IdentifiedCommandHandler<GeneratePdfCommand, GeneratePdfResponse>(mediator, logger, requestManager)
    {
        protected override GeneratePdfResponse CreateResultForDuplicateRequest() => new(Guid.Empty, []);

    }
}
