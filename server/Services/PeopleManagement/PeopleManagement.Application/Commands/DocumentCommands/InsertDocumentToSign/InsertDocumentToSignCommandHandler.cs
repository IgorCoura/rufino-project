using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Infra.Idempotency;

namespace PeopleManagement.Application.Commands.DocumentCommands.InsertDocumentToSign
{
    public class InsertDocumentToSignCommandHandler(ISignDocumentService signDocumentService, IDocumentRepository documentRepository) : IRequestHandler<InsertDocumentToSignCommand, InsertDocumentToSignResponse>
    {
        private readonly ISignDocumentService _signDocumentService = signDocumentService;
        private readonly IDocumentRepository _documentRepository = documentRepository;
        public async Task<InsertDocumentToSignResponse> Handle(InsertDocumentToSignCommand request, CancellationToken cancellationToken)
        {
            var result = await _signDocumentService.InsertDocumentToSign(request.DocumentUnitId, request.DocumentId, request.EmployeeId, request.CompanyId,
                request.Extension, request.Stream, request.DateLimitToSign, request.EminderEveryNDays, cancellationToken: cancellationToken);
            await _documentRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
            return result;
        }
    }

    public class ReceiveDocumentToSignIdentifiedCommandHandler(IMediator mediator, 
        ILogger<IdentifiedCommandHandler<InsertDocumentToSignCommand, InsertDocumentToSignResponse>> logger, IRequestManager requestManager) 
        : IdentifiedCommandHandler<InsertDocumentToSignCommand, InsertDocumentToSignResponse>(mediator, logger, requestManager)
    {
        protected override InsertDocumentToSignResponse CreateResultForDuplicateRequest() => new(Guid.Empty);

    }
}
