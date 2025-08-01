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
            var query = _context.Documents.Where(x => x.EmployeeId == employeeId && x.CompanyId == companyId);

            var result = query.Select(x => new DocumentSimpleDto
            {
                Id = x.Id,
                Name = x.Name.Value,
                Description = x.Description.Value,
                Status = x.Status,
                EmployeeId = x.EmployeeId,
                CompanyId  = x.CompanyId,
                RequiredDocumentId = x.RequiredDocumentId,
                DocumentTemplateId = x.DocumentTemplateId,
                CreateAt = x.CreatedAt,
                UpdateAt = x.UpdatedAt
            }).ToListAsync();

            return await result;
        }

        public async Task<DocumentDto> GetById(Guid documentId, Guid employeeId, Guid companyId)
        {
            var query = _context.Documents.Where(x => x.Id == documentId && x.EmployeeId == employeeId && x.CompanyId == companyId);

            var result = await query.Select(x => new DocumentDto
            {
                Id = x.Id,
                Name = x.Name.Value,
                Description = x.Description.Value,
                Status = x.Status,
                EmployeeId = x.EmployeeId,
                CompanyId = x.CompanyId,
                RequiredDocumentId = x.RequiredDocumentId,
                DocumentTemplateId = x.DocumentTemplateId,
                CreateAt = x.CreatedAt,
                UpdateAt = x.UpdatedAt,
                DocumentsUnits = x.DocumentsUnits
                    .Select(x => (DocumentUnitDto)x)
                    .ToList()
            }).SingleOrDefaultAsync()
            ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Document), documentId.ToString()));

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
