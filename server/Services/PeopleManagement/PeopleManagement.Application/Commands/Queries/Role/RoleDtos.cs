using static PeopleManagement.Application.Commands.Queries.Base.BaseDtos;

namespace PeopleManagement.Application.Commands.Queries.Role
{
    public class RoleDtos
    {
        public record RoleDto
        {

            public Guid Id { get; init; }
            public string Name { get; init; } = string.Empty;
            public string Description { get; init; } = string.Empty;
            public string CBO { get; init; } = string.Empty;
            public RemunerationDto Remuneration { get; init; } = RemunerationDto.Empty;
            public Guid PositionId { get; init; }
            public PositionDto Position { get; init; } = PositionDto.Empty;

            public Guid CompanyId { get; init; }
        }

        public record PositionDto
        {
            public string Name { get; init; } = string.Empty;
            public string Description { get; init; } = string.Empty;
            public string CBO { get; init; } = string.Empty;
            public Guid DepartmentId { get; init; }
            public DepartmentDto Department { get; init; } = DepartmentDto.Empty;
            public static PositionDto Empty => new();
        }
        public record DepartmentDto
        {
            public string Name { get; init; } = string.Empty;
            public string Description { get; init; } = string.Empty;
            public static DepartmentDto Empty => new();
        }


        public record RemunerationDto
        {
            public EnumerationDto PaymentUnit { get; init; } = EnumerationDto.Empty;
            public CurrencyDto BaseSalary { get; init;} = CurrencyDto.Empty;
            public string Description { get; init;} = string.Empty;

            public static RemunerationDto Empty => new();
        }

        public record CurrencyDto
        {
            public EnumerationDto Type { get; init; } = EnumerationDto.Empty;
            public string Value { get; init; } = string.Empty;
            public static CurrencyDto Empty => new();
        }
        
    }
}
