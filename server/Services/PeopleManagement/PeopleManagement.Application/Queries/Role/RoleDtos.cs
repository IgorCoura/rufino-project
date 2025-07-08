using static PeopleManagement.Application.Queries.Base.BaseDtos;

namespace PeopleManagement.Application.Queries.Role
{
    public class RoleDtos
    {
        public record RoleSimpleDto
        {

            public Guid Id { get; init; }
            public string Name { get; init; } = string.Empty;
            public string Description { get; init; } = string.Empty;
            public string CBO { get; init; } = string.Empty;
            public RoleRemunerationDto Remuneration { get; init; } = RoleRemunerationDto.Empty;
            public Guid PositionId { get; init; }
            public Guid CompanyId { get; init; }
        }
        public record RoleDto
        {

            public Guid Id { get; init; }
            public string Name { get; init; } = string.Empty;
            public string Description { get; init; } = string.Empty;
            public string CBO { get; init; } = string.Empty;
            public RoleRemunerationDto Remuneration { get; init; } = RoleRemunerationDto.Empty;
            public Guid PositionId { get; init; }
            public RolePositionDto Position { get; init; } = RolePositionDto.Empty;

            public Guid CompanyId { get; init; }
        }

        public record RolePositionDto
        {
            public Guid Id { get; init; }
            public string Name { get; init; } = string.Empty;
            public string Description { get; init; } = string.Empty;
            public string CBO { get; init; } = string.Empty;
            public Guid DepartmentId { get; init; }
            public RoleDepartmentDto Department { get; init; } = RoleDepartmentDto.Empty;
            public static RolePositionDto Empty => new();
        }
        public record RoleDepartmentDto
        {
            public Guid Id { get; init; }
            public string Name { get; init; } = string.Empty;
            public string Description { get; init; } = string.Empty;
            public static RoleDepartmentDto Empty => new();
        }


        public record RoleRemunerationDto
        {
            public EnumerationDto PaymentUnit { get; init; } = EnumerationDto.Empty;
            public RoleCurrencyDto BaseSalary { get; init; } = RoleCurrencyDto.Empty;
            public string Description { get; init; } = string.Empty;

            public static RoleRemunerationDto Empty => new();
        }

        public record RoleCurrencyDto
        {
            public EnumerationDto Type { get; init; } = EnumerationDto.Empty;
            public string Value { get; init; } = string.Empty;
            public static RoleCurrencyDto Empty => new();
        }

    }
}
