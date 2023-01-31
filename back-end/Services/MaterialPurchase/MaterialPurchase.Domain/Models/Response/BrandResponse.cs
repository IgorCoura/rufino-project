using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MaterialPurchase.Domain.Models.Response
{
    public record BrandResponse
    (
        Guid Id,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        string Name,
        string Description
    );
}
