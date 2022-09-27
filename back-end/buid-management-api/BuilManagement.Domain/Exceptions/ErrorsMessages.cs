using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Domain.Exceptions
{
    public enum ErrorsMessages
    {
        [ApiError(1, "O paramentro {0} não está em um formato valido")]
        ErrorValidationRequest,

        [ApiError(100, "O username ou a senha está incorreto.")]
        AuthenticationFailed,

        [ApiError(200, "A ordem de compra de materila {0} não foi encontrada")]
        NotFoundMaterialPurchase

    }

    
}
