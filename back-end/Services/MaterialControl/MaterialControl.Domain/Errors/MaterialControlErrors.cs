using Commom.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialControl.Domain.Errors
{
    public enum MaterialControlErrors
    {
        [ApiError("3000", "Teste")]
        Teste,
    }
}
