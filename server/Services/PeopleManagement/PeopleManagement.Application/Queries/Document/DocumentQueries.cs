using Microsoft.EntityFrameworkCore;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Infra.Context;
using static PeopleManagement.Application.Queries.Document.DocumentDtos;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.options;
using PeopleManagement.Infra.Services;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using System.IO.Compression;

namespace PeopleManagement.Application.Queries.Document
{
    public class DocumentQueries(PeopleManagementContext context, IBlobService blobService) : IDocumentQueries
    {
        private readonly PeopleManagementContext _context = context;
        private readonly IBlobService _blobService = blobService;
        public async Task<IEnumerable<DocumentSimpleDto>> GetAllSimple(Guid employeeId, Guid companyId)
        {
            var query = from document in _context.Documents
                        join template in _context.DocumentTemplates on document.DocumentTemplateId equals template.Id
                        where document.EmployeeId == employeeId && document.CompanyId == companyId
                        select new { Document = document, Template = template };

            var data = await query.ToListAsync();

            var result = data.Select(x => new DocumentSimpleDto
            {
                Id = x.Document.Id,
                Name = x.Document.Name.Value,
                Description = x.Document.Description.Value,
                Status = x.Document.Status,
                EmployeeId = x.Document.EmployeeId,
                CompanyId  = x.Document.CompanyId,
                RequiredDocumentId = x.Document.RequiredDocumentId,
                DocumentTemplateId = x.Document.DocumentTemplateId,
                UsePreviousPeriod = x.Document.UsePreviousPeriod,
                IsSignable = x.Template.IsSignable,
                CanGenerateDocument = x.Template.CanGenerateDocuments,
                CreateAt = x.Document.CreatedAt,
                UpdateAt = x.Document.UpdatedAt
            }).ToList();

            return result;
        }

        public async Task<DocumentDto> GetById(Guid documentId, Guid employeeId, Guid companyId, DocumentUnitParams unitParams)
        {
            var document = await _context.Documents
                .AsNoTracking()
                .Where(x => x.Id == documentId && x.EmployeeId == employeeId && x.CompanyId == companyId)
                .SingleOrDefaultAsync()
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Document), documentId.ToString()));

            var template = await _context.DocumentTemplates
                .AsNoTracking()
                .Where(x => x.Id == document.DocumentTemplateId)
                .SingleOrDefaultAsync()
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentTemplate), document.DocumentTemplateId.ToString()));

            var unitsQuery = _context.Entry(document)
                .Collection(x => x.DocumentsUnits)
                .Query()
                .AsNoTracking();

            if (unitParams.StatusId.HasValue)
                unitsQuery = unitsQuery.Where(x => x.Status == (DocumentUnitStatus)unitParams.StatusId);

            var totalUnitsCount = await unitsQuery.CountAsync();

            var units = await unitsQuery.ToListAsync();

            var skip = (unitParams.PageNumber - 1) * unitParams.PageSize;

            var pagedUnits = units
                .OrderBy(x => DocumentUnitStatus.GetOrder(x.Status))
                .ThenByDescending(x => x.CreatedAt)
                .Skip(skip)
                .Take(unitParams.PageSize)
                .Select(x => (DocumentUnitDto)x)
                .ToList();

            return new DocumentDto
            {
                Id = document.Id,
                Name = document.Name.Value,
                Description = document.Description.Value,
                Status = document.Status,
                EmployeeId = document.EmployeeId,
                CompanyId = document.CompanyId,
                RequiredDocumentId = document.RequiredDocumentId,
                DocumentTemplateId = document.DocumentTemplateId,
                UsePreviousPeriod = document.UsePreviousPeriod,
                IsSignable = template.IsSignable,
                CanGenerateDocument = template.CanGenerateDocuments,
                CreateAt = document.CreatedAt,
                UpdateAt = document.UpdatedAt,
                TotalUnitsCount = totalUnitsCount,
                DocumentsUnits = pagedUnits
            };
        }

        public async Task<Stream> DownloadDocumentUnit(Guid documentUnitId, Guid documentId, Guid employeeId, Guid companyId)
        {
            var document = await _context.Documents.AsNoTracking().Include(x => x.DocumentsUnits.Where(x => x.Id == documentUnitId))
                .Where(x => x.EmployeeId == employeeId && x.CompanyId == companyId && x.Id == documentId).SingleOrDefaultAsync()
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Document), documentId.ToString()));

            var documentUnit = document.DocumentsUnits.SingleOrDefault(x => x.Id == documentUnitId)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentUnit), documentUnitId.ToString()));

            var file = await _blobService.DownloadAsync(documentUnit.GetNameWithExtension, document.CompanyId.ToString());

            return file;
        }

        public async Task<Stream> DownloadDocumentUnitRange(IEnumerable<DownloadRangeDocumentItem> items, Guid employeeId, Guid companyId)
        {
            var itemsList = items.ToList();
            var documentIds = itemsList.Select(x => x.DocumentId).ToList();
            var unitIdsByDocument = itemsList.ToDictionary(x => x.DocumentId, x => x.DocumentUnitIds.ToHashSet());

            var documents = await _context.Documents.AsNoTracking()
                .Include(x => x.DocumentsUnits)
                .Where(x => documentIds.Contains(x.Id) && x.EmployeeId == employeeId && x.CompanyId == companyId)
                .ToListAsync();

            var downloadTasks = documents
                .Where(doc => unitIdsByDocument.TryGetValue(doc.Id, out _))
                .SelectMany(doc =>
                    doc.DocumentsUnits
                        .Where(unit => unitIdsByDocument[doc.Id].Contains(unit.Id))
                        .Select(unit => new
                        {
                            EntryName = $"{doc.Id}/{unit.GetNameWithExtension}",
                            Task = _blobService.DownloadAsync(unit.GetNameWithExtension, companyId.ToString())
                        }))
                .ToList();

            var streams = await Task.WhenAll(downloadTasks.Select(x => x.Task));

            var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                for (int i = 0; i < downloadTasks.Count; i++)
                {
                    var entry = archive.CreateEntry(downloadTasks[i].EntryName, CompressionLevel.Fastest);
                    using var entryStream = entry.Open();
                    await streams[i].CopyToAsync(entryStream);
                }
            }

            memoryStream.Position = 0;
            return memoryStream;
        }

    }
}
