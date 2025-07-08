using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;
using PeopleManagement.Domain.SeedWord;
using System.Text.Json;

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

            public static implicit operator EnumerationDto(Enumeration enumeration)
            {
                return new EnumerationDto(enumeration.Id, enumeration.Name);
            }

           
        }
    }
}
