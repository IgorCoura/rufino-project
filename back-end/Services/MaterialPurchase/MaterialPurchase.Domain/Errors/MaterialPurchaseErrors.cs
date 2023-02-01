using Commom.Domain.Exceptions;

namespace MaterialPurchase.Domain.Errors
{
    public enum MaterialPurchaseErrors
    {
        [ApiError("2010", "A operação não pode ser efetuada com o status {0}")]
        PurchaseStatusInvalid,

        [ApiError("2020", "Autorização invalida.")]
        AuthorizationInvalid,

        [ApiError("2030", "A qauntidade de material recebida está divergente da compra")]
        MaterialReceivedInvalid,

    }
}
