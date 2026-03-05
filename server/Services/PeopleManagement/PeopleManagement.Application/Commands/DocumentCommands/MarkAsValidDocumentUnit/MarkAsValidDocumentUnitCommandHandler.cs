using Microsoft.EntityFrameworkCore;
using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Infra.Idempotency;

namespace PeopleManagement.Application.Commands.DocumentCommands.MarkAsValidDocumentUnit
{
    public class MarkAsValidDocumentUnitCommandHandler(IDocumentRepository documentRepository)
        : IRequestHandler<MarkAsValidDocumentUnitCommand, MarkAsValidDocumentUnitResponse>
    {
        private readonly IDocumentRepository _documentRepository = documentRepository;

        public async Task<MarkAsValidDocumentUnitResponse> Handle(MarkAsValidDocumentUnitCommand request, CancellationToken cancellationToken)
        {
            var document = await _documentRepository.FirstOrDefaultAsync(
                x => x.Id == request.DocumentId && x.EmployeeId == request.EmployeeId && x.CompanyId == request.CompanyId,
                include: i => i.Include(x => x.DocumentsUnits),
                cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Document), request.DocumentId.ToString()));

            document.MarkAsValidDocumentUnit(request.DocumentUnitId);
            await _documentRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
            return request.DocumentUnitId;
        }
    }

    public class MarkAsValidDocumentUnitIdentifiedCommandHandler(IMediator mediator,
        ILogger<IdentifiedCommandHandler<MarkAsValidDocumentUnitCommand, MarkAsValidDocumentUnitResponse>> logger, IRequestManager requestManager)
        : IdentifiedCommandHandler<MarkAsValidDocumentUnitCommand, MarkAsValidDocumentUnitResponse>(mediator, logger, requestManager)
    {
        protected override MarkAsValidDocumentUnitResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
    }
}
