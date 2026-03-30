using System.Text.Json;
using PeopleManagement.API.Authorization;
using PeopleManagement.Application.Commands.DocumentCommands.BatchCreateDocumentUnits;
using PeopleManagement.Application.Commands.DocumentCommands.BatchUpdateDocumentUnitDate;
using PeopleManagement.Application.Commands.DocumentCommands.InsertDocumentRange;
using PeopleManagement.Application.Commands.DocumentCommands.InsertDocumentRangeToSign;
using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Application.Queries.BatchDocument;
using static PeopleManagement.Application.Queries.BatchDocument.BatchDocumentDtos;

namespace PeopleManagement.API.Controllers
{
    [Route("api/v1/{company}/batch-document")]
    public class BatchDocumentController(
        ILogger<BatchDocumentController> logger,
        IMediator mediator,
        IBatchDocumentQueries batchDocumentQueries
    ) : BaseController(logger)
    {
        private readonly IMediator _mediator = mediator;
        private readonly IBatchDocumentQueries _batchDocumentQueries = batchDocumentQueries;

        [HttpGet("pending-units/{documentTemplateId}")]
        [ProtectedResource("document", "view")]
        public async Task<ActionResult<BatchDocumentUnitsResult>> GetPendingUnits(
            [FromRoute] Guid company,
            [FromRoute] Guid documentTemplateId,
            [FromQuery] BatchDocumentUnitParams filters)
        {
            var result = await _batchDocumentQueries.GetPendingDocumentUnits(company, documentTemplateId, filters);
            return OkResponse(result);
        }

        [HttpGet("missing-employees/{documentTemplateId}")]
        [ProtectedResource("document", "view")]
        public async Task<ActionResult<IEnumerable<EmployeeMissingDocumentDto>>> GetMissingEmployees(
            [FromRoute] Guid company,
            [FromRoute] Guid documentTemplateId,
            [FromQuery] BatchDocumentUnitParams filters)
        {
            var result = await _batchDocumentQueries.GetEmployeesWithoutPendingDocument(company, documentTemplateId, filters);
            return OkResponse(result);
        }

        [HttpPost("batch-create")]
        [ProtectedResource("document", "create")]
        public async Task<ActionResult<BatchCreateDocumentUnitsResponse>> BatchCreate(
            [FromRoute] Guid company,
            [FromBody] BatchCreateDocumentUnitsModel request,
            [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<BatchCreateDocumentUnitsCommand, BatchCreateDocumentUnitsResponse>(
                request.ToCommand(company), requestId);

            SendingCommandLog(request.DocumentTemplateId, request, requestId);

            var result = await _mediator.Send(command);

            CommandResultLog(result, request.DocumentTemplateId, request, requestId);

            return OkResponse(result);
        }

        [HttpPut("batch-update-date")]
        [ProtectedResource("document", "edit")]
        public async Task<ActionResult<BatchUpdateDocumentUnitDateResponse>> BatchUpdateDate(
            [FromRoute] Guid company,
            [FromBody] BatchUpdateDocumentUnitDateModel request,
            [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<BatchUpdateDocumentUnitDateCommand, BatchUpdateDocumentUnitDateResponse>(
                request.ToCommand(company), requestId);

            SendingCommandLog(request.Date, request, requestId);

            var result = await _mediator.Send(command);

            CommandResultLog(result, request.Date, request, requestId);

            return OkResponse(result);
        }

        [HttpPost("insert-range")]
        [ProtectedResource("document", "upload")]
        [RequestSizeLimit(125_829_120)]
        public async Task<ActionResult<InsertDocumentRangeResponse>> InsertRange(
            List<IFormFile> formFiles,
            [FromRoute] Guid company,
            [FromForm] string itemsJson,
            [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var items = JsonSerializer.Deserialize<List<InsertDocumentRangeItemModel>>(
                itemsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new ArgumentException("Invalid itemsJson");

            var commandItems = MatchFilesToItems(formFiles, items);

            var command = new InsertDocumentRangeCommand(commandItems, company);

            SendingCommandLog(items.Count, command, requestId);

            var result = await _mediator.Send(command);

            CommandResultLog(result, items.Count, command, requestId);

            return OkResponse(result);
        }

        [HttpPost("insert-range/send2sign")]
        [ProtectedResource("document", ["upload", "send2sign"])]
        [RequestSizeLimit(125_829_120)]
        public async Task<ActionResult<InsertDocumentRangeResponse>> InsertRangeToSign(
            List<IFormFile> formFiles,
            [FromRoute] Guid company,
            [FromForm] string itemsJson,
            [FromForm] DateTime dateLimitToSign,
            [FromForm] int reminderEveryNDays,
            [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var items = JsonSerializer.Deserialize<List<InsertDocumentRangeItemModel>>(
                itemsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new ArgumentException("Invalid itemsJson");

            var commandItems = MatchFilesToItems(formFiles, items);

            var command = new InsertDocumentRangeToSignCommand(commandItems, dateLimitToSign, reminderEveryNDays, company);

            SendingCommandLog(items.Count, command, requestId);

            var result = await _mediator.Send(command);

            CommandResultLog(result, items.Count, command, requestId);

            return OkResponse(result);
        }

        private static List<InsertDocumentRangeItem> MatchFilesToItems(List<IFormFile> formFiles, List<InsertDocumentRangeItemModel> items)
        {
            var commandItems = new List<InsertDocumentRangeItem>();

            foreach (var item in items)
            {
                var file = formFiles.FirstOrDefault(f => f.FileName == item.FileName)
                    ?? throw new ArgumentException($"File not found for {item.FileName}");

                commandItems.Add(new InsertDocumentRangeItem(
                    item.DocumentUnitId,
                    item.DocumentId,
                    item.EmployeeId,
                    Path.GetExtension(file.FileName),
                    file.OpenReadStream()));
            }

            return commandItems;
        }
    }

    public record InsertDocumentRangeItemModel(
        Guid DocumentUnitId,
        Guid DocumentId,
        Guid EmployeeId,
        string FileName);
}
