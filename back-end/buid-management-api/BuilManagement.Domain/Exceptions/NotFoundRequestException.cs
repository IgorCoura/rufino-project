using FluentValidation.Results;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Domain.Exceptions
{
    public class NotFoundRequestException : Exception
    {
        public List<ValidationFailure> Errors { get; set; } = new List<ValidationFailure>();
        public NotFoundRequestException()
        {
        }

        public NotFoundRequestException(string errorMessage, string memberNames) : this()
        {
            Errors.Add(new ValidationFailure(memberNames, errorMessage));
        }

        public NotFoundRequestException(IEnumerable<ValidationFailure> errorMessage) : this()
        {
            Errors.AddRange(errorMessage);
        }
    }
}
