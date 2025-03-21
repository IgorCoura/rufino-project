namespace PeopleManagement.Application.Queries.Base
{
    public class BaseDtos
    {
        public record EnumerationDto
        {
            private EnumerationDto(int id, string name)
            {
                Id = id;
                Name = name;
            }
            public EnumerationDto() { }
            public int Id { get; init; }
            public string Name { get; init; } = string.Empty;
            public static EnumerationDto Empty => new(-1, "");
        }
    }
}
