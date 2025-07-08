
namespace PeopleManagement.Application.Commands.DTO
{
    public record BaseDTO
    {
        public Guid Id { get; }

        public BaseDTO(Guid id)
        {
            Id = id;
        }


        public static implicit operator BaseDTO(Guid input) =>
            new(input);

    }
}
