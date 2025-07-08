using static PeopleManagement.Application.Queries.Base.BaseDtos;

namespace PeopleManagement.Application.Queries.ArchiveCategory
{
    public class ArchiveCategoryDtos
    {
        public record ArchiveCategoryDTO
        {
            public Guid Id { get; init; }
            public string Name { get; init; } = string.Empty;
            public string Description { get; init; } = string.Empty;
            public IEnumerable<EnumerationDto> ListenEvents{ get; init; } = [];
            public Guid CompanyId { get; init; } 
        }


    }
}
