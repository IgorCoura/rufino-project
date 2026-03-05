using Microsoft.EntityFrameworkCore;
using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Infra.Idempotency;

namespace PeopleManagement.Application.Commands.DocumentCommands.MarkAsNotApplicableDocumentUnit
{
    public class MarkAsNotApplicableDocumentUnitCommandHandler(IDocumentRepository documentRepository)
        : IRequestHandler<MarkAsNotApplicableDocumentUnitCommand, MarkAsNotApplicableDocumentUnitResponse>
    {
        private readonly IDocumentRepository _documentRepository = documentRepository;

        public async Task<MarkAsNotApplicableDocumentUnitResponse> Handle(MarkAsNotApplicableDocumentUnitCommand request, CancellationToken cancellationToken)
        {
            var document = await _documentRepository.FirstOrDefaultAsync(
                x => x.Id == request.DocumentId && x.EmployeeId == request.EmployeeId && x.CompanyId == request.CompanyId,
                include: i => i.Include(x => x.DocumentsUnits),
                cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Document), request.DocumentId.ToString()));

            document.MarkAsNotApplicableDocumentUnit(request.DocumentUnitId);
            await _documentRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
            return request.DocumentUnitId;
        }
    }

    public class MarkAsNotApplicableDocumentUnitIdentifiedCommandHandler(IMediator mediator,
        ILogger<IdentifiedCommandHandler<MarkAsNotApplicableDocumentUnitCommand, MarkAsNotApplicableDocumentUnitResponse>> logger, IRequestManager requestManager)
        : IdentifiedCommandHandler<MarkAsNotApplicableDocumentUnitCommand, MarkAsNotApplicableDocumentUnitResponse>(mediator, logger, requestManager)
    {
        protected override MarkAsNotApplicableDocumentUnitResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
    }
}
