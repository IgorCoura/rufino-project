using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;

namespace PeopleManagement.Application.Commands.DocumentCommands.GenerateDocumentToSign
{
    public class GenerateDocumentToSignCommandHandler(ISignDocumentService signDocumentService, IDocumentRepository documentRepository) : IRequestHandler<GenerateDocumentToSignCommand, GenerateDocumentToSignResponse>
    {
        private readonly ISignDocumentService _signDocumentService = signDocumentService;
        private readonly IDocumentRepository _documentRepository = documentRepository;

        public async Task<GenerateDocumentToSignResponse> Handle(GenerateDocumentToSignCommand request, CancellationToken cancellationToken)
        {
            var result = await _signDocumentService.GenerateDocumentToSign(request.DocumentUnitId, request.DocumentId, request.EmployeeId, request.CompanyId, request.DateLimitToSign, request.EminderEveryNDays, cancellationToken: cancellationToken);
            await _documentRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
            return result;
        }
    }

    public class GenerateDocumentToSignIdentifiedCommandHandler(IMediator mediator, ILogger<IdentifiedCommandHandler<GenerateDocumentToSignCommand, GenerateDocumentToSignResponse>> logger) : IdentifiedCommandHandler<GenerateDocumentToSignCommand, GenerateDocumentToSignResponse>(mediator, logger)
    {
        protected override GenerateDocumentToSignResponse CreateResultForDuplicateRequest() => new(Guid.Empty);

    }
}
