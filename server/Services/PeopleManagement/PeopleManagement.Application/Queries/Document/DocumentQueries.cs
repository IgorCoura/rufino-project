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

        public async Task<DocumentDto> GetById(Guid documentId, Guid employeeId, Guid companyId)
        {
            var document = await _context.Documents
                .Include(x => x.DocumentsUnits)
                .Where(x => x.Id == documentId && x.EmployeeId == employeeId && x.CompanyId == companyId)
                .SingleOrDefaultAsync()
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Document), documentId.ToString()));

            var template = await _context.DocumentTemplates
                .Where(x => x.Id == document.DocumentTemplateId)
                .SingleOrDefaultAsync()
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentTemplate), document.DocumentTemplateId.ToString()));

            var result = new DocumentDto
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
                DocumentsUnits = document.DocumentsUnits
                    .Select(x => (DocumentUnitDto)x)
                    .ToList()
            };

            var sortedResult = result with
            {
                DocumentsUnits = result.DocumentsUnits
                    .OrderBy(x => DocumentUnitStatus.GetOrder(x.Status.Id))
                    .ThenByDescending(x => x.CreateAt)
                    .ToList()
            };
            return sortedResult;
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
        
    }
}
