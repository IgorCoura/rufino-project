using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Domain.ErrorTools;
using static PeopleManagement.Application.Queries.Base.BaseDtos;

namespace PeopleManagement.Application.Queries.Document
{
    public class DocumentDtos
    {
        public record DocumentSimpleDto
        {
            public Guid Id { get; init; }
            public string Name { get; init; } = string.Empty;
            public string Description { get; init; } = string.Empty;
            public EnumerationDto Status { get; init; } = EnumerationDto.Empty;
            public Guid EmployeeId { get; init; }
            public Guid CompanyId { get; init; }
            public Guid RequiredDocumentId { get; init; }
            public Guid DocumentTemplateId { get; init; }
            public bool UsePreviousPeriod { get; init; }
            public bool IsSignable { get; init; }
            public bool CanGenerateDocument { get; init; }
            public DateTime CreateAt { get; init; }
            public DateTime UpdateAt { get; init; }
        };

        public record DocumentDto
        {
            public Guid Id { get; init; }
            public string Name { get; init; } = string.Empty;
            public string Description { get; init; } = string.Empty;
            public Guid EmployeeId { get; init; }
            public Guid CompanyId { get; init; }
            public Guid RequiredDocumentId { get; init; }
            public Guid DocumentTemplateId { get; init; }
            public bool UsePreviousPeriod { get; init; }
            public bool IsSignable { get; init; }
            public bool CanGenerateDocument { get; init; }
            public List<DocumentUnitDto> DocumentsUnits { get; init; } = [];
            public EnumerationDto Status { get; init; } = EnumerationDto.Empty;
            public DateTime CreateAt { get; init; }
            public DateTime UpdateAt { get; init; }

        };

        public record DocumentUnitDto
        {
            public Guid Id { get; init; }
            public string Content { get; init; } = string.Empty;
            public DateOnly? Validity { get; init; }
            public string? Name { get; init; } = string.Empty;
            public string? Extension { get; init; } = string.Empty;
            public EnumerationDto Status { get; init; } = EnumerationDto.Empty;
            public DateOnly Date { get; init; }
            public PeriodDto? Period { get; init; }
            public DateTime CreateAt { get; init; }
            public DateTime UpdateAt { get; init; }

            public static implicit operator DocumentUnitDto(DocumentUnit documentUnit)
            {
                return new DocumentUnitDto
                {
                    Id = documentUnit.Id,
                    Content = documentUnit.Content,
                    Validity = documentUnit.Validity,
                    Name = documentUnit.Name?.Value,
                    Extension = documentUnit.Extension?.Name,
                    Status = (EnumerationDto)documentUnit.Status,
                    Date = documentUnit.Date,
                    Period = documentUnit.Period != null ? new PeriodDto
                    {
                        Type = (EnumerationDto)documentUnit.Period.Type,
                        Day = documentUnit.Period.Day,
                        Week = documentUnit.Period.Week,
                        Month = documentUnit.Period.Month,
                        Year = documentUnit.Period.Year
                    } : null,
                    CreateAt = documentUnit.CreatedAt,
                    UpdateAt = documentUnit.UpdatedAt
                };
            }
        }

        public record PeriodDto
        {
            public EnumerationDto Type { get; init; } = EnumerationDto.Empty;
            public int? Day { get; init; }
            public int? Week { get; init; }
            public int? Month { get; init; }
            public int Year { get; init; }
        }

    }
}
