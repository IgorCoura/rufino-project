using Microsoft.EntityFrameworkCore;
using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Infra.Idempotency;

namespace PeopleManagement.Application.Commands.DocumentCommands.BatchCreateDocumentUnits
{
    public class BatchCreateDocumentUnitsCommandHandler(
        IDocumentService documentService,
        IDocumentRepository documentRepository
    ) : IRequestHandler<BatchCreateDocumentUnitsCommand, BatchCreateDocumentUnitsResponse>
    {
        private readonly IDocumentService _documentService = documentService;
        private readonly IDocumentRepository _documentRepository = documentRepository;

        public async Task<BatchCreateDocumentUnitsResponse> Handle(BatchCreateDocumentUnitsCommand request, CancellationToken cancellationToken)
        {
            var createdItems = new List<BatchCreatedItem>();

            foreach (var employeeId in request.EmployeeIds)
            {
                var document = await _documentRepository.FirstOrDefaultAsync(
                    x => x.DocumentTemplateId == request.DocumentTemplateId
                      && x.EmployeeId == employeeId
                      && x.CompanyId == request.CompanyId,
                    include: i => i.Include(x => x.DocumentsUnits),
                    cancellation: cancellationToken)
                    ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Document),
                        $"TemplateId={request.DocumentTemplateId}, EmployeeId={employeeId}"));

                var documentUnit = document.NewDocumentUnit(Guid.NewGuid());

                createdItems.Add(new BatchCreatedItem(employeeId, document.Id, documentUnit.Id));
            }

            await _documentRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            return new BatchCreateDocumentUnitsResponse(createdItems);
        }
    }

    public class BatchCreateDocumentUnitsIdentifiedCommandHandler(
        IMediator mediator,
        ILogger<IdentifiedCommandHandler<BatchCreateDocumentUnitsCommand, BatchCreateDocumentUnitsResponse>> logger,
        IRequestManager requestManager
    ) : IdentifiedCommandHandler<BatchCreateDocumentUnitsCommand, BatchCreateDocumentUnitsResponse>(mediator, logger, requestManager)
    {
        protected override BatchCreateDocumentUnitsResponse CreateResultForDuplicateRequest()
            => new([]);
    }
}
