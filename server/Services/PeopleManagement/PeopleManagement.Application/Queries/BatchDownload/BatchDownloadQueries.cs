using Microsoft.EntityFrameworkCore;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Infra.Context;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using System.IO.Compression;
using static PeopleManagement.Application.Queries.Base.BaseDtos;
using static PeopleManagement.Application.Queries.BatchDownload.BatchDownloadDtos;
using static PeopleManagement.Application.Queries.Document.DocumentDtos;

namespace PeopleManagement.Application.Queries.BatchDownload
{
    public class BatchDownloadQueries(PeopleManagementContext context, IBlobService blobService) : IBatchDownloadQueries
    {
        private readonly PeopleManagementContext _context = context;
        private readonly IBlobService _blobService = blobService;

        public async Task<BatchDownloadEmployeesResult> GetEmployeesForDownload(Guid companyId, BatchDownloadEmployeeParams filters)
        {
            var query = from emp in _context.Employees.AsNoTracking()
                        join role in _context.Roles.AsNoTracking()
                            on emp.RoleId equals role.Id into roleGroup
                        from role in roleGroup.DefaultIfEmpty()
                        join workplace in _context.Workplaces.AsNoTracking()
                            on emp.WorkPlaceId equals workplace.Id into workplaceGroup
                        from workplace in workplaceGroup.DefaultIfEmpty()
                        where emp.CompanyId == companyId
                        select new { emp, role, workplace };

            if (filters.StatusId.HasValue)
                query = query.Where(x => x.emp.Status == (Status)filters.StatusId);

            if (!string.IsNullOrWhiteSpace(filters.Name))
                query = query.Where(x => ((string)(object)x.emp.Name).Contains(filters.Name.ToUpper()));

            if (filters.WorkplaceId.HasValue)
                query = query.Where(x => x.emp.WorkPlaceId == filters.WorkplaceId);

            if (filters.RoleId.HasValue)
                query = query.Where(x => x.emp.RoleId == filters.RoleId);

            var totalCount = await query.CountAsync();

            var skip = (filters.PageNumber - 1) * filters.PageSize;

            var rawItems = await query
                .OrderBy(x => x.emp.Name)
                .Skip(skip)
                .Take(filters.PageSize)
                .Select(x => new
                {
                    x.emp.Id,
                    EmployeeName = x.emp.Name.Value,
                    StatusId = x.emp.Status.Id,
                    StatusName = x.emp.Status.Name,
                    RoleName = x.role != null ? x.role.Name.Value : string.Empty,
                    WorkplaceName = x.workplace != null ? x.workplace.Name.Value : string.Empty,
                })
                .ToListAsync();

            var items = rawItems.Select(x => new BatchDownloadEmployeeDto
            {
                Id = x.Id,
                Name = x.EmployeeName,
                Status = new EnumerationDto { Id = x.StatusId, Name = x.StatusName },
                RoleName = x.RoleName,
                WorkplaceName = x.WorkplaceName,
            });

            return new BatchDownloadEmployeesResult
            {
                Items = items,
                TotalCount = totalCount
            };
        }

