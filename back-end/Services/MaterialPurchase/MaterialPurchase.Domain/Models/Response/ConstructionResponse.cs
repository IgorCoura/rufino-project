using MaterialPurchase.Domain.Entities;


namespace MaterialPurchase.Domain.Models.Response
{
    public record ConstructionResponse
    (
        Guid Id,
        string CorporateName,
        string NickName,
        Address? Address
    );
}
