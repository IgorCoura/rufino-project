using static PeopleManagement.Application.Queries.Base.BaseDtos;
using static PeopleManagement.Application.Queries.Document.DocumentDtos;

namespace PeopleManagement.Application.Queries.BatchDownload
{
    public class BatchDownloadDtos
    {
        public record BatchDownloadEmployeeParams
        {
            public string? Name { get; init; }
            public int? StatusId { get; init; }
            public Guid? WorkplaceId { get; init; }
            public Guid? RoleId { get; init; }
            public int PageSize { get; init; } = 50;
            public int PageNumber { get; init; } = 1;
        }

        public record BatchDownloadEmployeeDto
        {
            public Guid Id { get; init; }
            public string Name { get; init; } = string.Empty;
            public EnumerationDto Status { get; init; } = EnumerationDto.Empty;
            public string RoleName { get; init; } = string.Empty;
            public string WorkplaceName { get; init; } = string.Empty;
        }

        public record BatchDownloadEmployeesResult
        {
            public IEnumerable<BatchDownloadEmployeeDto> Items { get; init; } = [];
            public int TotalCount { get; init; }
        }

        public record BatchDownloadUnitParams
        {
            public List<Guid> EmployeeIds { get; init; } = [];
            public Guid? DocumentGroupId { get; init; }
            public Guid? DocumentTemplateId { get; init; }
            public int? UnitStatusId { get; init; }
            public DateOnly? DateFrom { get; init; }
            public DateOnly? DateTo { get; init; }
            public int? PeriodTypeId { get; init; }
            public int? PeriodYear { get; init; }
            public int? PeriodMonth { get; init; }
            public int? PeriodDay { get; init; }
            public int? PeriodWeek { get; init; }
            public int PageSize { get; init; } = 50;
            public int PageNumber { get; init; } = 1;
        }

        public record BatchDownloadUnitDto
        {
            public Guid DocumentUnitId { get; init; }
            public Guid DocumentId { get; init; }
            public Guid EmployeeId { get; init; }
            public string EmployeeName { get; init; } = string.Empty;
            public string DocumentTemplateName { get; init; } = string.Empty;
            public string DocumentGroupName { get; init; } = string.Empty;
            public DateOnly Date { get; init; }
            public EnumerationDto Status { get; init; } = EnumerationDto.Empty;
            public PeriodDto? Period { get; init; }
            public bool HasFile { get; init; }
        }

        public record BatchDownloadUnitsResult
        {
            public IEnumerable<BatchDownloadUnitDto> Items { get; init; } = [];
            public int TotalCount { get; init; }
        }

        public record BatchDownloadRequest
        {
            public List<BatchDownloadItem> Items { get; init; } = [];
        }

        public record BatchDownloadItem
        {
            public Guid DocumentId { get; init; }
            public Guid DocumentUnitId { get; init; }
        }
    }
}
