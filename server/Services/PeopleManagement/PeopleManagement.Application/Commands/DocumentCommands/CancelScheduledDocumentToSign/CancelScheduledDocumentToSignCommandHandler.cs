using Microsoft.EntityFrameworkCore;
using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Infra.Idempotency;
using Document = PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Document;

namespace PeopleManagement.Application.Commands.DocumentCommands.CancelScheduledDocumentToSign
{
    public sealed class CancelScheduledDocumentToSignCommandHandler(IDocumentRepository documentRepository)
        : IRequestHandler<CancelScheduledDocumentToSignCommand, CancelScheduledDocumentToSignResponse>
    {
        private readonly IDocumentRepository _documentRepository = documentRepository;

        public async Task<CancelScheduledDocumentToSignResponse> Handle(CancelScheduledDocumentToSignCommand request, CancellationToken cancellationToken)
        {
            var document = await _documentRepository.FirstOrDefaultAsync(
                x => x.Id == request.DocumentId && x.EmployeeId == request.EmployeeId && x.CompanyId == request.CompanyId,
                include: x => x.Include(y => y.DocumentsUnits),
                cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Document), request.DocumentId.ToString()));

            document.CancelScheduledSignatureSend(request.DocumentUnitId);

            await _documentRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            return request.DocumentUnitId;
        }
    }

    public class CancelScheduledDocumentToSignIdentifiedCommandHandler(IMediator mediator,
        ILogger<IdentifiedCommandHandler<CancelScheduledDocumentToSignCommand, CancelScheduledDocumentToSignResponse>> logger, IRequestManager requestManager)
        : IdentifiedCommandHandler<CancelScheduledDocumentToSignCommand, CancelScheduledDocumentToSignResponse>(mediator, logger, requestManager)
    {
        protected override CancelScheduledDocumentToSignResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
    }
}
