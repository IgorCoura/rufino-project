using static PeopleManagement.Application.Queries.Base.BaseDtos;

namespace PeopleManagement.Application.Queries.Department
{
    public class DepartmentDtos
    {

        public record DepartmentSimpleDto
        {
            public Guid Id { get; init; }
            public string Name { get; init; } = string.Empty;
            public string Description { get; init; } = string.Empty;
            public Guid CompanyId { get; init; }
            public static DepartmentDto Empty => new();
        }
        public record DepartmentDto
        {
            public Guid Id { get; init; }
            public string Name { get; init; } = string.Empty;
            public string Description { get; init; } = string.Empty;
            public IEnumerable<DepartmentPositionDto> Positions { get; init; } = [];
            public Guid CompanyId { get; init; }
            public static DepartmentDto Empty => new();
        }
        public record DepartmentPositionDto
        {
            public Guid Id { get; init; }
            public string Name { get; init; } = string.Empty;
            public string Description { get; init; } = string.Empty;
            public string CBO { get; init; } = string.Empty;
            public IEnumerable<DepartmentRoleDto> Roles { get; init; } = [];
            public static DepartmentPositionDto Empty => new();
        }

        public record DepartmentRoleDto
        {

            public Guid Id { get; init; }
            public string Name { get; init; } = string.Empty;
            public string Description { get; init; } = string.Empty;
            public string CBO { get; init; } = string.Empty;
            public DepartmentRemunerationDto Remuneration { get; init; } = DepartmentRemunerationDto.Empty;

        }




        public record DepartmentRemunerationDto
        {
            public EnumerationDto PaymentUnit { get; init; } = EnumerationDto.Empty;
            public DepartmentCurrencyDto BaseSalary { get; init; } = DepartmentCurrencyDto.Empty;
            public string Description { get; init; } = string.Empty;

            public static DepartmentRemunerationDto Empty => new();
        }

        public record DepartmentCurrencyDto
        {
            public EnumerationDto Type { get; init; } = EnumerationDto.Empty;
            public string Value { get; init; } = string.Empty;
            public static DepartmentCurrencyDto Empty => new();
        }
    }
}
