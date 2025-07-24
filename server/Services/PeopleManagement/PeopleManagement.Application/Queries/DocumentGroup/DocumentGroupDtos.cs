using static PeopleManagement.Application.Queries.Base.BaseDtos;

namespace PeopleManagement.Application.Queries.DocumentGroup
{
    public class DocumentGroupDtos
    {
        public record DocumentGroupDto
        {
            public Guid Id { get; init; }
            public string Name { get; init; } = string.Empty;
            public string Description { get; init; } = string.Empty;
            public Guid CompanyId { get; init; }
        }

        public record DocumentGroupWithDocumentsDto
        {
            public Guid Id { get; init; }
            public string Name { get; init; } = string.Empty;
            public string Description { get; init; } = string.Empty;
            public Guid CompanyId { get; init; }
            public EnumerationDto DocumentsStatus { get; init; } = EnumerationDto.Empty;
            public List<DocumentDocumentGroupDto> Documents { get; init; } = new();
        }

        public record DocumentDocumentGroupDto
        {
            public Guid Id { get; init; }
            public string Name { get; init; } = string.Empty;
            public string Description { get; init; } = string.Empty;
            public EnumerationDto Status { get; init; } = EnumerationDto.Empty;
            public Guid EmployeeId { get; init; }
            public DateTime CreateAt { get; init; }
            public DateTime UpdateAt { get; init; }
        }


    }
}
