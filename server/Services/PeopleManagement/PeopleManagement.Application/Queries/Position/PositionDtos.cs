using static PeopleManagement.Application.Queries.Department.DepartmentDtos;

namespace PeopleManagement.Application.Queries.Position
{
    public class PositionDtos
    {
        public record PositionSimpleDto
        {
            public Guid Id { get; init; }
            public string Name { get; init; } = string.Empty;
            public string Description { get; init; } = string.Empty;
            public string CBO { get; init; } = string.Empty;
            public static DepartmentPositionDto Empty => new();
        }
    }
}
