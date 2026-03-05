using Microsoft.EntityFrameworkCore;
using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Infra.Idempotency;

namespace PeopleManagement.Application.Commands.DocumentCommands.MarkAsInvalidDocumentUnit
{
    public class MarkAsInvalidDocumentUnitCommandHandler(IDocumentRepository documentRepository)
        : IRequestHandler<MarkAsInvalidDocumentUnitCommand, MarkAsInvalidDocumentUnitResponse>
    {
        private readonly IDocumentRepository _documentRepository = documentRepository;

        public async Task<MarkAsInvalidDocumentUnitResponse> Handle(MarkAsInvalidDocumentUnitCommand request, CancellationToken cancellationToken)
        {
            var document = await _documentRepository.FirstOrDefaultAsync(
                x => x.Id == request.DocumentId && x.EmployeeId == request.EmployeeId && x.CompanyId == request.CompanyId,
                include: i => i.Include(x => x.DocumentsUnits),
                cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Document), request.DocumentId.ToString()));

            document.MarkAsInvalidDocumentUnit(request.DocumentUnitId);
            await _documentRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
            return request.DocumentUnitId;
        }
    }

    public class MarkAsInvalidDocumentUnitIdentifiedCommandHandler(IMediator mediator,
        ILogger<IdentifiedCommandHandler<MarkAsInvalidDocumentUnitCommand, MarkAsInvalidDocumentUnitResponse>> logger, IRequestManager requestManager)
        : IdentifiedCommandHandler<MarkAsInvalidDocumentUnitCommand, MarkAsInvalidDocumentUnitResponse>(mediator, logger, requestManager)
    {
        protected override MarkAsInvalidDocumentUnitResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
    }
}
