using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MaterialPurchase.Domain.Models.Response
{
    public record MaterialResponse
    (
        Guid Id,
        string Name,
        string Description,
        string Unity
    );
}
