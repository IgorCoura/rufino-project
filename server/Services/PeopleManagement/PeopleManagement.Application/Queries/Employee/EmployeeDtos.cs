namespace PeopleManagement.Application.Queries.Employee
{
    public record EmployeeSimpleDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Registration { get; set; }
        public int Status { get; set; }
        public Guid? RoleId { get; set; }
        public string RoleName { get; set; }
    }
}
