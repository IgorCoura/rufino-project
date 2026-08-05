using Microsoft.EntityFrameworkCore;
using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Infra.Idempotency;
using Document = PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Document;

namespace PeopleManagement.Application.Commands.DocumentCommands.ScheduleDocumentToSign
{
    public sealed class ScheduleDocumentToSignCommandHandler(IDocumentRepository documentRepository)
        : IRequestHandler<ScheduleDocumentToSignCommand, ScheduleDocumentToSignResponse>
    {
        private readonly IDocumentRepository _documentRepository = documentRepository;

        public async Task<ScheduleDocumentToSignResponse> Handle(ScheduleDocumentToSignCommand request, CancellationToken cancellationToken)
        {
            var document = await _documentRepository.FirstOrDefaultAsync(
                x => x.Id == request.DocumentId && x.EmployeeId == request.EmployeeId && x.CompanyId == request.CompanyId,
                include: x => x.Include(y => y.DocumentsUnits),
                cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Document), request.DocumentId.ToString()));

            document.ScheduleSignatureSend(request.DocumentUnitId, request.SendOn, request.DateLimitToSign, request.EminderEveryNDays);

            await _documentRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            return request.DocumentUnitId;
        }
    }

    public class ScheduleDocumentToSignIdentifiedCommandHandler(IMediator mediator,
        ILogger<IdentifiedCommandHandler<ScheduleDocumentToSignCommand, ScheduleDocumentToSignResponse>> logger, IRequestManager requestManager)
        : IdentifiedCommandHandler<ScheduleDocumentToSignCommand, ScheduleDocumentToSignResponse>(mediator, logger, requestManager)
    {
        protected override ScheduleDocumentToSignResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
    }
}
