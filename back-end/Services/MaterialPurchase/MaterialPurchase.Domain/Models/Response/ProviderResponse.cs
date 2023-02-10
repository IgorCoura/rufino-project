using MaterialPurchase.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MaterialPurchase.Domain.Models.Response
{
    public record ProviderResponse
    (
        Guid Id,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        string Name, 
        string Description,
        string Cnpj,
        string Email,
        string Site,
        string Phone,
        Address? Address
    );
}
