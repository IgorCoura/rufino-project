using Commom.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Commom.Domain.Errors
{
    public enum ErrorsMessages 
    {
        [ApiError("10", "O campo {TProperty} - {1}")]
        InvalidField,

        [ApiError("20", " O {PropertyName} com valor {0} não foi encontrado.")]
        FieldNotFound,
    }
}
