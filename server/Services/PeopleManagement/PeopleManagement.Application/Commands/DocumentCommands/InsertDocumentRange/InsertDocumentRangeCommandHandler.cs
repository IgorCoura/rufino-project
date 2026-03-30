using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Domain.ErrorTools;

namespace PeopleManagement.Application.Commands.DocumentCommands.InsertDocumentRange
{
    public class InsertDocumentRangeCommandHandler(
        IDocumentService documentService,
        IDocumentRepository documentRepository
    ) : IRequestHandler<InsertDocumentRangeCommand, InsertDocumentRangeResponse>
    {
        private readonly IDocumentService _documentService = documentService;
        private readonly IDocumentRepository _documentRepository = documentRepository;

        public async Task<InsertDocumentRangeResponse> Handle(InsertDocumentRangeCommand request, CancellationToken cancellationToken)
        {
            foreach (var item in request.Items)
            {
                await _documentService.InsertFileWithoutRequireValidation(
                    item.DocumentUnitId, item.DocumentId, item.EmployeeId,
                    request.CompanyId, item.Extension, item.Stream, cancellationToken);
            }

            await _documentRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            return new InsertDocumentRangeResponse(
                request.Items.Select(i => new InsertDocumentRangeResultItem(i.DocumentUnitId, true)));
        }


    }
}
