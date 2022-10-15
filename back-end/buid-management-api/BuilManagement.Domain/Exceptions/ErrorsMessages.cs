using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Domain.Exceptions
{
    public enum ErrorsMessages
    {
        [ApiError("10", "O campo {TProperty} - {1}")]
        InvalidField,

        [ApiError("20", " O {PropertyName} com valor {0} não foi encontrado.")]
        FieldNotFound,

        [ApiError("100", "O username ou a senha está incorreto.")]
        AuthenticationFailed,

        [ApiError("200", "A ordem de compra de materila {0} não foi encontrada")]
        NotFoundMaterialPurchase,

        [ApiError("210", "O status não poder ser alterado de {0} para {1}.")]
        StatusChangeInvalid,

        [ApiError("220", "Essa função não pode ser realizada com o status {0}.")]
        InvalidStatus,

        [ApiError("230", "O material com id {0} não foi encontrado.")]
        MaterialNotFound,

        [ApiError("240", "A quantidade recebida no materialReceive[{0}] execede a quantidade informada na ordem de compra.")]
        ExcessiveAmountMaterial
    }

    
}
