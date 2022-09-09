
using FluentValidation.Results;
using System.ComponentModel.DataAnnotations;

namespace BuildManagement.Domain.Exceptions
{
    public class BadRequestException : Exception
    {
        public List<ValidationFailure> Errors { get; set; } = new List<ValidationFailure>();
        public BadRequestException()
        {
        }

        public BadRequestException(string errorMessage, string memberNames) : this()
        {
            Errors.Add(new ValidationFailure(memberNames, errorMessage));
        }

        public BadRequestException(IEnumerable<ValidationFailure> errorMessage) : this()
        {
            Errors.AddRange(errorMessage);
        }

    }
}
