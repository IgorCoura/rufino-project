using System.Linq.Expressions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace PeopleManagement.Application.Queries.Employee
{
    public record EmployeeSimpleDto
    { 
        public Guid Id { get; init; }
        public string Name { get; init; }
        public string? Registration { get; init; }
        public int Status { get; init; }
        public Guid? RoleId { get; init; }
        public string RoleName { get; init; }
    }

    public record EmployeeParams
    {
        public string? Name { get; init; }
        public string? Role { get; init; }
        public int? Status { get; init; }
        public int PageSize { get; init; } = 10;
        public int SizeSkip { get; init; } = 0;
        public SortOrder SortOrder { get; init; } = SortOrder.ASC;
    }

    public enum SortOrder
    {
        ASC,
        DESC,
    }
}
