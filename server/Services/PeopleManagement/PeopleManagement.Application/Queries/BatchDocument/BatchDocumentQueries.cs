using Microsoft.EntityFrameworkCore;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Infra.Context;
using static PeopleManagement.Application.Queries.Base.BaseDtos;
using static PeopleManagement.Application.Queries.BatchDocument.BatchDocumentDtos;
using static PeopleManagement.Application.Queries.Document.DocumentDtos;

namespace PeopleManagement.Application.Queries.BatchDocument
{
    public class BatchDocumentQueries(PeopleManagementContext context) : IBatchDocumentQueries
    {
        private readonly PeopleManagementContext _context = context;

        public async Task<BatchDocumentUnitsResult> GetPendingDocumentUnits(Guid companyId, Guid documentTemplateId, BatchDocumentUnitParams filters)
        {
            var query = from doc in _context.Documents.AsNoTracking()
                        join unit in _context.DocumentsUnits.AsNoTracking()
                            on doc.Id equals unit.DocumentId
                        join emp in _context.Employees.AsNoTracking()
                            on doc.EmployeeId equals emp.Id
                        join template in _context.DocumentTemplates.AsNoTracking()
                            on doc.DocumentTemplateId equals template.Id
                        where doc.DocumentTemplateId == documentTemplateId
                           && doc.CompanyId == companyId
                           && unit.Status == DocumentUnitStatus.Pending
                        select new { doc, unit, emp, template };

            if (filters.EmployeeStatusId.HasValue)
                query = query.Where(x => x.emp.Status == (Status)filters.EmployeeStatusId);

            if (!string.IsNullOrWhiteSpace(filters.EmployeeName))
                query = query.Where(x => ((string)(object)x.emp.Name).Contains(filters.EmployeeName.ToUpper()));

            if (filters.PeriodTypeId.HasValue)
                query = query.Where(x => x.unit.Period != null && x.unit.Period.Type == (PeriodType)filters.PeriodTypeId);

            if (filters.PeriodYear.HasValue)
                query = query.Where(x => x.unit.Period != null && x.unit.Period.Year == filters.PeriodYear);

            if (filters.PeriodMonth.HasValue)
                query = query.Where(x => x.unit.Period != null && x.unit.Period.Month == filters.PeriodMonth);

            if (filters.PeriodDay.HasValue)
                query = query.Where(x => x.unit.Period != null && x.unit.Period.Day == filters.PeriodDay);

            if (filters.PeriodWeek.HasValue)
                query = query.Where(x => x.unit.Period != null && x.unit.Period.Week == filters.PeriodWeek);

            var totalCount = await query.CountAsync();

            var skip = (filters.PageNumber - 1) * filters.PageSize;

            var rawItems = await query
                .OrderBy(x => x.emp.Name)
                .Skip(skip)
                .Take(filters.PageSize)
                .Select(x => new
                {
                    DocumentUnitId = x.unit.Id,
                    DocumentId = x.doc.Id,
                    EmployeeId = x.emp.Id,
                    EmployeeName = x.emp.Name.Value,
                    EmployeeStatusId = x.emp.Status.Id,
                    EmployeeStatusName = x.emp.Status.Name,
                    DocumentUnitDate = x.unit.Date,
                    DocumentUnitStatusId = x.unit.Status.Id,
                    DocumentUnitStatusName = x.unit.Status.Name,
                    PeriodTypeId = x.unit.Period != null ? (int?)x.unit.Period.Type.Id : null,
                    PeriodTypeName = x.unit.Period != null ? x.unit.Period.Type.Name : null,
                    PeriodDay = x.unit.Period != null ? x.unit.Period.Day : null,
                    PeriodWeek = x.unit.Period != null ? x.unit.Period.Week : null,
                    PeriodMonth = x.unit.Period != null ? (int?)x.unit.Period.Month : null,
                    PeriodYear = x.unit.Period != null ? (int?)x.unit.Period.Year : null,
                    x.template.IsSignable,
                    CanGenerateDocument = x.template.CanGenerateDocuments,
                })
                .ToListAsync();

            var items = rawItems.Select(x => new BatchDocumentUnitDto
            {
                DocumentUnitId = x.DocumentUnitId,
                DocumentId = x.DocumentId,
                EmployeeId = x.EmployeeId,
                EmployeeName = x.EmployeeName,
                EmployeeStatus = new EnumerationDto { Id = x.EmployeeStatusId, Name = x.EmployeeStatusName },
                DocumentUnitDate = x.DocumentUnitDate,
                DocumentUnitStatus = new EnumerationDto { Id = x.DocumentUnitStatusId, Name = x.DocumentUnitStatusName },
                Period = x.PeriodTypeId != null ? new PeriodDto
                {
                    Type = new EnumerationDto { Id = x.PeriodTypeId.Value, Name = x.PeriodTypeName ?? string.Empty },
                    Day = x.PeriodDay,
                    Week = x.PeriodWeek,
                    Month = x.PeriodMonth,
                    Year = x.PeriodYear ?? 0
                } : null,
                IsSignable = x.IsSignable,
                CanGenerateDocument = x.CanGenerateDocument,
            });

            return new BatchDocumentUnitsResult
            {
                Items = items,
                TotalCount = totalCount
            };
        }

        public async Task<IEnumerable<EmployeeMissingDocumentDto>> GetEmployeesWithoutPendingDocument(Guid companyId, Guid documentTemplateId, BatchDocumentUnitParams filters)
        {
            // Employees who already have a pending DocumentUnit for this template
            var employeesWithPendingUnit = await _context.Documents
                .AsNoTracking()
                .Where(d => d.CompanyId == companyId && d.DocumentTemplateId == documentTemplateId)
                .Join(_context.DocumentsUnits.AsNoTracking().Where(u => u.Status == DocumentUnitStatus.Pending),
                    d => d.Id, u => u.DocumentId, (d, u) => d.EmployeeId)
                .Distinct()
                .ToListAsync();

            var excludeIds = employeesWithPendingUnit.ToHashSet();

            // Employees who HAVE a Document for this template but do NOT have a pending unit
            var query = from doc in _context.Documents.AsNoTracking()
                        join emp in _context.Employees.AsNoTracking()
                            on doc.EmployeeId equals emp.Id
                        where doc.CompanyId == companyId
                           && doc.DocumentTemplateId == documentTemplateId
                           && !excludeIds.Contains(emp.Id)
                        select emp;

            if (filters.EmployeeStatusId.HasValue)
                query = query.Where(e => e.Status == (Status)filters.EmployeeStatusId);

            if (!string.IsNullOrWhiteSpace(filters.EmployeeName))
                query = query.Where(e => ((string)(object)e.Name).Contains(filters.EmployeeName.ToUpper()));

            var rawEmployees = await query
                .Distinct()
                .OrderBy(e => e.Name)
                .Select(e => new
                {
                    e.Id,
                    EmployeeName = e.Name.Value,
                    StatusId = e.Status.Id,
                    StatusName = e.Status.Name,
                })
                .ToListAsync();

            var employees = rawEmployees.Select(e => new EmployeeMissingDocumentDto
            {
                EmployeeId = e.Id,
                EmployeeName = e.EmployeeName,
                EmployeeStatus = new EnumerationDto { Id = e.StatusId, Name = e.StatusName },
            }).ToList();

            return employees;
        }
    }
}
