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

        public async Task<BatchDocumentUnitsResult> GetPendingDocumentUnits(Guid companyId, BatchDocumentUnitParams filters)
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
                           && unit.Status == DocumentUnitStatus.Pending
                        select new { doc, unit, emp, template, docGroup };

            if (filters.DocumentGroupId.HasValue)
                query = query.Where(x => x.template.DocumentGroupId == filters.DocumentGroupId);

            if (filters.DocumentTemplateId.HasValue)
                query = query.Where(x => x.doc.DocumentTemplateId == filters.DocumentTemplateId);

            if (filters.EmployeeId.HasValue)
                query = query.Where(x => x.emp.Id == filters.EmployeeId);

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

            // Sem template fixo a lista mistura documentos, e ordenar so por nome
            // deixaria as unidades do mesmo funcionario em ordem arbitraria — o
            // suficiente para uma linha aparecer em duas paginas ou em nenhuma.
            var rawItems = await query
                .OrderBy(x => x.emp.Name)
                .ThenBy(x => x.template.Name)
                .ThenBy(x => x.unit.Date)
                .ThenBy(x => x.unit.Id)
                .Skip(skip)
                .Take(filters.PageSize)
                .Select(x => new
                {
                    DocumentUnitId = x.unit.Id,
                    DocumentId = x.doc.Id,
                    DocumentTemplateId = x.template.Id,
                    DocumentTemplateName = x.template.Name.Value,
                    DocumentGroupName = x.docGroup.Name.Value,
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
                DocumentTemplateId = x.DocumentTemplateId,
                DocumentTemplateName = x.DocumentTemplateName,
                DocumentGroupName = x.DocumentGroupName,
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

        public async Task<IEnumerable<EmployeeMissingDocumentDto>> GetEmployeesWithoutPendingDocument(Guid companyId, BatchDocumentUnitParams filters)
        {
            // Criar pendencia exige saber para qual template criar. Sem grupo nem
            // template o escopo seria "todos os templates da empresa", que nao e
            // uma operacao que faca sentido — devolve vazio e a acao fica fora.
            if (!filters.DocumentTemplateId.HasValue && !filters.DocumentGroupId.HasValue)
                return [];

            // Os documentos que ja tem pendencia saem materializados antes: a mesma
            // pergunta como subconsulta correlacionada (`!DocumentsUnits.Any(...)`)
            // nao traduz para SQL com o conversor do smart enum.
            var documentsWithPendingUnit = await (
                from doc in _context.Documents.AsNoTracking()
                join unit in _context.DocumentsUnits.AsNoTracking()
                    on doc.Id equals unit.DocumentId
                where doc.CompanyId == companyId
                   && unit.Status == DocumentUnitStatus.Pending
                select doc.Id).Distinct().ToListAsync();

            var excludeIds = documentsWithPendingUnit.ToHashSet();

            // Documentos do escopo cuja unidade pendente nao existe: o par
            // funcionario x template que falta gerar.
            var query = from doc in _context.Documents.AsNoTracking()
                        join emp in _context.Employees.AsNoTracking()
                            on doc.EmployeeId equals emp.Id
                        join template in _context.DocumentTemplates.AsNoTracking()
                            on doc.DocumentTemplateId equals template.Id
                        where doc.CompanyId == companyId
                           && !excludeIds.Contains(doc.Id)
                        select new { doc, emp, template };

            if (filters.DocumentGroupId.HasValue)
                query = query.Where(x => x.template.DocumentGroupId == filters.DocumentGroupId);

            if (filters.DocumentTemplateId.HasValue)
                query = query.Where(x => x.doc.DocumentTemplateId == filters.DocumentTemplateId);

            if (filters.EmployeeId.HasValue)
                query = query.Where(x => x.emp.Id == filters.EmployeeId);

            if (filters.EmployeeStatusId.HasValue)
                query = query.Where(x => x.emp.Status == (Status)filters.EmployeeStatusId);

            if (!string.IsNullOrWhiteSpace(filters.EmployeeName))
                query = query.Where(x => ((string)(object)x.emp.Name).Contains(filters.EmployeeName.ToUpper()));

            var rawEmployees = await query
                .OrderBy(x => x.emp.Name)
                .ThenBy(x => x.template.Name)
                .Select(x => new
                {
                    x.emp.Id,
                    EmployeeName = x.emp.Name.Value,
                    StatusId = x.emp.Status.Id,
                    StatusName = x.emp.Status.Name,
                    DocumentTemplateId = x.template.Id,
                    DocumentTemplateName = x.template.Name.Value,
                })
                .ToListAsync();

            // A linha e o par funcionario x template; um segundo documento para o
            // mesmo par nao vira uma segunda pendencia a criar.
            var employees = rawEmployees
                .DistinctBy(e => (e.Id, e.DocumentTemplateId))
                .Select(e => new EmployeeMissingDocumentDto
                {
                    EmployeeId = e.Id,
                    EmployeeName = e.EmployeeName,
                    EmployeeStatus = new EnumerationDto { Id = e.StatusId, Name = e.StatusName },
                    DocumentTemplateId = e.DocumentTemplateId,
                    DocumentTemplateName = e.DocumentTemplateName,
                }).ToList();

            return employees;
        }
    }
}
