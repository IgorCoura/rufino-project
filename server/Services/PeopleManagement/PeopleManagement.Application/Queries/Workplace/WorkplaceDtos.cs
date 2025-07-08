using static PeopleManagement.Application.Queries.Role.RoleDtos;

namespace PeopleManagement.Application.Queries.Workplace
{
    public class WorkplaceDtos
    {
        public record WorkplaceDto
        {
            public Guid Id { get; init; }
            public string Name { get; init; } = string.Empty;
            public WorkplaceAddressDto Address { get; init; } = WorkplaceAddressDto.Empty;
            public Guid CompanyId { get; init; }
        }

        public record WorkplaceAddressDto
        {
            public string ZipCode { get; init; } = string.Empty;
            public string Street { get; init; } = string.Empty;
            public string Number { get; init; } = string.Empty;
            public string Complement { get; init; } = string.Empty;
            public string Neighborhood { get; init; } = string.Empty;
            public string City { get; init; } = string.Empty;
            public string State { get; init; } = string.Empty;
            public string Country { get; init; } = string.Empty;

            public static WorkplaceAddressDto Empty => new();
        }

    }
}
