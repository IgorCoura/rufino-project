using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;

namespace PeopleManagement.Application.Commands.DocumentCommands.GeneratePdfRange
{
    public class GeneratePdfRangeCommandHandler(IDocumentService documentService)
        : IRequestHandler<GeneratePdfRangeCommand, GeneratePdfRangeResponse>
    {
        private readonly IDocumentService _documentService = documentService;

        public async Task<GeneratePdfRangeResponse> Handle(GeneratePdfRangeCommand request, CancellationToken cancellationToken)
        {
            var results = await _documentService.GeneratePdfRange(
                request.Items.Select(x => (x.DocumentId, x.DocumentUnitIds)),
                request.EmployeeId,
                request.CompanyId,
                cancellationToken);

            return new GeneratePdfRangeResponse(
                results.Select(r => new GeneratePdfRangeResponseItem(r.DocumentUnitId, r.DocumentId, r.DocumentName, r.DocumentUnitDate, r.Pdf)).ToList());
        }
    }
}
