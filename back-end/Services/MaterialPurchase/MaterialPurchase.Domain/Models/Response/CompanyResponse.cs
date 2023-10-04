using MaterialPurchase.Domain.Entities;

namespace MaterialPurchase.Domain.Models.Response
{
    public record CompanyResponse
    (
        Guid Id,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        string Name,
        string Description,
        string Cnpj,
        string Email,
        string Phone,
        Address? Address
    );
}