        public async Task<BatchDownloadUnitsResult> GetDocumentUnitsForDownload(Guid companyId, BatchDownloadUnitParams filters)
        {
            var query = from doc in _context.Documents.AsNoTracking()
                        join unit in _context.DocumentsUnits.AsNoTracking()
                            on doc.Id equals unit.DocumentId
                        join emp in _context.Employees.AsNoTracking()
                            on doc.EmployeeId equals emp.Id
                        join template in _context.DocumentTemplates.AsNoTracking()
                            on doc.DocumentTemplateId equals template.Id
                        join docGroup in _context.DocumentGroups.AsNoTracking()
                            on template.DocumentGroupId equals docGroup.Id
                        where doc.CompanyId == companyId
                        select new { doc, unit, emp, template, docGroup };

            if (filters.EmployeeIds.Count > 0)
                query = query.Where(x => filters.EmployeeIds.Contains(x.emp.Id));

            if (filters.DocumentGroupId.HasValue)
                query = query.Where(x => x.template.DocumentGroupId == filters.DocumentGroupId);

            if (filters.DocumentTemplateId.HasValue)
                query = query.Where(x => x.doc.DocumentTemplateId == filters.DocumentTemplateId);

            if (filters.UnitStatusId.HasValue)
                query = query.Where(x => x.unit.Status == (DocumentUnitStatus)filters.UnitStatusId);

            if (filters.DateFrom.HasValue)
                query = query.Where(x => x.unit.Date >= filters.DateFrom.Value);

            if (filters.DateTo.HasValue)
                query = query.Where(x => x.unit.Date <= filters.DateTo.Value);

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
                .ThenByDescending(x => x.unit.Date)
                .Skip(skip)
                .Take(filters.PageSize)
                .Select(x => new
                {
                    DocumentUnitId = x.unit.Id,
                    DocumentId = x.doc.Id,
                    EmployeeId = x.emp.Id,
                    EmployeeName = x.emp.Name.Value,
                    DocumentTemplateName = x.template.Name.Value,
                    DocumentGroupName = x.docGroup.Name.Value,
                    DocumentUnitDate = x.unit.Date,
                    DocumentUnitStatusId = x.unit.Status.Id,
                    DocumentUnitStatusName = x.unit.Status.Name,
                    PeriodTypeId = x.unit.Period != null ? (int?)x.unit.Period.Type.Id : null,
                    PeriodTypeName = x.unit.Period != null ? x.unit.Period.Type.Name : null,
                    PeriodDay = x.unit.Period != null ? x.unit.Period.Day : null,
                    PeriodWeek = x.unit.Period != null ? x.unit.Period.Week : null,
                    PeriodMonth = x.unit.Period != null ? (int?)x.unit.Period.Month : null,
                    PeriodYear = x.unit.Period != null ? (int?)x.unit.Period.Year : null,
                    HasFile = x.unit.Name != null,
                })
                .ToListAsync();

            var items = rawItems.Select(x => new BatchDownloadUnitDto
            {
                DocumentUnitId = x.DocumentUnitId,
                DocumentId = x.DocumentId,
                EmployeeId = x.EmployeeId,
                EmployeeName = x.EmployeeName,
                DocumentTemplateName = x.DocumentTemplateName,
                DocumentGroupName = x.DocumentGroupName,
                Date = x.DocumentUnitDate,
                Status = new EnumerationDto { Id = x.DocumentUnitStatusId, Name = x.DocumentUnitStatusName },
                Period = x.PeriodTypeId != null ? new PeriodDto
                {
                    Type = new EnumerationDto { Id = x.PeriodTypeId.Value, Name = x.PeriodTypeName ?? string.Empty },
                    Day = x.PeriodDay,
                    Week = x.PeriodWeek,
                    Month = x.PeriodMonth,
                    Year = x.PeriodYear ?? 0
                } : null,
                HasFile = x.HasFile,
            });

            return new BatchDownloadUnitsResult
            {
                Items = items,
                TotalCount = totalCount
            };
        }

        public async Task<Stream> DownloadBatchDocumentUnits(Guid companyId, IEnumerable<BatchDownloadItem> items)
        {
            var itemsList = items.ToList();
            var documentIds = itemsList.Select(x => x.DocumentId).Distinct().ToList();
            var unitIdsByDocument = itemsList
                .GroupBy(x => x.DocumentId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.DocumentUnitId).ToHashSet());

            var documents = await _context.Documents.AsNoTracking()
                .Include(x => x.DocumentsUnits)
                .Where(x => documentIds.Contains(x.Id) && x.CompanyId == companyId)
                .ToListAsync();

            var employeeIds = documents.Select(d => d.EmployeeId).Distinct().ToList();
            var employees = await _context.Employees.AsNoTracking()
                .Where(e => employeeIds.Contains(e.Id))
                .ToDictionaryAsync(e => e.Id, e => e.Name.Value);

            var templateIds = documents.Select(d => d.DocumentTemplateId).Distinct().ToList();
            var templates = await _context.DocumentTemplates.AsNoTracking()
                .Where(t => templateIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, t => t.Name.Value);

            var downloadTasks = documents
                .Where(doc => unitIdsByDocument.TryGetValue(doc.Id, out _))
                .SelectMany(doc =>
                    doc.DocumentsUnits
                        .Where(unit => unitIdsByDocument[doc.Id].Contains(unit.Id) && unit.Name != null)
                        .Select(unit =>
                        {
                            var idSuffix = unit.Id.ToString()[^4..];
                            var employeeSlug = employees.GetValueOrDefault(doc.EmployeeId, "unknown").Trim().Replace(" ", "-").ToLowerInvariant();
                            var docName = templates.GetValueOrDefault(doc.DocumentTemplateId, "document");
                            return new
                            {
                                EntryName = $"{employeeSlug}-{unit.Date:yyyy-MM-dd}-{docName}-{idSuffix}.{unit.Extension}",
                                Task = _blobService.DownloadAsync(unit.GetNameWithExtension, companyId.ToString())
                            };
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
