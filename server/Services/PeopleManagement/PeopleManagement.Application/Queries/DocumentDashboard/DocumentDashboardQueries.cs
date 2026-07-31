using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentGroupAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.Options;
using PeopleManagement.Infra.Context;
using static PeopleManagement.Application.Queries.Base.BaseDtos;
using static PeopleManagement.Application.Queries.Document.DocumentDtos;
using static PeopleManagement.Application.Queries.DocumentDashboard.DocumentDashboardDtos;

namespace PeopleManagement.Application.Queries.DocumentDashboard
{
    public class DocumentDashboardQueries(PeopleManagementContext context, IOptions<TimeZoneOptions> timeZoneOptions) : IDocumentDashboardQueries
    {
        private readonly PeopleManagementContext _context = context;
        private readonly TimeZoneInfo _timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneOptions.Value.TimeZoneId);

        private DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _timeZone));

        public async Task<DashboardSummaryDto> GetSummary(Guid companyId, DashboardFilterParams filters)
        {
            var today = Today;
            var rows = BuildRows(companyId, filters);

            return new DashboardSummaryDto
            {
                Expired = await ApplyBucket(rows, DashboardBucket.Expired, today, filters.ExpiringInDays).CountAsync(),
                Expiring = await ApplyBucket(rows, DashboardBucket.Expiring, today, filters.ExpiringInDays).CountAsync(),
                Pending = await ApplyBucket(rows, DashboardBucket.Pending, today, filters.ExpiringInDays).CountAsync(),
                AwaitingSignature = await ApplyBucket(rows, DashboardBucket.AwaitingSignature, today, filters.ExpiringInDays).CountAsync(),
                RequiresValidation = await ApplyBucket(rows, DashboardBucket.RequiresValidation, today, filters.ExpiringInDays).CountAsync(),
            };
        }

        public async Task<DashboardUnitsResult> GetUnits(Guid companyId, DashboardUnitsParams filters)
        {
            var today = Today;
            var rows = ApplyBucket(BuildRows(companyId, filters), filters.Bucket, today, filters.ExpiringInDays);

            var totalCount = await rows.CountAsync();

            var ordered = filters.Bucket is DashboardBucket.Expired or DashboardBucket.Expiring
                ? rows.OrderBy(r => r.Unit.Validity == null)
                    .ThenBy(r => r.Unit.Validity)
                    .ThenBy(r => r.Emp.Name)
                : rows.OrderBy(r => r.Unit.Date)
                    .ThenBy(r => r.Emp.Name);

            var skip = (filters.PageNumber - 1) * filters.PageSize;

            var rawItems = await ordered
                .Skip(skip)
                .Take(filters.PageSize)
                .Select(r => new
                {
                    DocumentUnitId = r.Unit.Id,
                    DocumentId = r.Doc.Id,
                    EmployeeId = r.Emp.Id,
                    EmployeeName = r.Emp.Name.Value,
                    EmployeeStatusId = r.Emp.Status.Id,
                    EmployeeStatusName = r.Emp.Status.Name,
                    DocumentTemplateName = r.Template.Name.Value,
                    DocumentGroupName = r.Group.Name.Value,
                    DocumentUnitDate = r.Unit.Date,
                    r.Unit.Validity,
                    DocumentUnitStatusId = r.Unit.Status.Id,
                    DocumentUnitStatusName = r.Unit.Status.Name,
                    PeriodTypeId = r.Unit.Period != null ? (int?)r.Unit.Period.Type.Id : null,
                    PeriodTypeName = r.Unit.Period != null ? r.Unit.Period.Type.Name : null,
                    PeriodDay = r.Unit.Period != null ? r.Unit.Period.Day : null,
                    PeriodWeek = r.Unit.Period != null ? r.Unit.Period.Week : null,
                    PeriodMonth = r.Unit.Period != null ? (int?)r.Unit.Period.Month : null,
                    PeriodYear = r.Unit.Period != null ? (int?)r.Unit.Period.Year : null,
                    HasFile = r.Unit.Name != null,
                })
                .ToListAsync();

            var items = rawItems.Select(x => new DashboardUnitDto
            {
                DocumentUnitId = x.DocumentUnitId,
                DocumentId = x.DocumentId,
                EmployeeId = x.EmployeeId,
                EmployeeName = x.EmployeeName,
                EmployeeStatus = new EnumerationDto { Id = x.EmployeeStatusId, Name = x.EmployeeStatusName },
                DocumentTemplateName = x.DocumentTemplateName,
                DocumentGroupName = x.DocumentGroupName,
                Date = x.DocumentUnitDate,
                Validity = x.Validity,
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

            return new DashboardUnitsResult
            {
                Items = items,
                TotalCount = totalCount
            };
        }

        private IQueryable<DashboardRow> BuildRows(Guid companyId, DashboardFilterParams filters)
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
                        select new DashboardRow { Doc = doc, Unit = unit, Emp = emp, Template = template, Group = docGroup };

            if (filters.EmployeeStatusId.HasValue)
                query = query.Where(r => r.Emp.Status == (Status)filters.EmployeeStatusId);

            if (!string.IsNullOrWhiteSpace(filters.EmployeeName))
                query = query.Where(r => ((string)(object)r.Emp.Name).Contains(filters.EmployeeName.ToUpper()));

            if (filters.DocumentGroupId.HasValue)
                query = query.Where(r => r.Template.DocumentGroupId == filters.DocumentGroupId);

            if (filters.DocumentTemplateId.HasValue)
                query = query.Where(r => r.Doc.DocumentTemplateId == filters.DocumentTemplateId);

            return query;
        }

        private IQueryable<DashboardRow> ApplyBucket(IQueryable<DashboardRow> rows, DashboardBucket bucket, DateOnly today, int expiringInDays)
        {
            var horizon = today.AddDays(expiringInDays);

            return bucket switch
            {
                // "Vencido" agora é um status persistido: a unidade que caducou fica Expired e só vira
                // Deprecated quando um substituto chega. Antes disso este bucket reconstruía a mesma
                // pergunta em SQL — "Deprecated/Invalid sem substituto resolvido na competência" — e
                // podia discordar do status do documento e do funcionário.
                //
                // A segunda cláusula continua necessária: cobre a janela entre a data de vencimento e a
                // execução do job de depreciação, quando a unidade ainda está OK/Warning no banco.
                DashboardBucket.Expired => rows.Where(r =>
                    r.Unit.Status == DocumentUnitStatus.Expired
                    || ((r.Unit.Status == DocumentUnitStatus.OK || r.Unit.Status == DocumentUnitStatus.Warning)
                        && r.Unit.Validity != null && r.Unit.Validity < today)),

                DashboardBucket.Expiring => rows.Where(r =>
                    (r.Unit.Status == DocumentUnitStatus.OK || r.Unit.Status == DocumentUnitStatus.Warning)
                    && r.Unit.Validity != null && r.Unit.Validity >= today && r.Unit.Validity <= horizon),

                DashboardBucket.Pending => rows.Where(r => r.Unit.Status == DocumentUnitStatus.Pending),

                DashboardBucket.AwaitingSignature => rows.Where(r => r.Unit.Status == DocumentUnitStatus.AwaitingSignature),

                DashboardBucket.RequiresValidation => rows.Where(r => r.Unit.Status == DocumentUnitStatus.RequiresValidation),

                _ => rows.Where(r => false),
            };
        }

        private sealed class DashboardRow
        {
            public Domain.AggregatesModel.DocumentAggregate.Document Doc { get; init; } = null!;
            public DocumentUnit Unit { get; init; } = null!;
            public Domain.AggregatesModel.EmployeeAggregate.Employee Emp { get; init; } = null!;
            public Domain.AggregatesModel.DocumentTemplateAggregate.DocumentTemplate Template { get; init; } = null!;
            public Domain.AggregatesModel.DocumentGroupAggregate.DocumentGroup Group { get; init; } = null!;
        }
    }
}
