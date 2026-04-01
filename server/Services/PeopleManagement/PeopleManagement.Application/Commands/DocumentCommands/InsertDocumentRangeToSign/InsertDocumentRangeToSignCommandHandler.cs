using PeopleManagement.Application.Commands.DocumentCommands.InsertDocumentRange;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;

namespace PeopleManagement.Application.Commands.DocumentCommands.InsertDocumentRangeToSign
{
    public class InsertDocumentRangeToSignCommandHandler(
        ISignDocumentService signDocumentService,
        IDocumentRepository documentRepository
    ) : IRequestHandler<InsertDocumentRangeToSignCommand, InsertDocumentRangeResponse>
    {
        private readonly ISignDocumentService _signDocumentService = signDocumentService;
        private readonly IDocumentRepository _documentRepository = documentRepository;

        public async Task<InsertDocumentRangeResponse> Handle(InsertDocumentRangeToSignCommand request, CancellationToken cancellationToken)
        {
            foreach (var item in request.Items)
            {
                await _signDocumentService.InsertDocumentToSign(
                    item.DocumentUnitId, item.DocumentId, item.EmployeeId,
                    request.CompanyId, item.Extension, item.Stream,
                    request.DateLimitToSign, request.ReminderEveryNDays,
                    cancellationToken: cancellationToken);
            }

            await _documentRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            return new InsertDocumentRangeResponse(
                request.Items.Select(i => new InsertDocumentRangeResultItem(i.DocumentUnitId, true)));
        }
    }
}
