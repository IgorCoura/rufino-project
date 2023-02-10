using Commom.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Commom.Domain.Errors
{
    public enum CommomErrors 
    {
        [ApiError("10", "O campo {0} - {1}")]
        InvalidField,

        [ApiError("20", " O {0} com valor {1} não foi encontrado.")]
        PropertyNotFound,

        [ApiError("30", " Violação de chave extrangeira.")]
        ReferenceConstraintViolation,

        [ApiError("31", " Unique constraint violation.")]
        UniqueConstraintViolation,

        [ApiError("101", "{0} deve ser maior ou igual a {1}.")]
        GreaterThanOrEqualValidator,

        [ApiError("102", "{0} must not be empty.")]
        NotEmptyValidator,

        [ApiError("103", "The length of {0} must be {1} characters or fewer.")]
        MaximumLengthValidator,
    }
}
