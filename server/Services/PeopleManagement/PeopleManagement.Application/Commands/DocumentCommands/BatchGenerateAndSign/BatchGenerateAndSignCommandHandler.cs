using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Infra.Idempotency;

namespace PeopleManagement.Application.Commands.DocumentCommands.BatchGenerateAndSign
{
    public class BatchGenerateAndSignCommandHandler(
        ISignDocumentService signDocumentService,
        IDocumentRepository documentRepository
    ) : IRequestHandler<BatchGenerateAndSignCommand, BatchGenerateAndSignResponse>
    {
        private readonly ISignDocumentService _signDocumentService = signDocumentService;
        private readonly IDocumentRepository _documentRepository = documentRepository;

        public async Task<BatchGenerateAndSignResponse> Handle(BatchGenerateAndSignCommand request, CancellationToken cancellationToken)
        {
            foreach (var item in request.Items)
            {
                await _signDocumentService.GenerateDocumentToSign(
                    item.DocumentUnitId, item.DocumentId, item.EmployeeId,
                    request.CompanyId, request.DateLimitToSign, request.ReminderEveryNDays,
                    cancellationToken: cancellationToken);
            }

            await _documentRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            return new BatchGenerateAndSignResponse(request.Items.Count());
        }
    }

    public class BatchGenerateAndSignIdentifiedCommandHandler(
        IMediator mediator,
        ILogger<IdentifiedCommandHandler<BatchGenerateAndSignCommand, BatchGenerateAndSignResponse>> logger,
        IRequestManager requestManager
    ) : IdentifiedCommandHandler<BatchGenerateAndSignCommand, BatchGenerateAndSignResponse>(mediator, logger, requestManager)
    {
        protected override BatchGenerateAndSignResponse CreateResultForDuplicateRequest() => new(0);
    }
}
