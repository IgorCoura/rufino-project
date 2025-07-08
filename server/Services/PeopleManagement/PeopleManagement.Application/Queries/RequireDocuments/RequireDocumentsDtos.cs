using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate;
using static PeopleManagement.Application.Queries.Base.BaseDtos;

namespace PeopleManagement.Application.Queries.RequireDocuments
{
    public class RequireDocumentsDtos
    {
        public record RequiredWithDocumentListDto
        {
            public Guid Id { get; init; }
            public string Name { get; init; } = string.Empty;
            public string Description { get; init; } = string.Empty;
            public Guid CompanyId { get; init; }
            public EnumerationDto DocumentsStatus { get; init; } = EnumerationDto.Empty;
            public IEnumerable<RequireDocumentSimpleDocumentDto> Documents { get; init; } = [];
        }
        public record RequireDocumentSimpleDocumentDto
        {
            public Guid Id { get; init; }
            public string Name { get; init; } = string.Empty;
            public string Description { get; init; } = string.Empty;
            public EnumerationDto Status { get; init; } = EnumerationDto.Empty;
            public Guid EmployeeId { get; init; }
            public DateTime CreateAt { get; init; }
            public DateTime UpdateAt { get; init; }
        };
        public record RequireDocumentSimpleDto
        {
            public Guid Id { get; init; }
            public string Name { get; init; } = string.Empty;
            public string Description { get; init; } = string.Empty;
            public Guid CompanyId { get; init; }
        }

        public record RequireDocumentDto
        {
            public Guid Id { get; init; }
            public Guid CompanyId { get; init; }
            public string Name { get; init; } = string.Empty;
            public string Description { get; init; } = string.Empty;
            public AssociationDto Association { get; init; }
            public EnumerationDto AssociationType { get; init; } = EnumerationDto.Empty;
            public IEnumerable<RequireDocumentDocumentTemplateDto> DocumentsTemplates { get; init; } = [];
            public IEnumerable<ListenEventDto> ListenEvents { get; init; } = [];
        }

        public record ListenEventDto
        {
            public EnumerationDto Event { get; init; } = null!;
            public IEnumerable<int> Status { get; init; } = []; 
        }

        public record RequireDocumentDocumentTemplateDto
        {
            public Guid Id { get; init; }
            public string Name { get; init; } = string.Empty;
            public string Description { get; init; } = string.Empty;
            public Guid CompanyId { get; init; }
        }

        public record AssociationDto
        {
            public Guid Id { get; init; }
            public string Name { get; init; } = string.Empty;

            public static implicit operator AssociationDto(Guid id) => new AssociationDto { Id = id };
        }
    }
}

