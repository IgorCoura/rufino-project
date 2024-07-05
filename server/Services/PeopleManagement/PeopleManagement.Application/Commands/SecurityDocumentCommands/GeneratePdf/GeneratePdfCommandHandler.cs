
using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.SecurityDocumentAggregate.Interfaces;

namespace PeopleManagement.Application.Commands.SecurityDocumentCommands.GeneratePdf
{
    public class GeneratePdfCommandHandler : IRequestHandler<GeneratePdfCommand, GeneratePdfResponse>
    {
        private readonly ISecurityDocumentService _securityDocumentService;

        public GeneratePdfCommandHandler(ISecurityDocumentService securityDocumentService)
        {
            _securityDocumentService = securityDocumentService;
        }

        public async Task<GeneratePdfResponse> Handle(GeneratePdfCommand request, CancellationToken cancellationToken)
        {
            var pdf = await _securityDocumentService.GeneratePdf(request.DocumentId, request.SecurityDocumentId, request.EmployeeId, request.CompanyId, cancellationToken);
            return new GeneratePdfResponse(request.DocumentId, pdf);
        }
    }
    public class CreateDocumentIdentifiedCommandHandler(IMediator mediator, ILogger<IdentifiedCommandHandler<GeneratePdfCommand, GeneratePdfResponse>> logger) : IdentifiedCommandHandler<GeneratePdfCommand, GeneratePdfResponse>(mediator, logger)
    {
        protected override GeneratePdfResponse CreateResultForDuplicateRequest() => new(Guid.Empty, []);

    }
}
